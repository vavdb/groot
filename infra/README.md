# Infra: PocketBase on the VPS

Target: one VPS, one PocketBase binary, SQLite on disk, Caddy in front with automatic HTTPS,
nightly backup. No containers required; a systemd unit is the whole deployment.

## Install (once)

```bash
# as root on the VPS
useradd --system --home /opt/groot --shell /usr/sbin/nologin groot
mkdir -p /opt/groot && cd /opt/groot
# check latest: https://github.com/pocketbase/pocketbase/releases
curl -fsSL -o pb.zip https://github.com/pocketbase/pocketbase/releases/latest/download/pocketbase_linux_amd64.zip
unzip pb.zip && rm pb.zip && chown -R groot:groot /opt/groot
cp /path/to/repo/infra/groot-pb.service /etc/systemd/system/
systemctl enable --now groot-pb
/opt/groot/pocketbase superuser create admin@example.com   # prompts for password
```

## Caddy

Add `infra/Caddyfile.snippet` to the existing Caddyfile and reload. Caddy handles the certificate.

## Backup

`infra/backup-groot.sh` in root's crontab, nightly:

```
15 3 * * * /opt/groot/backup-groot.sh
```

PocketBase keeps SQLite in `/opt/groot/pb_data`. The script snapshots with sqlite3 `.backup`
(safe while running) and keeps 14 days. Off-box copy: rsync target of your choice, see the
commented line in the script.

## App collections (created via admin UI or migration once the app lands)

`users` (PocketBase built-in auth: username/password now; enable Google + Apple providers later),
`workouts`, `sets`, `weeks`, `programs` (for MVP++ downloadable definitions), all with owner-only
API rules: `@request.auth.id != "" && user = @request.auth.id`.
