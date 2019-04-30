#!/usr/bin/env bash

CHAKRA_VERSION="$1"
FILE_NAME="ChakraCore.dll"
TO_FILE=$2${FILE_NAME}
FROM_FILE=

if [[ -f "$TO_FILE" ]]; then
    echo "$TO_FILE exists, no need to download again"
    exit
fi

GET_OS()
{
    UNAME_OUTPUT=$(uname -mrsn)

    if [[ "${UNAME_OUTPUT}" =~ 'Darwin' ]]; then
        CURRENT_OS="macOS"
        FROM_FILE="libChakraCore.dylib"
        return
    fi

    if [[ "${UNAME_OUTPUT}" =~ 'Linux' ]]; then
        CURRENT_OS="Linux"
        FROM_FILE="libChakraCore.so"
        return
    fi

    exit
}

GET_OS

if [ -f "$HOME/.chakracore/$CHAKRA_VERSION/ChakraCore.dll" ]; then
    echo "$HOME/.chakracore/$CHAKRA_VERSION/ChakraCore.dll exists"
    cp $HOME/.chakracore/$CHAKRA_VERSION/${FILE_NAME}  ${FILE_NAME}
    exit
fi

WORK_PATH=`pwd`
SCRIPTS_PATH=$(cd "$(dirname "$0")";pwd)
DOWNLOAD_PATH=${SCRIPTS_PATH}/.tmp/chakracore

CHAKRACORE_FILE=${DOWNLOAD_PATH}/ChakraCoreFiles/lib/${FROM_FILE}

if [[ ! -f "$CHAKRACORE_FILE" ]]; then
    rm -rf ${DOWNLOAD_PATH} && mkdir -p ${DOWNLOAD_PATH}
    cd ${DOWNLOAD_PATH}
    bash ${SCRIPTS_PATH}/download_chakracore.sh $CHAKRA_VERSION
fi

cd ${WORK_PATH}
if [ -d "$HOME/.chakracore" ]; then
    rm -rf $HOME/.chakracore/*
    mkdir -p $HOME/.chakracore/$CHAKRA_VERSION/
    cp ${CHAKRACORE_FILE}  $HOME/.chakracore/$CHAKRA_VERSION/${FILE_NAME}
    cp ${CHAKRACORE_FILE}  ${FILE_NAME}
    exit
fi
cp ${CHAKRACORE_FILE}  ${TO_FILE}
