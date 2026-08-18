# Infra: Groot.Api on the VPS

Target: one VPS, one self-contained `dotnet publish` output, SQLite on disk, Caddy in front with
automatic HTTPS, nightly backup. A systemd unit is the whole deployment.

*(History: this folder originally described a PocketBase setup; replaced when the backend decision
reversed to an own Minimal API — research.md §10, 2026-08-18.)*

## Deploy (repeatable)

```bash
# on the dev machine
dotnet publish src/Groot.Api -c Release -r linux-x64 --self-contained -o out/api
rsync -a out/api/ the VPS:/opt/groot/api/

# once, as root on the VPS
useradd --system --home /opt/groot --shell /usr/sbin/nologin groot
mkdir -p /opt/groot/{api,data,backups} && chown -R groot:groot /opt/groot
cp /opt/groot/api/infra/groot-api.service /etc/systemd/system/   # or scp from repo
systemctl enable --now groot-api
```

## Caddy

Add `infra/Caddyfile.snippet` to the existing Caddyfile and reload; Caddy handles the certificate.
The API listens on localhost only; Caddy is the front door.

## Backup

`infra/backup-groot.sh` in root's crontab, nightly:

```
15 3 * * * /opt/groot/backup-groot.sh
```

The API keeps SQLite in `/opt/groot/data`. The script snapshots with sqlite3 `.backup` (safe while
running) and keeps 14 days. Off-box copy: uncomment the rsync line and point it somewhere that is
not this VPS.

## Secrets

`/opt/groot/api/appsettings.Production.json` (not in git): JWT signing key + connection string.
Generate the key once: `openssl rand -base64 48`.
