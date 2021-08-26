#!/bin/bash

set -euo pipefail

if [[ `id -u` -eq 0 ]]; then
  echo "ERROR: running as root is not supported"
  echo "Please run 'export UID' before running docker-compose!"
  exit 1
fi

if [ $# -lt 4 ]; then
  echo "ERROR: use $0 <folder> <url> <prefix> <all> [subfolder1 [subfolder2]]" 
  exit 1
fi

FOLDER=$1
URL=$2
PREFIX=$3
ALL=$4

shift 4
SUBFOLDERS=${@:-all}

# split by comma
SUBFOLDERS=$(echo ${SUBFOLDERS} | tr ',' ' ')

if [ "${FORCE_REBUILD}" == "true" ]; then
  ALL=1
fi

function list_folders {
    echo SUBFOLDERS=${SUBFOLDERS} > /dev/stderr
    
    if [ "${SUBFOLDERS}" == "all" ]; then
        find ${FOLDER} -mindepth 1 -maxdepth 1 -type d
    else
        for subfolder in ${SUBFOLDERS}; do
            path=${FOLDER}/$subfolder
            
            if [ -d "$path" ]; then
                echo $path
            else
                echo "Can't asset find folder ${path}" > /dev/stderr
            fi
        done
    fi
}

for f in $(list_folders); do
  NAME=`basename "${f}"`
  if [[ ! "${NAME}" =~ "@tmp" ]]; then

    ID=`GIT_DIR="${f}/.git" git rev-parse HEAD`

    if [ "${ALL}" -eq "1" ]; then
      echo $ID $NAME
    else
      CHECK=`curl -sILw '%{http_code}\n' "https://${URL}/${ID}/${PREFIX}_${NAME}" -o /dev/null || true`
      if [ "${CHECK}" -ne "200" ]; then
        echo "https://${URL}/${ID}/${PREFIX}_${NAME} doesn't exist, include it in the build" >&2
        echo $ID $NAME
      else
        echo "https://${URL}/${ID}/${PREFIX}_${NAME} does exist, skip building it (you can use FORCE_REBUILD to rebuild existing assets)" >&2
      fi
    fi
  else
    continue
  fi
done
