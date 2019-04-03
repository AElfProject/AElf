SET scriptdir=%~dp0

SET version="v1.0.1"
SET filename="contract_csharp_plugin-%version%-win32.zip"

if not exist "%scriptdir%contract_csharp_plugin.exe" (
    powershell -Command "(new-object net.webclient).DownloadFile('https://github.com/AElfProject/contract-plugin/releases/download/%version%/%filename%', '%scriptdir%%filename%')"
    unzip "%scriptdir%%filename%"  -d "%scriptdir%"
    del "%scriptdir%%filename%"
)
