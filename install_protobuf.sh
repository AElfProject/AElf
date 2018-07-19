#!/bin/sh
#set -e
# check to see if protobuf folder is empty
#if [ ! -d "$HOME/protobuf/lib" ]; then
#  wget https://github.com/google/protobuf/releases/download/v3.6.0/protobuf-all-3.6.0.tar.gz
#  tar -xzvf protobuf-all-3.6.0.tar.gz
#  cd protobuf-3.6.0 && ./configure --prefix=$HOME/protobuf && make && make install
#else
#  echo "Using cached directory."
#fi


# Make sure you grab the latest version
curl -OL https://github.com/google/protobuf/releases/download/v3.6.0/protoc-3.6.0-linux-x86_64.zip

# Unzip
unzip protoc-3.6.0-linux-x86_64.zip -d protoc3

# Move protoc to /usr/local/bin/
mv protoc3/bin/* /usr/local/bin/

# Move protoc3/include to /usr/local/include/
mv protoc3/include/* /usr/local/include/

# Optional: change owner
chown $USER /usr/local/bin/protoc
chown -R $USER /usr/local/include/google