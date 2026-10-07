# Hosting runbook

Kitchen Command Center runs on a Raspberry Pi 5 behind a Cloudflare Tunnel. This runbook sets it up from nothing and
keeps it running. The design and its reasons are in `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`,
§13.

## What runs where

- **The Pi** runs Docker Compose from `/srv/kcc/repo/deploy`, a sparse clone of this repository. Three services run
  all the time:
  - `app`, Umbraco and the site, on port 8080 inside Docker's networks only;
  - `ssr`, the Node service that renders Vue on the server, reachable from `app` alone, with no route out;
  - `cloudflared`, the tunnel, which connects out to Cloudflare, so no port on the router or the Pi is open.
- **Two timers** run jobs as the user `kcc`:
  - `kcc-deploy`, every five minutes, pulls `deploy/` from git and the images from GHCR. If anything changed, it takes
    a snapshot of the database, then restarts what changed.
  - `kcc-backup`, at 03:00, sends the database, its keys and the media to the backup bucket, then pings
    healthchecks.io.
- **GitHub Actions** builds the images on every push to `main`, tests them, and pushes them to private packages on
  GHCR.
- **Cloudflare** holds the domain's DNS. Cloudflare Access asks for a one-time PIN before anyone reaches the site:
  until launch for the whole site, and always for `/umbraco`. R2 holds the backups.

Commands below that start with `sudo -u kcc -H kcc` run Docker Compose for the stack, as `kcc`, from any folder.

## 1. What you need

- **Accounts:**
  - Cloudflare, on the Free plan. Zero Trust and R2 ask for a payment method but charge nothing within their free
    tiers.
  - healthchecks.io, on the free plan.
  - Tailscale, on the free plan.
  - GitHub, with admin rights on this repository.
- **Hardware:**
  - a Raspberry Pi 5: 4 GB is enough, 8 GB comfortable;
  - the official 27 W USB-C power supply, since a USB SSD needs the full 5 A;
  - the active cooler and a case;
  - an M.2 HAT+ with an NVMe SSD, or a USB 3 SSD. Not a microSD card: the database and the indexes would wear it out.
  - an Ethernet cable, and an adapter to flash the SSD from your computer.
- **Choose the hostname.** Use the apex (`example.com`) if the domain serves nothing else. If the Squarespace site stays
  on the apex, use a subdomain such as `kitchen.example.com`. It is `KCC_HOST` from here on.

## 2. Move the domain's DNS to Cloudflare

Start this first: the switch can take up to two days, and nothing else waits for it until section 4.

1. In Cloudflare, **Add a domain**, enter the domain, and pick the **Free** plan. Cloudflare scans for common records.
2. Compare its list with Squarespace's DNS page (Domains → the domain → DNS), and add whatever the scan missed:
   - every email record: `MX`, the SPF `TXT` on `@`, the DKIM records (`TXT` or `CNAME`, under custom names the scan
     misses), and the DMARC `TXT` on `_dmarc`;
   - if the Squarespace site stays, its records, set to **DNS only** (grey cloud): four `A` records on `@`
     (`198.185.159.144`, `198.185.159.145`, `198.49.23.144`, `198.49.23.145`), the `CNAME` `www` →
     `ext-cust.squarespace.com`, and the site's own verification `CNAME` → `verify.squarespace.com`.
3. At Squarespace: Domains → the domain → DNS → **Domain Nameservers** → **Use Custom Nameservers**. Squarespace asks
   to turn DNSSEC off; continue. Enter the two nameservers Cloudflare gave you, and save.
4. Wait for Cloudflare's email that the domain is active. Check that mail still arrives and the Squarespace site, if
   kept, still loads.
5. In Cloudflare, SSL/TLS → **Edge Certificates**: turn **Always Use HTTPS** on, and set **Minimum TLS Version** to 1.2.
   Leave HSTS off here: the app sends it.
6. Optional: DNS → Settings → enable **DNSSEC**, then add the DS record Cloudflare shows at Squarespace (DNS →
   DNSSEC).

## 3. Prepare the Pi

