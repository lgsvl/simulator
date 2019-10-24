#!/bin/bash

set -euo pipefail

if [[ `id -u` -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ $# -ne 4 ]; then
  echo "ERROR: use $0 <folder> <url> <prefix> <all>"
  exit 1
fi

FOLDER=$1
URL=$2
PREFIX=$3
ALL=$4

for f in "${FOLDER}"/*/; do

  NAME=`basename "${f}"`
  if [[ ! "${NAME}" =~ "@tmp" ]]; then

    ID=`GIT_DIR="${f}/.git" git rev-parse HEAD`

    if [ "${ALL}" -eq "1" ]; then
      echo $ID $NAME
    else
      CHECK=`curl -sILw '%{http_code}\n' "https://${URL}/${ID}/${PREFIX}_${NAME}" -o /dev/null`
      if [ "${CHECK}" -ne "200" ]; then
        echo $ID $NAME
      fi
    fi

  fi

done
