#!/bin/sh

#set -eu
#set -x

if [ `id -u` -eq 0 ] ; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ -z "$WISE_USERNAME" ] ; then
  echo "ERROR: WISE_USERNAME is empty"
  exit 2
fi
if [ -z "$WISE_PASSWORD" ] ; then
  echo "ERROR: WISE_PASSWORD is empty"
  exit 2
fi
if [ -z "$WISE_URL" ] ; then
  echo "ERROR: WISE_URL is empty"
  exit 2
fi

WISE_CLI="nodejs /app/index.js"
BUNDLES=/mnt/AssetBundles
BUNDLES_SOURCES=/mnt/Assets/External
RESULT=0
ASSETS=0

function uploadAssets()
{
  local ASSET_FOLDER=$2
  local ASSET_PREFIX=$3
  local ASSET_TYPE=$4
  # we don't need ASSET_IDs here
  local ASSET_NAMES=`echo "$1" | awk '{print $2}'`
  for ASSET_NAME in $ASSET_NAMES; do
    ASSETS=`expr $ASSETS + 1`
    # We need to get the name from description.json, because e.g. asset_name SanFrancisco
    # when uploaded with current description.json will have a name shown by wise-cli list
    # as "San Francisco" and because the extra space, the grep doesn't catch it and
    # it's not deleted before upload
    ASSET_REAL_NAME=`jq .name ${BUNDLES_SOURCES}/${ASSET_FOLDER}/${ASSET_NAME}/description.json`
    echo "INFO: Check if '$ASSET_NAME' asset is already uploaded with the name from description.json: '$ASSET_REAL_NAME'"
    # There are already quotes around the asset name from jq output, which is good, because then
    # this grep won't so easily match with some other asset which contains ASSET_REAL_NAME as substring
    # just add spaces around as well
    local ASSET_UID=`$WISE_CLI list -t $ASSET_TYPE | grep " ${ASSET_REAL_NAME} " | awk '{print $1}'`

    if [ -n "$ASSET_UID" ]; then
      echo "INFO: $ASSET_NAME: Asset already uploaded"
      echo "INFO: Removing already uploaded version"
      if ! $WISE_CLI delete -t $ASSET_TYPE --id $ASSET_UID; then
        echo "ERROR: Failed to delete asset: $ASSET_NAME"
        RESULT=`expr $RESULT + 1`
      fi
    fi

    echo "INFO: $ASSET_NAME: Uploading the asset"
    if ! $WISE_CLI upload -t $ASSET_TYPE -a ${BUNDLES}/${ASSET_FOLDER}/${ASSET_PREFIX}_${ASSET_NAME} -d ${BUNDLES_SOURCES}/${ASSET_FOLDER}/${ASSET_NAME}/description.json; then
      echo "ERROR: Failed to upload asset: $ASSET_NAME"
      RESULT=`expr $RESULT + 1`
    fi
  done
}

echo "INFO: Login into WISE:$WISE_URL just to test if provided username/password works"
if ! $WISE_CLI login --url $WISE_URL; then
        echo "ERROR: Failed to login to WISE:$WISE_URL"
        exit 1
fi

if [ ! -z ${SIM_ENVIRONMENTS+x} ]; then
  uploadAssets "${SIM_ENVIRONMENTS}" Environments environment map
fi

if [ ! -z ${SIM_VEHICLES+x} ]; then
  uploadAssets "${SIM_VEHICLES}" Vehicles vehicle vehicle
fi

echo "INFO: ${ASSETS} assets were processed"

if [ "${RESULT}" -ne 0 ] ; then
  echo "ERROR: there were ${RESULT} errors while processing them"
fi

exit ${RESULT}