1. **Flash the SSD** from your computer with Raspberry Pi Imager, through the adapter. Choose Raspberry Pi 5, then
   Raspberry Pi OS **Lite (64-bit)**. In its customisation:
   - set the hostname `kcc`, your username and your time zone;
   - allow SSH with **public-key authentication only**, and paste your public key;
   - leave Wi-Fi off.
2. **Fit the SSD** and boot without a microSD card. A current Pi 5 boots from NVMe or USB by itself. If yours does not,
   boot once from a microSD with Raspberry Pi OS, run `sudo rpi-eeprom-update -a`, then choose `sudo raspi-config` →
   Advanced Options → Boot Order → **NVMe/USB Boot**.
3. **Update and keep updating:**

   ```bash
   ssh <you>@kcc.local
   sudo apt update && sudo apt full-upgrade -y
   sudo apt install -y unattended-upgrades git ufw
   sudo dpkg-reconfigure -plow unattended-upgrades
   sudo reboot
   ```

4. **Close SSH to passwords.** Create `/etc/ssh/sshd_config.d/10-kcc.conf` with `sudo nano`:

   ```text
   PasswordAuthentication no
   KbdInteractiveAuthentication no
   PermitRootLogin no
   ```

   Run `sudo systemctl reload ssh`, then open a second SSH session to check the key still works before you close the
   first.
5. **Tailscale,** for SSH from anywhere:

   ```bash
   curl -fsSL https://tailscale.com/install.sh | sh
   sudo tailscale up
   ```

   Open the link it prints, and approve the Pi in your tailnet. From then on `ssh <you>@kcc` works over the tailnet.
6. **The firewall.** Allow SSH from the tailnet and your LAN only, and replace `192.168.1.0/24` with your LAN's range:

   ```bash
   sudo ufw default deny incoming
   sudo ufw default allow outgoing
   sudo ufw allow in on tailscale0
   sudo ufw allow from 192.168.1.0/24 to any port 22 proto tcp
   sudo ufw enable
   ```

   Ports that Docker publishes bypass these rules. The stack publishes none, and must never publish one: the site
   trusts the tunnel's headers only because the tunnel is the only way in.
7. **Docker Engine,** from Docker's Debian repository (Docker's page for Raspberry Pi OS covers 32-bit only):

   ```bash
   sudo install -m 0755 -d /etc/apt/keyrings
   sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
   sudo chmod a+r /etc/apt/keyrings/docker.asc
   printf 'Types: deb\nURIs: https://download.docker.com/linux/debian\nSuites: %s\nComponents: stable\nSigned-By: /etc/apt/keyrings/docker.asc\n' \
     "$(. /etc/os-release && echo "$VERSION_CODENAME")" | sudo tee /etc/apt/sources.list.d/docker.sources
   sudo apt update
   sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
   docker compose version
   ```

   Compose must be 2.24 or later.
8. **The stack's user and folder:**

   ```bash
   sudo useradd --system --create-home --home-dir /srv/kcc --shell /usr/sbin/nologin --groups docker kcc
   sudo chmod 755 /srv/kcc
   sudo -u kcc git clone --filter=blob:none --sparse https://github.com/Th3FenrisWolf/Kitchen-Command-Center.git /srv/kcc/repo
   sudo -u kcc git -C /srv/kcc/repo sparse-checkout set deploy
   sudo ln -s /srv/kcc/repo/deploy/kcc /usr/local/bin/kcc
   ```

   Debian creates home folders readable by their owner alone, and the steps below work in `/srv/kcc` as you, so the
   `chmod` opens it. The secrets stay private: `.env`, `first-boot.env` and Docker's `config.json` are mode 600.

## 4. The tunnel and Access

Once the domain is active in Cloudflare:

1. Open **Zero Trust**. Pick a team name, and the **Free** plan (it asks for a payment method and charges nothing).
2. **One-time PIN login.** Zero Trust → Integrations → Identity providers → **Add new** → **One-time PIN**. In older
   dashboards: Settings → Authentication.
3. **The tunnel.** Networking → Tunnels → **Create Tunnel**:
   - name it `kcc`, and choose **Docker**;
   - copy the token, the long string after `--token` in the command shown (do not run the command);
   - open the tunnel → Routes → **Add route** → **Published application**: the hostname is `KCC_HOST`, and the service
     URL is `http://app:8080`. Older dashboards call this a public hostname, with service type HTTP and URL `app:8080`.
