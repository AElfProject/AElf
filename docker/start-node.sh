#!/bin/bash
PUBLISH_PORT='-p 6800:6800 -p 8000:8000'
BIND_VOLUME='-v /opt:/opt'
CONfIGURE_PATH='/opt/aelf-node'
docker run -itd $PUBLISH_PORT  $BIND_VOLUME -w  $CONfIGURE_PATH  aelf/node  dotnet /app/AElf.Launcher.dll  --config.path $CONfIGURE_PATH
