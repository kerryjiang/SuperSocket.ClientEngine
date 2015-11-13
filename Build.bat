@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework64

if not exist %fdir% (
	set fdir=%WINDIR%\Microsoft.NET\Framework
)

set msbuild=%fdir%\v4.0.30319\msbuild.exe

reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\v4.0.30319\SKUs\.NETFramework,Version=v4.5" 2>nul
if errorlevel 0 (
    %msbuild% SuperSocket.ClientEngine.Net45.csproj /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\net45\Debug
	FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

	%msbuild% SuperSocket.ClientEngine.Net45.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\net45\Release
	FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"
)

%msbuild% SuperSocket.ClientEngine.Net40.csproj /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\net40\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net40.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\net40\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net35.csproj /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\net35\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net35.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\net35\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net20.csproj /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\net20\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net20.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\net20\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

pause