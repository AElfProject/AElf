#!/bin/sh
set -e
# check to see if protobuf folder is empty
if [ ! -d "$HOME/protobuf/lib" ]; then
  wget https://protobuf.googlecode.com/files/protobuf-3.6.0.tar.gz
  tar -xzvf protobuf-3.6.0.tar.gz
  cd protobuf-3.6.0 && ./configure --prefix=$HOME/protobuf && make && make install
else
  echo "Using cached directory."
fi