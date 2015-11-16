@echo off

set msbuild="%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe"

%msbuild% SuperSocket.ClientEngine.build /t:BuildAndPack

pause