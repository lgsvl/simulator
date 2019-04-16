#!/bin/sh

set -eu

~/.local/bin/coverage run -m unittest discover -v -c
~/.local/bin/coverage html --omit "~/.local/*","tests/*"
