#!/bin/bash
set -x
set -u

LOG_FILE=$1

grep 'threw exception' -B 100 $LOG_FILE

RESULT=$?

if [ "${RESULT}" == "0" ] ; then
    echo "Unity threw exception. Check log ${LOG_FILE}"
    exit 1
else
    echo "No exceptions found"
fi
