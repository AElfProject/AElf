#!/bin/bash
BIND_VOLUME='-v /opt:/opt'
CONFIGURE_PATH='-w /opt/aelf-node'
docker run -it --rm   $BIND_VOLUME $CONFIGURE_PATH   aelf/node

