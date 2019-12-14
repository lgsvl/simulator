#!/bin/bash

set -eu

if [[ $EUID -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ $# -ne 1 ]; then
  echo "ERROR: please specify argument:"
  echo "  pack - build development mode"
  echo "  pack-p - build production mode"
  exit 1
fi

if [ "$1" != "pack" ] && [ "$1" != "pack-p" ]; then
  echo "Unknown argument $1"
  exit 1
fi

export HOME=/tmp

cd /mnt/WebUI
npm install
npm run $1
