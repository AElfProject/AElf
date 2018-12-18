#!/bin/bash

DIR="$( cd "$( dirname "$0" )" >/dev/null && pwd )"

cd $DIR/aelf-sdk
npm install
node ./node_modules/gulp/bin/gulp.js standalone
