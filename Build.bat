@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework64

if not exist %fdir% (
	set fdir=%WINDIR%\Microsoft.NET\Framework
)

set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.sln /p:Configuration=Debug /t:Rebuild /p:OutputPath=..\bin\Net40\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.sln /p:Configuration=Release /t:Rebuild /p:OutputPath=..\bin\Net40\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net35.sln /p:Configuration=Debug /t:Rebuild /p:OutputPath=..\bin\Net35\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.Net35.sln /p:Configuration=Release /t:Rebuild /p:OutputPath=..\bin\Net35\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.SL40.sln /p:Configuration=Debug /t:Rebuild /p:OutputPath=..\bin\SL40\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.SL40.sln /p:Configuration=Release /t:Rebuild /p:OutputPath=..\bin\SL40\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.WP71.sln /p:Configuration=Debug /t:Rebuild /p:OutputPath=..\bin\WP71\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.WP71.sln /p:Configuration=Release /t:Rebuild /p:OutputPath=..\bin\WP71\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Debug /t:Rebuild /p:OutputPath=..\bin\MD22\Debug
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Release /t:Rebuild /p:OutputPath=..\bin\MD22\Release
FOR /F "tokens=*" %%G IN ('DIR /B /AD /S obj') DO RMDIR /S /Q "%%G"

pause