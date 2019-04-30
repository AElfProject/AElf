#!/usr/bin/env bash
scriptdir=`dirname "$0"`

d="Darwin"
l="Linux"
if [ "$(uname -s | grep ${d})" != "" ]; then
  osn="macosx"
elif [ "$(uname -s | grep ${l})" != "" ]; then
  osn="linux"
else
  osn="windows"
fi

plugin="${scriptdir}/contract_csharp_plugin"

version="v1.0.2"

if [[ ! -f ${plugin} ]]; then
    if [ $osn == "macosx" ]; then
        suffix="osx"
    elif [ $osn == "linux" ]; then
        suffix="linux"
    fi
    filename=contract_csharp_plugin-${version}-${suffix}.zip
    # Make sure you grab the latest version
    curl -OL https://github.com/AElfProject/contract-plugin/releases/download/${version}/${filename}
    # Unzip
    unzip -o ${filename} -d "${scriptdir}"
    rm ${filename}
fi
