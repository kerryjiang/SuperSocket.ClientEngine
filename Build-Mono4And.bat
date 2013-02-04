@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework
set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\MD22\Debug

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\MD22\Release


pause