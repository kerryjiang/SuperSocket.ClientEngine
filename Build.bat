@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework64

if not exist %fdir% (
	set fdir=%WINDIR%\Microsoft.NET\Framework
)

set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\Net40\Debug

%msbuild% SuperSocket.ClientEngine.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\Net40\Release

%msbuild% SuperSocket.ClientEngine.Net35.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\Net35\Debug

%msbuild% SuperSocket.ClientEngine.Net35.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\Net35\Release

%msbuild% SuperSocket.ClientEngine.Net20.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\Net20\Debug

%msbuild% SuperSocket.ClientEngine.Net20.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\Net20\Release

pause