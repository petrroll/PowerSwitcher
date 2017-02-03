if not exist "..\Build" mkdir ..\Build
if not exist "..\Build\Appx" mkdir ..\Build\Appx

MakeAppx pack /o /f MappingFile.txt /p ..\Build\Appx\PowerSwitcher.appx
copy /y ..\Build\Appx\PowerSwitcher.appx ..\Build\Appx\PowerSwitcher_ns.appx
signtool.exe sign -f ..\Build\Certs\my.pfx -fd SHA256 -v ..\Build\Appx\PowerSwitcher.appx