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

%msbuild% SuperSocket.ClientEngine.SL40.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\SL40\Debug

%msbuild% SuperSocket.ClientEngine.SL40.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\SL40\Release

%msbuild% SuperSocket.ClientEngine.SL50.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\SL50\Debug

%msbuild% SuperSocket.ClientEngine.SL50.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\SL50\Release

%msbuild% SuperSocket.ClientEngine.WP71.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\WP71\Debug

%msbuild% SuperSocket.ClientEngine.WP71.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\WP71\Release

%msbuild% SuperSocket.ClientEngine.WP80.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\WP80\Debug

%msbuild% SuperSocket.ClientEngine.WP80.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\WP80\Release

set fdir=%WINDIR%\Microsoft.NET\Framework
set msbuild=%fdir%\v4.0.30319\msbuild.exe

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Debug /t:Clean;Rebuild /p:OutputPath=..\bin\MD22\Debug

%msbuild% SuperSocket.ClientEngine.MonoDroid.sln /p:Configuration=Release /t:Clean;Rebuild /p:OutputPath=..\bin\MD22\Release


pause