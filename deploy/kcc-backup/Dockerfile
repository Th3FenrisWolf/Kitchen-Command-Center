FROM alpine:3.24.2
# The volumes mount under /srv/kcc. An empty volume takes the content and owner of the directory it is mounted on, so
# these are empty and belong to the app's user (1654), who owns the files in them.
RUN apk add --no-cache sqlite rclone curl tar gzip \
    && addgroup -g 1654 kcc && adduser -D -u 1654 -G kcc kcc \
    && mkdir -p /srv/kcc/data /srv/kcc/media /srv/kcc/snapshots && chown -R kcc:kcc /srv/kcc
# The bucket is configured through RCLONE_CONFIG_BUCKET_* variables, so rclone needs no config file.
ENV RCLONE_CONFIG=/dev/null
COPY --chmod=755 kcc-backup /usr/local/bin/kcc-backup
USER kcc
ENTRYPOINT ["kcc-backup"]
CMD ["nightly"]
