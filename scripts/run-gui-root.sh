#!/usr/bin/env bash
#
# Launches SharpDisk.Gui as root with a graphical password prompt.
#
# Raw access to /dev/nvme0n1 and friends needs root; listing drives via
# /sys/class/block does not. Until the udisks2/D-Bus route is built, this is how
# you run the whole thing privileged.
#
# Usage:
#   scripts/run-gui-root.sh [--dry-run] [-- <args passed to SharpDisk.Gui>]
#
# Override the binary with SHARPDISK_GUI=/path/to/SharpDisk.Gui.

set -euo pipefail

DRY_RUN=0
if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=1
    shift
fi
[[ "${1:-}" == "--" ]] && shift

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname -- "$SCRIPT_DIR")"
APP="${SHARPDISK_GUI:-$REPO_ROOT/SharpDisk.Gui/bin/Debug/net10.0/SharpDisk.Gui}"

die() { printf 'run-gui-root: %s\n' "$1" >&2; exit 1; }

[[ -x "$APP" ]] || die "not found or not executable: $APP
build it first:  dotnet build SharpDisk.Gui/SharpDisk.Gui.csproj"

# Already root: nothing to do.
if [[ "$(id -u)" -eq 0 ]]; then
    (( DRY_RUN )) && { echo "already root -> exec $APP"; exit 0; }
    exec "$APP" "$@"
fi

# --- the X11 part -------------------------------------------------------------
# sudo wipes the environment, and a GTK/WebKit process with no DISPLAY or no
# auth cookie dies with "cannot open display". Both are passed explicitly below
# rather than via -E, which sudoers is free to ignore.
[[ -n "${DISPLAY:-}" || -n "${WAYLAND_DISPLAY:-}" ]] || die "no DISPLAY/WAYLAND_DISPLAY - run this from a graphical session"

XAUTH="${XAUTHORITY:-$HOME/.Xauthority}"
[[ -f "$XAUTH" ]] || XAUTH=""

# --- how do we ask for the password? ------------------------------------------
# pkexec is the nicer answer, but only when a polkit authentication agent is
# actually running. Tiling WMs (awesome, i3, sway...) do not start one, and
# without an agent pkexec from a GUI fails silently with no dialog at all.
# So: use it when an agent is up, fall back to sudo with a zenity/yad askpass.
agent_running() {
    pgrep -f 'polkit-[a-z]*-authentication-agent' >/dev/null 2>&1
}

pick_askpass_tool() {
    for tool in zenity yad; do
        command -v "$tool" >/dev/null 2>&1 && { echo "$tool"; return 0; }
    done
    return 1
}

run_pkexec() {
    local cmd=(pkexec env DISPLAY="${DISPLAY:-}")
    [[ -n "$XAUTH" ]] && cmd+=(XAUTHORITY="$XAUTH")
    cmd+=("$APP" "$@")

    (( DRY_RUN )) && { echo "would run: ${cmd[*]}"; return 0; }
    exec "${cmd[@]}"
}

run_sudo_askpass() {
    local tool="$1"; shift

    if (( DRY_RUN )); then
        echo "would run: sudo -A -- env DISPLAY=${DISPLAY:-} ${XAUTH:+XAUTHORITY=$XAUTH} $APP $*"
        echo "           (askpass via $tool)"
        return 0
    fi

    # sudo -A execs SUDO_ASKPASS with no arguments and reads the password from
    # its stdout, so the prompt has to be its own little program.
    local askpass
    askpass="$(mktemp --tmpdir sharpdisk-askpass.XXXXXX.sh)"
    trap 'rm -f "$askpass"' EXIT

    case "$tool" in
        zenity)
            cat >"$askpass" <<'SHIM'
#!/bin/sh
exec zenity --password --title="SharpDisk needs root" 2>/dev/null
SHIM
            ;;
        yad)
            cat >"$askpass" <<'SHIM'
#!/bin/sh
exec yad --entry --hide-text --title="SharpDisk needs root" --text="Password:" 2>/dev/null
SHIM
            ;;
    esac
    chmod 700 "$askpass"

    local cmd=(env DISPLAY="${DISPLAY:-}")
    [[ -n "$XAUTH" ]] && cmd+=(XAUTHORITY="$XAUTH")
    cmd+=("$APP" "$@")

    # Deliberately not preserving HOME: root gets /root, so WebKit's caches do
    # not land in your home directory owned by uid 0.
    SUDO_ASKPASS="$askpass" sudo -A -- "${cmd[@]}"
}

if agent_running; then
    run_pkexec "$@"
elif tool="$(pick_askpass_tool)"; then
    run_sudo_askpass "$tool" "$@"
else
    # No agent, no graphical prompt available - ask on the terminal instead.
    (( DRY_RUN )) && { echo "would run: sudo -- env DISPLAY=... $APP $* (terminal prompt)"; exit 0; }
    printf 'run-gui-root: no polkit agent and no zenity/yad - asking on this terminal\n' >&2
    exec sudo -- env DISPLAY="${DISPLAY:-}" ${XAUTH:+XAUTHORITY="$XAUTH"} "$APP" "$@"
fi
