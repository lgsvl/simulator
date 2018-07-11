#!/bin/sh

set -eu

export HOME=/tmp

HOST_UID=`stat -c "%u" /mnt`
HOST_GID=`stat -c "%g" /mnt`
chown -R ${HOST_UID}:${HOST_GID} /tmp/.local
sudo -E -u \#${HOST_UID} -g \#${HOST_GID} /opt/Unity/Editor/Unity
