# Runs one CodeRabbit round under a machine-wide lock.
#
# PowerShell twin of review-run.sh. Keep the two in step.
#
#   review-run.ps1 <repo> review --agent --base-commit <base> -c <files>...
#
# The tool keeps one cache under ~/.coderabbit/reviews for every review on the
# machine, and a stale cache entry fails a later round with "is outside
# repository". Clearing it while another agent reviews stops that review. So
# each round takes the lock, clears the cache, runs in <repo>, and releases the
# lock. One review at a time also keeps parallel agents under the rate limit.
#
# Output goes straight through, so redirect it to a file. The exit code is the
# tool's, or 5 when the lock stays busy for 30 minutes.
$repo = $args[0]
$rest = @($args | Select-Object -Skip 1)
$home_ = [Environment]::GetFolderPath('UserProfile')
$lock = Join-Path $home_ '.coderabbit/bizstream.lock'
New-Item -ItemType Directory -Force -Path (Join-Path $home_ '.coderabbit') | Out-Null
$waited = 0
while ($true) {
  # Creating a directory that exists throws, so this is the atomic take.
  try { New-Item -ItemType Directory -Path $lock -ErrorAction Stop | Out-Null; break } catch { }
  $holder = Get-Content -Path (Join-Path $lock 'pid') -ErrorAction SilentlyContinue
  $age = ((Get-Date) - (Get-Item $lock -ErrorAction SilentlyContinue).LastWriteTime).TotalSeconds
  # A holder that died, or a lock older than 30 minutes, is stale.
  $dead = $holder -and -not (Get-Process -Id $holder -ErrorAction SilentlyContinue)
  if ($dead -or $age -gt 1800) { Remove-Item -Recurse -Force $lock -ErrorAction SilentlyContinue; continue }
  if ($waited -ge 1800) { [Console]::Error.WriteLine("the CodeRabbit lock stayed busy for 30 minutes: $lock"); exit 5 }
  if ($waited -eq 0) { [Console]::Error.WriteLine('waiting for another CodeRabbit review to finish') }
  Start-Sleep -Seconds 10; $waited += 10
}
Set-Content -Path (Join-Path $lock 'pid') -Value $PID
$code = 4
try {
  Remove-Item -Recurse -Force (Join-Path $home_ '.coderabbit/reviews') -ErrorAction SilentlyContinue
  Set-Location $repo -ErrorAction Stop
  & coderabbit @rest
  $code = $LASTEXITCODE
} finally { Remove-Item -Recurse -Force $lock -ErrorAction SilentlyContinue }
exit $code
