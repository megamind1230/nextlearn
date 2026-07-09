#!/bin/sh
# Installs NextLearn icon + .desktop entry for Linux task bar
set -e
APP=nextlearn
ICON_SRC="$1"
DLL_PATH="$2"
PREFIX="${3:-$HOME/.local}"

# Icon
ICON_DIR="$PREFIX/share/icons/hicolor/256x256/apps"
mkdir -p "$ICON_DIR"
cp "$ICON_SRC" "$ICON_DIR/$APP.png"

# Launcher wrapper
BIN_DIR="$PREFIX/bin"
mkdir -p "$BIN_DIR"
WRAPPER="$BIN_DIR/$APP"
cat > "$WRAPPER" << SCRIPT
#!/bin/sh
exec dotnet "$DLL_PATH" "\$@"
SCRIPT
chmod +x "$WRAPPER"

# Desktop entry
APPS_DIR="$PREFIX/share/applications"
mkdir -p "$APPS_DIR"
cat > "$APPS_DIR/$APP.desktop" << EOF
[Desktop Entry]
Type=Application
Name=NextLearn
Comment=Let's Fight Distraction
Exec=$WRAPPER
Icon=$APP
Terminal=false
Categories=Education;Office;
EOF

# Update desktop database
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$APPS_DIR" || true
