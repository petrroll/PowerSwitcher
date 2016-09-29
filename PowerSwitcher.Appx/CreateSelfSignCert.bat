if not exist "..\Build" mkdir ..\Build
if not exist "..\Build\Appx" mkdir ..\Build\Certs

MakeCert.exe -r -h 0 -n "CN=Petrroll, L=Prague, C=Czech Republic" -eku 1.3.6.1.5.5.7.3.3 -pe -sv ../Build/Certs/my.pvk ../Build/Certs/my.cer
pvk2pfx.exe -pvk ../Build/Certs/my.pvk -spc ../Build/Certs/my.cer -pfx ../Build/Certs/my.pfx
