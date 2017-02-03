if not exist "..\Build" mkdir ..\Build
if not exist "..\Build\Certs" mkdir ..\Build\Certs

MakeCert.exe -r -h 0 -n "CN=DFCA8587-B6FA-4F7D-9B35-1ABB74757F2E" -eku 1.3.6.1.5.5.7.3.3 -pe -sv ../Build/Certs/my.pvk ../Build/Certs/my.cer
pvk2pfx.exe -pvk ../Build/Certs/my.pvk -spc ../Build/Certs/my.cer -pfx ../Build/Certs/my.pfx
