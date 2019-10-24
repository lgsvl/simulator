#!/bin/sh

set -eu

if [[ `id -u` -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

BUNDLES=/mnt/AssetBundles

function uploadAssets()
{
  local PREFIX=$2
  echo "$1" | while IFS= read -r LINE ; do
    local ID="${LINE%% *}"
    local NAME="${LINE#* }"
    aws s3 cp "${BUNDLES}/${PREFIX}_${NAME}" s3://${S3_BUCKET_NAME}/${ID}/
  done
}

###

if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
  uploadAssets "${SIM_ENVIRONMENTS}" environment
fi

if [ ! -z ${SIM_VEHICLES+x} ]; then
  uploadAssets "${SIM_VEHICLES}" vehicle
fi
