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

export HOME=/tmp

cd /mnt

function finish
{
  /opt/Unity/Editor/Unity \
    -batchmode \
    -force-vulkan \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

PREFIX=lgsvlsimulator

if [ -v GIT_TAG ]; then
  SUFFIX=${GIT_TAG}
elif [ -v JENKINS_BUILD_ID ]; then
  SUFFIX=${JENKINS_BUILD_ID}
else
  SUFFIX=
fi

/opt/Unity/Editor/Unity \
  -serial ${UNITY_SERIAL} \
  -username ${UNITY_USERNAME} \
  -password ${UNITY_PASSWORD} \
  -batchmode \
  -force-vulkan \
  -silent-crashes \
  -quit \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -saveBundleLinks /mnt/${PREFIX}-bundles-${SUFFIX}.html \
  -logFile /dev/stdout
