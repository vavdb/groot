#!/usr/bin/env bash
# kanban.sh – tiny kanban helper for ~/dev/Groot/Plan/
# Board = one markdown file per card, status lives in frontmatter (no column dirs).

BASE="$(dirname "$(realpath "$0")")"
PLAN_DIR="${BASE}"
CARD_TEMPLATE="${BASE}/card-template.md"

usage() {
  cat <<EOF
Kanban board helper for the frontmatter-status board described in CLAUDE.md.

  list               – list all cards
  board              – group cards by status (backlog / doing / done)
  status <file>      – show a card's status
  set <file> <status> – set a card's status (backlog|doing|done)
  create <slug>      – copy CARD_TEMPLATE and rename it with <slug>
  delete <file>      – remove a card (confirms first)
  tags <file>        – show space-separated list of tags
  edit <file>        – open the card in \$EDITOR (or \$VISUAL)
  search <term>      – grep the body of all cards for <term>
EOF
}

if [[ ! -d "${PLAN_DIR}" ]]; then
  echo "❌ Plan directory not found at ${PLAN_DIR}" >&2; exit 1
fi

case "$1" in
  list)
    find "${PLAN_DIR}" -maxdepth 1 -type f -name "*.md" ! -name "card-template.md" | sort
    ;;
  board)
    for s in backlog doing done; do
      echo "== ${s} =="
      grep -l "^status: \"${s}\"" "${PLAN_DIR}"/*.md 2>/dev/null | grep -v card-template.md | sort
    done
    ;;
  status)
    file="$2"
    [[ -z "$file" ]] && { echo "❌ missing file"; exit 1; }
    grep -m1 "^status:" "$file" 2>/dev/null | cut -d: -f2- | tr -d ' "'
    ;;
  set)
    file="$2"; status="$3"
    [[ -z "$file" || -z "$status" ]] && { echo "❌ usage: set <file> <backlog|doing|done>"; exit 1; }
    sed -i "s/^status: .*/status: \"${status}\"/" "$file"
    echo "✅ ${file} → ${status}"
    ;;
  create)
    slug="${2:?usage: $0 create <slug>}"
    dst="${PLAN_DIR}/${slug}.md"
    if [[ -f "${dst}" ]]; then
      echo "❌ File already exists: ${dst}" >&2; exit 1
    fi
    cp "${CARD_TEMPLATE}" "${dst}"
    echo "✅ created ${dst}"
    ;;
  delete)
    file="$2"
    [[ -z "$file" ]] && { echo "❌ missing file"; exit 1; }
    read -p "⚠️  really delete ${file}? (y/N) " ans
    [[ "$ans" == "y" ]] && rm -f "$file" && echo "🗑 deleted ${file}" || echo "Cancelled"
    ;;
  tags)
    file="$2"
    [[ -z "$file" ]] && { echo "❌ missing file"; exit 1; }
    grep "^tags:" "$file" 2>/dev/null | cut -d: -f2- | tr -d '"' | tr ',' ' '
    ;;
  edit)
    file="$2"
    [[ -z "$file" ]] && { echo "❌ missing file"; exit 1; }
    ${EDITOR:-nano} "$file"
    ;;
  search)
    term="${2:?usage: $0 search <term>}"
    grep -Ri --include="*.md" "$term" "${PLAN_DIR}"
    ;;
  *)
    usage
    ;;
esac