4. **Access for the backoffice, for good.** Zero Trust → Access controls → Applications → **Add an application** →
   **Self-hosted**:
   - name `KCC backoffice`; public hostname `KCC_HOST`, path `umbraco`;
   - identity provider One-time PIN; session duration 24 hours;
   - one policy, `Owner`: action **Allow**, include **Emails**, with your email address.
5. **Access for the whole site, until launch.** The same again:
   - name `KCC pre-launch`; public hostname `KCC_HOST`, no path;
   - policy `Testers`: **Allow**, **Emails**: yours, and anyone else's who should see the site before launch.

   The more specific application wins, so `/umbraco` stays yours alone. Launch (replatform Phase 8) deletes this
   application.

## 5. The backup bucket, and the check that it ran

1. **R2.** R2 Object Storage: subscribe (free tier, $0), then **Create bucket** named `kcc-backups`, with location
   Automatic. The free tier holds 10 GB, so 14 daily and 8 weekly archives fit while an archive (the database plus the
   media) stays under about 450 MB.
2. **Its token.** R2 → Account Details → API Tokens → **Manage** → **Create Account API token**:
   - permission **Object Read & Write**, applied to `kcc-backups` only; no expiry;
   - note the **Access Key ID**, the **Secret Access Key** (shown once) and the S3 endpoint,
     `https://<account id>.r2.cloudflarestorage.com`.
3. **A second token for drills,** the same with **Object Read** only. Keep it for section 9.
4. **healthchecks.io.** Add a check named `kcc-backup`:
   - schedule **Cron** `0 3 * * *`, with the Pi's time zone;
   - grace time **3 hours**;
   - copy its ping URL, `https://hc-ping.com/<uuid>`. It emails you when a night passes without a backup.

Backblaze B2 works too, with no card and 10 GB free: commit a change to `main` that sets
`RCLONE_CONFIG_BUCKET_PROVIDER` to `Other` in `compose.yaml`'s backup environment, and use B2's S3 endpoint and an
application key. Do not edit the Pi's clone: a local change to a tracked file makes `git pull --ff-only` refuse once
`main` changes that file.

## 6. GitHub

1. **A token for the Pi to pull images.** Settings → Developer settings → Personal access tokens → **Tokens (classic)**
   → Generate new token:
   - scope `read:packages` only, and an expiry of a year, with a reminder to rotate it (section 8);
   - GHCR accepts only classic tokens.
2. **The packages stay private.** After the first push to `main` (the images job), open your profile's Packages. For
   each of `kcc-app`, `kcc-ssr` and `kcc-backup`, Package settings should say **Private**, and list this repository
   with the Admin role under "Manage Actions access". The images job needs that role to prune old versions.
3. **Dependabot alerts.** Under Settings → Advanced Security, turn on **Dependabot alerts**, leave **Dependabot
   security updates** off, and set **Automatic dependency submission**, under Dependency graph, to **Enabled**. The
   submission gives the dependency graph the versions in `Directory.Packages.props`. Without it the graph reads each
   NuGet package as `>= 0`, and no NuGet alert fires.

## 7. Install the stack and boot it

1. **The settings.** Fill in every value:

   ```bash
   cd /srv/kcc/repo/deploy
   sudo -u kcc cp .env.example .env && sudo chmod 600 .env
   sudo -u kcc nano .env
   ```

   - `KCC_HOST` is the hostname from section 1.
   - For `KCC_IMAGING_HMAC_KEY`, run `openssl rand -base64 64 | tr -d '\n'` and paste the output.
   - `KCC_TUNNEL_TOKEN` is from section 4, and the bucket's four values and the ping URL from section 5.
   - `ANTHROPIC_API_KEY` is optional.
2. **The first boot's administrator:**

   ```bash
   sudo -u kcc cp first-boot.env.example first-boot.env && sudo chmod 600 first-boot.env
   sudo -u kcc nano first-boot.env
   ```

   Set your name, your email and a password of at least 10 characters. Keep them in your password manager.
