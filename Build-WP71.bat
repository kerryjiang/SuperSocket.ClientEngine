@echo off

set fdir=%WINDIR%\Microsoft.NET\Framework

set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.WP71.csproj /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\wp71\Debug

%msbuild% SuperSocket.ClientEngine.WP71.csproj /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\wp71\Release


pause