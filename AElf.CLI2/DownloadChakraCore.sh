OUT_DIR=$1
FROM_FILE=
TO_FILE="ChakraCore.dll"

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
cp temp/ChakraCoreFiles/lib/$FROM_FILE $OUT_DIR$TO_FILE
rm -rf temp