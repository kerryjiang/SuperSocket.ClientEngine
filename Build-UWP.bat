@echo off

set fdir="%ProgramFiles%\MSBuild\14.0\Bin"

if not exist %fdir% (
	set fdir="%ProgramFiles(x86)%\MSBuild\14.0\Bin"
)

set msbuild=%fdir%\msbuild.exe


%msbuild% SuperSocket.ClientEngine.UWPcsprojsln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=bin\win\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.UWP.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=bin\win\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

pause