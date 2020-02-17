#!/bin/bash

set -ex

dotnet tool install --global cake.tool
dotnet tool restore

export PATH="$PATH:$HOME/.dotnet/tools"

dotnet tool install --global coverlet.console

dotnet cake ./build.cake "$@"
