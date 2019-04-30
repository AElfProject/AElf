SET scriptdir=%~dp0

SET version=v1.0.2
SET filename=contract_csharp_plugin-%version%-win32.zip

SET url=https://github.com/AElfProject/contract-plugin/releases/download/%version%/%filename%
SET file=%scriptdir%%filename%

echo %url%
echo %file%

if not exist "%scriptdir%contract_csharp_plugin.exe" (
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%url%' -OutFile '%file%'"
    unzip %scriptdir%%filename% -d %scriptdir%
    del %scriptdir%%filename%
)
