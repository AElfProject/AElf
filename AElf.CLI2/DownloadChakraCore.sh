FILE_NAME="ChakraCore.dll"
TO_FILE=$1$FILE_NAME
FROM_FILE=

if [ -f "$TO_FILE" ]; then
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

rm -rf temp
mkdir temp
cd temp
curl -SL https://aka.ms/chakracore/install | bash

cd ..
cp temp/ChakraCoreFiles/lib/$FROM_FILE $TO_FILE
rm -rf temp