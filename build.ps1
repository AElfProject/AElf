dotnet tool install --global cake.tool --version 0.35.0
dotnet tool restore
dotnet cake ./build.cake -verbosity=verbose
