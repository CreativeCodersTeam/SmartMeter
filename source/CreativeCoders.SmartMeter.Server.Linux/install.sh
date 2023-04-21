#!/bin/bash

SERVICE_NAME="smartmeter-server"
APP_DIR="/opt/smartmetersrv"
USER_NAME="smartmeter-user"

if systemctl status "$SERVICE_NAME" >/dev/null 2>&1; then
  echo "Stop existing systemd service"
  systemctl stop "$SERVICE_NAME"
  systemctl disable "$SERVICE_NAME"
fi

if [ -d "$APP_DIR" ]; then
  echo "Delete existing installation files"
  rm -rf "${APP_DIR:?}/"*
else
  echo "Create installation target directory"
  mkdir "$APP_DIR"
fi

echo "Copy all files to installation target directory"
cp -R -f . "$APP_DIR"

if id -u "$USER_NAME" >/dev/null 2>&1; then
  echo "User $USER_NAME exists."
else
  echo "Create user for service and installation"
  useradd -r -s /bin/false "$USER_NAME"
fi

echo "Change files owner"
chown -R "$USER_NAME:$USER_NAME" "${APP_DIR:?}/"*
chown "$USER_NAME:$USER_NAME" "${APP_DIR:?}"

echo ""
dotnet "$APP_DIR"/smartmetersrv.dll --install