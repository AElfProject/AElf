SET scriptdir=%~dp0

if not exist "%scriptdir%contract_csharp_plugin.exe" (
    powershell -Command "(new-object net.webclient).DownloadFile('https://github.com/AElfProject/contract-plugin/releases/download/v1.0.0/contract_csharp_plugin-v1.0.0-win32.zip', '%scriptdir%contract_csharp_plugin-v1.0.0-win32.zip')"
    unzip "%scriptdir%contract_csharp_plugin-v1.0.0-win32.zip"  -d "%scriptdir%"
)
