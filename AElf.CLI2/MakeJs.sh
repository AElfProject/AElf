#!/bin/bash

DIR="$( cd "$( dirname "$0" )" >/dev/null && pwd )"

cd $DIR/aelf-sdk
npm install
npm install -g github:gulpjs/gulp#v4.0.0
gulp standalone
