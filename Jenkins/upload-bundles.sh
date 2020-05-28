#!/bin/sh

set -eu
set -x

if [[ `id -u` -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

BUNDLES=/mnt/AssetBundles

function uploadAssets()
{
  local SOURCE_FOLDER=$2
  local PREFIX=$3
  echo "$1" | while IFS= read -r LINE ; do
    local ID="${LINE%% *}"
    local NAME="${LINE#* }"
    aws s3 cp "${SOURCE_FOLDER}/${PREFIX}_${NAME}" s3://${S3_BUCKET_NAME}/v2/${ID}/
  done
}

###

if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
  uploadAssets "${SIM_ENVIRONMENTS}" ${BUNDLES}/Environments environment
fi

if [ ! -z ${SIM_VEHICLES+x} ]; then
  uploadAssets "${SIM_VEHICLES}" ${BUNDLES}/Vehicles vehicle
fi
