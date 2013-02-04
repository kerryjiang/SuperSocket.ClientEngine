@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework

set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.WP80.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\WP80\Debug

%msbuild% SuperSocket.ClientEngine.WP80.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\WP80\Release


pause