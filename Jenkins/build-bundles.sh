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

if [ -z ${SIM_ENVIRONMENTS+x} ] && [ -z ${SIM_VEHICLES+x} ] && [ -z ${SIM_CONTROLLABLES+x} ]; then
  echo All environments,vehicles amd controllables are up to date!
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

export HOME=/tmp

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

if [ ! -z ${SIM_CONTROLLABLES+x} ]; then
  getAssets "${SIM_CONTROLLABLES}"
  CONTROLLABLES="-buildControllables ${ASSETS}"
else
  CONTROLLABLESS=
fi

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
  -force-glcore \
  -silent-crashes \
  -quit \
  -projectPath /mnt \
  -executeMethod Simulator.Editor.Build.Run \
  -buildBundles \
  ${ENVIRONMENTS} \
  ${VEHICLES} \
  ${CONTROLLABLES} \
  ${SENSORS} \
  -logFile /dev/stdout
