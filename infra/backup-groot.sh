#!/usr/bin/env bash
# Nightly Groot backup: consistent SQLite snapshot + 14-day retention.
set -euo pipefail

DATA=/opt/groot/data
DEST=/opt/groot/backups
STAMP=$(date +%Y%m%d)

mkdir -p "$DEST"
sqlite3 "$DATA/groot.db" ".backup '$DEST/groot-$STAMP.db'"
gzip -f "$DEST/groot-$STAMP.db"

find "$DEST" -type f -mtime +14 -delete

# off-box copy (uncomment and point somewhere that is not this VPS):
# rsync -a "$DEST/" backup-target:/backups/groot/
