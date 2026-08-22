#!/usr/bin/env bash
# PreToolUse hook for Edit|Write|MultiEdit: refuses edits to generated files.
# tokens.css comes from src/Groot.UI/Theme/GrootPalette.cs; scoped CSS bundles, obj/ and bin/
# are build output. See AGENTS.md, "Working in this repo".
input=$(cat)
path=$(printf '%s' "$input" \
    | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' \
    | head -n 1 \
    | tr -s '\\' '/')

[ -z "$path" ] && exit 0

case "$path" in
    */wwwroot/tokens.css|*.styles.css|*/obj/*|*/bin/*)
        echo "Blocked: '$path' is generated. Edit src/Groot.UI/Theme/GrootPalette.cs and rebuild src/Groot.UI instead (AGENTS.md)." >&2
        exit 2
        ;;
esac

exit 0
