#!/usr/bin/env bash
set -ev

TAG=$1
NUGET_API_KEY=$2
VERSION=$(echo ${TAG} | cut -b 2-)

# Define paths and directories
src_path="src"
contract_path="contract"
build_output="/tmp/aelf-build"

# Clean up the temporary build directory if it exists
if [[ -d ${build_output} ]]; then
    rm -rf ${build_output}
fi

# Restore project dependencies
dotnet restore AElf.All.sln

# ---- Phase 1: Build all projects ----
echo "=== Starting build phase ==="
for path in ${src_path} ${contract_path}; do
    cd ${path}
    echo "---- Building projects in path: ${path} ----"
    for name in $(ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'); do
        # Check if the project has a .csproj file and if it is configured to generate NuGet packages
        if [[ -f ${name}/${name}.csproj ]] && [[ $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) -eq 1 ]]; then
            echo "Building project: ${name}/${name}.csproj"
            dotnet build ${name}/${name}.csproj --configuration Release \
                -P:Version=${VERSION} -P:Authors=AElf -o ${build_output} /clp:ErrorsOnly
        fi
    done
    cd ../
done

# ---- Phase 2: Push NuGet packages ----
echo "=== Starting push phase ==="
for path in ${src_path} ${contract_path}; do
    cd ${path}
    echo "---- Pushing NuGet packages in path: ${path} ----"
    for name in $(ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'); do
        # Check if the .nupkg file exists and push it to NuGet if it does
        if [[ -f ${name}/${name}.csproj ]] && [[ $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) -eq 1 ]]; then
            PACKAGE_PATH="${build_output}/${name}.${VERSION}.nupkg"
            if [[ -f ${PACKAGE_PATH} ]]; then
                echo "Pushing package: ${PACKAGE_PATH}"
                dotnet nuget push ${PACKAGE_PATH} -k ${NUGET_API_KEY} \
                    -s https://api.nuget.org/v3/index.json --skip-duplicate
            else
                echo "Error: Package not found at ${PACKAGE_PATH}"
                exit 1
            fi
        fi
    done
    cd ../
done

echo "=== All tasks completed successfully! ==="