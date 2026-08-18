#!/usr/bin/env bash
# Nightly PocketBase backup: consistent SQLite snapshot + 14-day retention.
set -euo pipefail

DATA=/opt/groot/pb_data
DEST=/opt/groot/backups
STAMP=$(date +%Y%m%d)

mkdir -p "$DEST"
sqlite3 "$DATA/data.db" ".backup '$DEST/data-$STAMP.db'"
[ -f "$DATA/auxiliary.db" ] && sqlite3 "$DATA/auxiliary.db" ".backup '$DEST/aux-$STAMP.db'"
tar -czf "$DEST/pb_data-$STAMP.tar.gz" -C /opt/groot --exclude=pb_data/backups pb_data

find "$DEST" -type f -mtime +14 -delete

# off-box copy (uncomment and point somewhere that is not this VPS):
# rsync -a "$DEST/" backup-target:/backups/groot/
