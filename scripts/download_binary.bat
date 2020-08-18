SET scriptdir=%~dp0

SET version=v1.0.3
SET filename=contract_csharp_plugin-%version%-win32.zip

SET url=https://github.com/AElfProject/contract-plugin/releases/download/%version%/%filename%
SET file=%scriptdir%%filename%

if not exist "%scriptdir%contract_csharp_plugin.exe" (
    if not exist "%file%" (
        echo "download contract plugin from: %url%"
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%url%' -OutFile '%file%'"
    )
    echo "unzip file: %file%"
    unzip %scriptdir%%filename% -d %scriptdir%
)
