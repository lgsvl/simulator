#!/bin/sh

set -eu

if [[ `id -u` -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

BUNDLES=/mnt/AssetBundles

for file in "${BUNDLES}"/* ;
do
  name="${file##*/}"
  extension="${name##*.}"
  if [[ "${extension}" != "manifest" ]] ;
  then
    if [[ "${name}" != "${name#vehicle_}" ]] || 
       [[ "${name}" != "${name#environment_}" ]] ;
    then
      aws s3 cp "${file}" s3://${S3_BUCKET_NAME}/${GIT_COMMIT}/
    fi
  fi
done

