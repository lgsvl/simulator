#!/bin/bash
set -u

LOG_FILE=$1

if [ ! -f "$LOG_FILE" ]; then
    echo "Can't find file: $LOG_FILE "
    exit 1
fi

grep --extended-regexp \
    --ignore-case \
    --before-context=100 --after-context=100 \
    'threw exception|Fatal Error|Caught fatal signal|Scripts have compiler errors|^Player build result: Failed!' \
    $LOG_FILE

RESULT=$?

if [ "${RESULT}" == "0" ] ; then
    echo "Fatal error found. Check logfile $(basename ${LOG_FILE})"
    exit 1
else
    echo "No fatal error found"
fi
