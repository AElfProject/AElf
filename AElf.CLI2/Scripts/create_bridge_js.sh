#!/bin/bash
cd $(dirname $0)
cd AElf.bridge
npm install
browserify app/app.js -o bridge.js
mv bridge.js ..
