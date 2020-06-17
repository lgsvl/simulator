#!/bin/bash

set -euo pipefail

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

if [ -z ${SIM_ENVIRONMENTS+x} ] && [ -z ${SIM_VEHICLES+x} ]; then
  echo All environments and vehicles are up to date!
  exit 0
fi

function getAssets()
{
  ASSETS=
  local DELIM=
  while read -r LINE; do
    local ITEMS=( ${LINE} )
    local NAME="${ITEMS[1]}"
    ASSETS="${ASSETS}${DELIM}${NAME}"
    DELIM=","
  done <<< "$1"
}

CHECK_UNITY_LOG=$(readlink -f "$(dirname $0)/check-unity-log.sh")

function check_unity_log {
    ${CHECK_UNITY_LOG} $@
}

export HOME=/tmp

cd /mnt

###

if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
  getAssets "${SIM_ENVIRONMENTS}"
  ENVIRONMENTS="-buildEnvironments ${ASSETS}"
else
  ENVIRONMENTS=
fi

if [ ! -z ${SIM_VEHICLES+x} ]; then
  getAssets "${SIM_VEHICLES}"
  VEHICLES="-buildVehicles ${ASSETS}"
else
  VEHICLES=
fi

function get_unity_license {
    echo "Fetching unity license"

    /opt/Unity/Editor/Unity \
        -logFile /dev/stdout \
        -batchmode \
        -serial ${UNITY_SERIAL} \
        -username ${UNITY_USERNAME} \
        -password ${UNITY_PASSWORD} \
        -projectPath /mnt \
        -quit
}

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

get_unity_license

PREFIX=lgsvlsimulator

if [ -v GIT_TAG ]; then
  SUFFIX=${GIT_TAG}
elif [ -v JENKINS_BUILD_ID ]; then
  SUFFIX=${JENKINS_BUILD_ID}
else
  SUFFIX=
fi

echo "I: Cleanup AssetBundles before build"

rm -Rf /mnt/AssetBundles || true

/opt/Unity/Editor/Unity \
  -force-vulkan \
  -silent-crashes \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildBundles \
  ${ENVIRONMENTS} \
  ${VEHICLES} \
  -logFile /dev/stdout | tee unity-build-bundles.log

check_unity_log unity-build-bundles.log