3. **Sign in to GHCR** as `kcc`, pasting the classic token when Docker asks for the password:

   ```bash
   sudo -u kcc -H docker login ghcr.io -u <your GitHub username>
   ```

   Docker stores it in `/srv/kcc/.docker/config.json`, readable by `kcc` alone.
4. **Boot:**

   ```bash
   sudo -u kcc -H kcc up -d --wait
   sudo -u kcc -H kcc logs app | grep "uSync First boot complete"
   sudo -u kcc -H kcc ps
   ```

   The first pull takes a few minutes. The `grep` must print a line: uSync imports the baseline on the first boot only,
   and never tries again if that boot failed (section 11). `ps` shows `app`, `ssr` and `cloudflared` as healthy.
5. **Look at it** from a phone off your Wi-Fi:
   - open `https://KCC_HOST`, pass Access with your email and the PIN it sends, and the site appears;
   - open `/umbraco`, and sign in to Umbraco as the administrator from `first-boot.env`.
6. **Turn the installer off** for good:

   ```bash
   sudo rm /srv/kcc/repo/deploy/first-boot.env
   sudo -u kcc -H kcc up -d --wait
   ```

   Without `first-boot.env` the site never installs Umbraco. So a lost database stops at Umbraco's installer, behind
   Access, and the health check fails, instead of the site quietly becoming an empty one.
7. **The timers:**

   ```bash
   sudo cp /srv/kcc/repo/deploy/systemd/kcc-* /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now kcc-deploy.timer kcc-backup.timer
   systemctl list-timers 'kcc-*'
   ```

   The units are copies. If a later commit changes them, copy them again and reload.
8. **The first backup, now:**

   ```bash
   sudo systemctl start kcc-backup.service
   journalctl -u kcc-backup -n 20 --no-pager
   ```

   The journal ends with `uploaded daily/kcc-<date>.tar.gz`, the bucket holds the file, and the healthchecks.io check
   turns green.

## 8. Everyday operations

