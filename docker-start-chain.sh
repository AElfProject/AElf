#!/bin/bash

password=admin123

(echo "y"
sleep 1
echo $password
sleep 1
echo $password)|aelf-command create

cd ~/.local/share/aelf/keys

export AELF_ADDRESS=`ls -lt |awk '{if ($9) printf("%s\n",$9)}'|head -n 1|cut -d . -f1`
export AELF_PASSWORD=password

echo $AELF_ADDRESS
echo $AELF_PASSWORD

cd /app

dotnet /app/AElf.Launcher.dll --environment Docker --config.path /app