#!/bin/bash

set -eu

if [[ $EUID -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ ! -v UNITY_USERNAME ]; then
  echo "ERROR: UNITY_USERNAME environment variable is not set"
  exit 1
fi

if [ ! -v UNITY_PASSWORD ]; then
  echo "ERROR: UNITY_PASSWORD environment variable is not set"
  exit 1
fi

if [ ! -v UNITY_SERIAL ]; then
  echo "ERROR: UNITY_SERIAL environment variable is not set"
  exit 1
fi

if [ $# -ne 1 ]; then
  echo "ERROR: please specify OS!"
  echo "  windows - runs 64-bit Windows build"
  echo "  linux - runs 64-bit Linux build"
  echo "  macos - runs macOS build"
  exit 1
fi

export HOME=/tmp

function finish
{
  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-glcore \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

if [ "$1" == "windows" ]; then
  BUILD_TARGET=Win64
elif [ "$1" == "linux" ]; then
  BUILD_TARGET=Linux64
elif [ "$1" == "macos" ]; then
  BUILD_TARGET=OSXUniversal
else
  echo "Unknown command $1"
  exit 1
fi

/opt/Unity/Editor/Unity \
  -serial ${UNITY_SERIAL} \
  -username ${UNITY_USERNAME} \
  -password ${UNITY_PASSWORD} \
  -batchmode \
  -force-glcore \
  -silent-crashes \
  -quit \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildTarget ${BUILD_TARGET} \
  -buildOutput /mnt/AssetBundles \
  -skipPlayer \
  -logFile /dev/stdout
