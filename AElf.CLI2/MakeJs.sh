#!/bin/bash

DIR="$( cd "$( dirname "$0" )" >/dev/null && pwd )"

cd $DIR/aelf-sdk
npm install
gulp standalone
