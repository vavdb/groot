#!/usr/bin/env bash
# Mechanical checks for the rules in AGENTS.md. CI runs this; run it before a commit:
#   bash tools/check-rules.sh
# Prints one RULE line per violation (with the offending lines above it) and exits 1.
set -u
cd "$(dirname "$0")/.." || exit 1
export LC_ALL=C

status=0
fail() { printf 'RULE  %s\n\n' "$1"; status=1; }

components=(src/Groot.UI/Components/*.css)
dash=$(printf '\xe2\x80\x94')

# Component CSS: every colour is a token. A tint is color-mix() on a token, never a literal.
if grep -n -E '#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(' "${components[@]}"; then
    fail "colour literal in component CSS; use var(--g-*) or color-mix(in srgb, var(--g-*) N%, transparent)"
fi

# Component CSS: font sizes are steps on the type scale, families are tokens.
if grep -n -E 'font-size:[[:space:]]*[0-9.]+(px|pt|r?em)' "${components[@]}"; then
    fail "font-size literal in component CSS; use var(--g-text-*)"
fi
if grep -n -E 'font-family:[[:space:]]*"?(Fraunces|Public Sans)' "${components[@]}"; then
    fail "font family literal in component CSS; use var(--g-font-display) / var(--g-font-ui)"
fi

# Every custom interactive element has a visible focus style.
for razor in src/Groot.UI/Components/*.razor; do
    if grep -q -E '<button|<a |@onclick|tabindex=' "$razor"; then
        css="${razor}.css"
        if [ ! -f "$css" ] || ! grep -q 'focus-visible' "$css"; then
            fail "$razor renders an interactive element but ${css##*/} has no :focus-visible rule"
        fi
    fi
done

# Every animation respects prefers-reduced-motion.
for css in "${components[@]}"; do
    if grep -q -E 'animation:|transition:' "$css" && ! grep -q 'prefers-reduced-motion' "$css"; then
        fail "$css animates without a prefers-reduced-motion block"
    fi
done

# No async void: an exception must reach Blazor's error boundary, not the thread pool.
if grep -rn --include='*.cs' --include='*.razor' -E '\basync void\b' src; then
    fail "async void; use async Task (EventCallback and @bind:after await it)"
fi

# Copy: no em dashes in user-facing or spoken strings (design/habit-system.md 5b).
# Comments are stripped first; a bare dash glyph used as a value ("—") is allowed.
for file in src/Groot.UI/Components/*.razor src/Groot.Core/Intervals/RunCueText.cs; do
    if perl -0777 -pe 's/\@\*.*?\*\@//gs; s{/\*.*?\*/}{}gs; s{(^|[[:space:]])//[^\n]*}{}mg' "$file" \
        | grep -n -E "[A-Za-z][[:space:]]*${dash}|${dash}[[:space:]]*[A-Za-z]"; then
        fail "$file: em dash in copy; write two sentences or use a comma"
    fi
done

if [ "$status" -eq 0 ]; then
    echo "check-rules: clean"
fi
exit "$status"