- **Deploys.** Merge to `main`. CI pushes the images, and within five minutes the Pi deploys whatever changed,
  snapshotting the database first, and the app restarts for about a minute. A merge that changes no image, such as one
  to `docs/` alone, deploys nothing: the build reuses its cache, down to each image's digest. To watch, run
  `journalctl -u kcc-deploy -f`. To deploy now: `sudo systemctl start kcc-deploy.service`.

  A failed deploy of a new image is not retried. With nothing to deploy, a run succeeds only while `app`, `ssr` and
  `cloudflared` are all running and none is unhealthy. Otherwise it fails with `not healthy: <service> <state>, …` on
  stderr, such as `not healthy: app unhealthy`, and keeps failing every five minutes until the stack is healthy again or
  a newer deploy replaces the failed one. The failure shows in `systemctl --failed` and `journalctl -u kcc-deploy`;
  nothing sends an alert.

  A failed change under `deploy/` is the exception. The Pi keeps the last commit it deployed in the git ref
  `refs/kcc/deployed`, and moves it only after `docker compose up --wait` succeeds, so the change stays pending and is
  retried every five minutes. A deploy takes a snapshot only while the app is running and not unhealthy. So while the
  failed change leaves the app unhealthy, the retries keep the first attempt's snapshot, and a deploy onto an app that
  was already unhealthy (stuck at Umbraco's installer with no database, say) goes ahead without one: the nightly
  backup is then the restore point. A change that breaks `ssr` or `cloudflared` but leaves the app healthy takes a
  snapshot on every retry, and only the newest five are kept: the first attempt's is gone after five retries. To make
  the timer deploy again anyway, delete the ref and start the service:

  ```bash
  sudo -u kcc git -C /srv/kcc/repo update-ref -d refs/kcc/deployed
  sudo systemctl start kcc-deploy.service
  ```

  With no ref, a run deploys once, snapshot first: the first run after the timers are enabled, and the first after the
  ref is deleted.
- **Status and logs.** `sudo -u kcc -H kcc ps`, and `sudo -u kcc -H kcc logs --tail 100 app`. The backoffice's
  Settings → Log Viewer reads Umbraco's own logs, kept for 31 days on the data volume.
- **Pause for maintenance**, so no deploy or backup starts in the middle:

  ```bash
  sudo systemctl stop kcc-deploy.timer kcc-backup.timer
  sudo systemctl stop kcc-deploy.service kcc-backup.service
  sudo systemctl start kcc-deploy.timer kcc-backup.timer
  ```

  The first two lines pause; the third, afterwards, resumes. Stopping a timer does not stop a run already in progress,
  and what you run by hand does not take `/srv/kcc/kcc.lock` as the timers' jobs do, so the second line stops the
  services too.
- **Roll back a bad deploy.** A bad image goes back by pinning the last good one:
  1. Pause the timers and stop their services, as in Pause for maintenance above.
  2. Find the last good commit's full SHA in the repository's history. Each image carries its commit's SHA as a tag.
  3. Add `KCC_IMAGE_TAG=<sha>` to `.env`.
  4. If the bad version changed the database, as an Umbraco upgrade does, put back the snapshot the deploy took
     first:

     ```bash
     sudo -u kcc -H kcc stop app
     sudo -u kcc -H kcc run --rm --entrypoint ls backup /srv/kcc/snapshots/pre-deploy
     sudo -u kcc -H kcc run --rm backup rollback Umbraco-<time>.sqlite.db
     ```

  5. Run `sudo -u kcc -H kcc up -d --wait`, then resume the timers.
  6. Once `main` has the fix, take the line out of `.env`, then run `sudo -u kcc -H kcc pull app ssr backup` and
     `sudo -u kcc -H kcc up -d --wait`. The deploy timer never reacts to `.env` alone.

  A bad change under `deploy/`, such as the compose file, is not an image, so no tag rolls it back. Revert it on
  `main`. Once the Pi has pulled the revert (`sudo -u kcc git -C /srv/kcc/repo log -1 --oneline` shows it), run
  `sudo -u kcc -H kcc up -d --wait`. The timer does not do it for you: a revert of a change that never deployed leaves
  `deploy/` exactly as the Pi last deployed it, so the timer sees nothing to change, and the stack keeps the failed
  configuration until you run the command.
- **Restore from the bucket,** when the database is lost or damaged:

  ```bash
  sudo systemctl stop kcc-deploy.timer kcc-backup.timer
  sudo systemctl stop kcc-deploy.service kcc-backup.service
  sudo -u kcc -H kcc stop app
  sudo -u kcc -H kcc run --rm restore latest
  sudo -u kcc -H kcc up -d --wait
  sudo systemctl start kcc-deploy.timer kcc-backup.timer
  ```

  Name an archive instead of `latest` to go further back, such as `daily/kcc-2026-10-01.tar.gz` or
  `weekly/kcc-2026-09-27.tar.gz`. To list them:
  `sudo -u kcc -H kcc run --rm --entrypoint sh backup -c 'rclone lsf -R "bucket:$KCC_BACKUP_BUCKET"'`.
- **Production data down to your computer** (spec §12), over Tailscale.
  1. On the Pi:

     ```bash
     sudo -u kcc -H kcc run --rm backup snapshot
     sudo -u kcc -H kcc run --rm -T --entrypoint sh backup -c \
       'cat "$(find /srv/kcc/snapshots/pre-deploy -name "*.sqlite.db" | sort | tail -n 1)"' >/tmp/Umbraco.sqlite.db
     sudo -u kcc -H kcc run --rm -T --entrypoint tar backup -czf - -C /srv/kcc media >/tmp/kcc-media.tar.gz
     ```

  2. On your computer, with the development site stopped, from the repository's root:

     ```bash
     mkdir -p src/KCC.Web/umbraco/Data
     rm -rf src/KCC.Web/umbraco/Data/TEMP
     rm -f src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*
     scp '<you>@kcc:/tmp/Umbraco.sqlite.db' src/KCC.Web/umbraco/Data/
     scp '<you>@kcc:/tmp/kcc-media.tar.gz' .
     rm -rf src/KCC.Web/wwwroot/media && tar -xzf kcc-media.tar.gz -C src/KCC.Web/wwwroot && rm kcc-media.tar.gz
     ```

     The database lands straight in a folder git ignores, so members' data never sits loose in the repository's root.
     Development's `TEMP` holds indexes of the old database, so it goes too, as it does in a restore on the Pi.

  3. Delete the copies on the Pi: `sudo rm /tmp/Umbraco.sqlite.db /tmp/kcc-media.tar.gz`. They hold members' data.

  The development site then signs you in as the production administrator.
- **Updates.** Dependabot opens no pull requests. Its alerts flag a vulnerable npm, NuGet or Actions package: bump it
  by hand, and merging the bump deploys it. Alerts follow security advisories only, so Umbraco's other 17.x patches
  come unannounced, and Docker images have no alerts at all. Now and then, check for new releases of Umbraco, of the
  images in both Dockerfiles' `FROM` lines, and of cloudflared and Caddy under `deploy/`. The OS updates itself; reboot
  now and then for a new kernel (`sudo reboot`), and the stack comes back on its own.
- **Rotate the GHCR token** before it expires: make a new classic token (section 6), then run
  `sudo -u kcc -H docker login ghcr.io -u <your GitHub username>` again.

## 9. The restore drill

Once a quarter, and before launch, restore the latest backup from the bucket into a throwaway stack on your computer
(Docker Desktop on an Apple silicon Mac runs the same arm64 images).

1. Sign in to GHCR with the classic token: `docker login ghcr.io -u <your GitHub username>`.
2. Outside the repository, create `~/kcc-drill/drill.env`:

   ```sh
   KCC_HOST=localhost
   KCC_IMAGING_HMAC_KEY=<output of: openssl rand -base64 64 | tr -d '\n'>
   KCC_TUNNEL_TOKEN=unused
   KCC_FIRST_BOOT_ENV=/Users/<you>/kcc-drill/no-first-boot.env
   KCC_BACKUP_BUCKET=kcc-backups
   KCC_BACKUP_ENDPOINT=https://<account id>.r2.cloudflarestorage.com
   KCC_BACKUP_ACCESS_KEY_ID=<the read-only token's id>
   KCC_BACKUP_SECRET_ACCESS_KEY=<the read-only token's secret>
   ```

   `KCC_FIRST_BOOT_ENV` names a file that never exists, written out as a full path, so that a `first-boot.env` in the
   checkout cannot turn the install on and make the drill's boot prove nothing.
3. From `deploy/` in a checkout of `main`:

   ```bash
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml run --rm restore latest
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml up -d --wait
   ```

4. Open `https://localhost:8443` and accept the local certificate. Check:
   - the home page and a recipe, with its image;
   - `/umbraco`, where you sign in as the production administrator and find your content.
5. Clean up:

   ```bash
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml --profile jobs down -v
   rm -rf ~/kcc-drill
   ```

Note the date and the archive's name. A drill that fails is the most useful thing this runbook can tell you.

## 10. Moving to a VPS

The same compose file runs on any Linux host with Docker (spec §13.7).
- An x86 host needs amd64 images. Add `"linux/amd64"` to `platforms` in `docker-bake.hcl`. The images job then has to
  build on QEMU and push both platforms in one step, because a multi-platform image cannot be loaded into the runner's
  Docker for the smoke test. Plan that change when it is needed.
- The new host uses the same tunnel token, and nothing in DNS changes: the tunnel is the route. So the Pi goes first
  (step 1 below), or the two would serve different databases.

Then, in this order:

1. **On the Pi, retire the timers and the tunnel, and take one last backup:**

   ```bash
   sudo systemctl disable --now kcc-deploy.timer kcc-backup.timer
   sudo systemctl stop kcc-deploy.service kcc-backup.service
   sudo -u kcc -H kcc stop cloudflared
   sudo systemctl start kcc-backup.service
   journalctl -u kcc-backup -n 20 --no-pager
   ```

   The timers go first, and their services with them, so that nothing on the Pi undoes the rest: `kcc-deploy` would
   fail every five minutes with `not healthy: cloudflared exited` and start the tunnel again at the next change, and
   `kcc-backup` would upload to the same `daily/kcc-<date>.tar.gz` names as the VPS, where the later upload wins. The
   tunnel stops before the backup, so no write reaches the Pi after it. The journal must end with
   `uploaded daily/kcc-<date>.tar.gz`. Leave the tunnel stopped: any `up` on the Pi starts it again.
2. **On the VPS, set the host up** as in section 3 from step 3 (steps 1 and 2 flash and fit the Pi's SSD). Skip step 5,
   Tailscale, if you do not want it, and in step 6 allow SSH from your own address instead of the LAN range.
3. **Write `.env` and sign in to GHCR** as in section 7, steps 1 and 3, with the same values as the Pi's. Skip
   `first-boot.env`: the restored database needs no installer.
4. **Restore the last archive, then boot:**

   ```bash
   sudo -u kcc -H kcc run --rm restore latest
   sudo -u kcc -H kcc up -d --wait
   ```

   `latest` is the archive step 1 just uploaded. The restore fills the new volumes before the first `up`, so the app
   boots on the restored database.
5. **Install and enable the timers** on the VPS as in section 7, step 7.

## 11. Troubleshooting

- **`app` unhealthy, or restarting.** Read `sudo -u kcc -H kcc logs --tail 200 app`.
  - If the first boot never logged `uSync First boot complete`, its import failed and will not run again. If the timers
    are already enabled, pause them and stop their services first (section 8), so that no deploy starts the stack again
    underneath you. Then run `sudo -u kcc -H kcc down` and `sudo -u kcc -H docker volume rm kcc_data`, fix the cause,
    boot again with `first-boot.env`, and resume the timers.
  - If Umbraco is at its installer, the database is missing: restore it (section 8).
- **`kcc-deploy` fails every five minutes with `not healthy: app unhealthy`,** or another service and state
  (`systemctl --failed` lists the unit, and `journalctl -u kcc-deploy -n 20 --no-pager` has the message). A deploy
  failed, or a service stopped. Read that service's logs (`sudo -u kcc -H kcc logs --tail 200 <service>`), then fix
  forward or roll back (section 8). The unit recovers by itself once every service is running and healthy.
- **Cloudflare error 1033 or 502.** The tunnel is not connected, or the app is not healthy. Check
  `sudo -u kcc -H kcc ps`, and `sudo -u kcc -H kcc logs --tail 50 cloudflared`.
- **The backoffice says "This server only accepts HTTPS requests",** or its sign-in loops. The app is not applying the
  tunnel's headers. cloudflared must be at `172.30.9.10`, and the app's `Hosting__TunnelAddress` in `compose.yaml` must
  say the same. To check the first:
  `sudo -u kcc -H docker inspect kcc-cloudflared-1 -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}'`.
- **The backoffice shows network errors after a day.** The Access session has expired, and the backoffice's API calls
  meet Cloudflare's sign-in instead of Umbraco. Reload the page and pass Access again.
- **healthchecks.io says a backup is late,** or failed: `journalctl -u kcc-backup -n 50 --no-pager`.
- **A backup, or the snapshot before a deploy, refuses the database.** The journal of `kcc-backup` or `kcc-deploy`
  says `/srv/kcc/data/Umbraco.sqlite.db is missing or empty`, `the copy of the database failed its integrity check` or
  `the copy of the database has no Umbraco schema`. The database is gone or damaged, and the backups refuse to replace
  good archives with it: restore it (section 8). A restore that finds the same in an archive says
  `the database in <archive> failed its integrity check` or `the database in <archive> has no Umbraco schema`, and
  touches nothing: name an earlier archive.
- **The nightly backup fails with `tar exited with status 1, so daily/kcc-<date>.tar.gz is incomplete`.** A media file
  changed while it was being read, such as an upload during the backup. The job deletes the incomplete archive and
  pings healthchecks.io's failure address, so it emails you. The next night's run repairs it; look further only if it
  repeats.
- **`Cache instruction sync did not complete`** in the log should never appear: `WriteLockedCacheInstructionService`
  (`src/KCC.Web/Features/Sqlite/`) exists to prevent it. If it does, the guard is missing and the site's cache updates
  pause for about 20 minutes, so find out why before anything else.
- **Pulls fail with `denied`.** The GHCR token has expired or lacks `read:packages`: rotate it (section 8).
- **The disk fills up.** `sudo -u kcc -H docker system df` shows what is using it. Container logs rotate by themselves,
  and each deploy prunes unused images.
