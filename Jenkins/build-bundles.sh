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

if [ ! -v UNITY_VERSION ]; then
  echo "ERROR: UNITY_VERSION environment variable is not set"
  exit 1
fi

if [ -z ${SIM_ENVIRONMENTS+x} ] && [ -z ${SIM_VEHICLES+x} ] && [ -z ${SIM_SENSORS+x} ] && [ -z ${SIM_BRIDGES+x} ]; then
  echo All environments, vehicles, sensors and bridges are up to date!
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

UNITY=/opt/Unity/Editor/Unity

if [ ! -x "${UNITY}" ] ; then
    echo "ERROR: ${UNITY} doesn't exist or isn't executable"
    exit 1
fi

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

if [ ! -z ${SIM_SENSORS+x} ]; then
  getAssets "${SIM_SENSORS}"
  SENSORS="-buildSensors ${ASSETS}"
else
  SENSORS=
fi

if [ ! -z ${SIM_BRIDGES+x} ]; then
  getAssets "${SIM_BRIDGES}"
  BRIDGES="-buildBridges ${ASSETS}"
else
  BRIDGES=
fi

function get_unity_license {
    echo "Fetching unity license"

    ${UNITY} \
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
  ${UNITY} \
    -batchmode \
    -force-vulkan \
    -silent-crashes \
    -quit \
    -returnlicense
}
trap finish EXIT

get_unity_license

PREFIX=svlsimulator

if [ -v GIT_TAG ]; then
  SUFFIX=${GIT_TAG}
elif [ -v JENKINS_BUILD_ID ]; then
  SUFFIX=${JENKINS_BUILD_ID}
else
  SUFFIX=
fi

echo "I: Cleanup AssetBundles before build"

rm -Rf /mnt/AssetBundles || true

${UNITY} \
  -force-vulkan \
  -silent-crashes \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildBundles \
  ${ENVIRONMENTS} \
  ${VEHICLES} \
  ${SENSORS} \
  ${BRIDGES} \
  -logFile /dev/stdout | tee unity-build-bundles.log

check_unity_log unity-build-bundles.log
