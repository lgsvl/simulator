#!/bin/sh

set -eu

wget -O - https://www.sqlite.org/2019/sqlite-autoconf-3290000.tar.gz | tar xfz -
cd sqlite-autoconf-*
./configure
make libsqlite3.la
cp .libs/libsqlite3.so ../x64/
strip --strip-unneeded ../x64/libsqlite3.so
cd ..
rm -rf sqlite-autoconf-*
