SET msbuildpath="C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"

%msbuildpath% Tools\AssetCompileHelper\AssetCompileHelper.csproj /t:Rebuild /p:Configuration=Release
%msbuildpath% Tools\MPTanks.MapMaker\MPTanks.MapMaker.csproj /t:Rebuild /p:Configuration=Release
%msbuildpath% Tools\MPTanks.ModCompiler\MPTanks.ModCompiler.csproj /t:Rebuild /p:Configuration=Release

%msbuildpath% Tools\AssetCompileHelper\AssetCompileHelper.csproj /t:Rebuild /p:Configuration=Debug
%msbuildpath% Tools\MPTanks.MapMaker\MPTanks.MapMaker.csproj /t:Rebuild /p:Configuration=Debug
%msbuildpath% Tools\MPTanks.ModCompiler\MPTanks.ModCompiler.csproj /t:Rebuild /p:Configuration=Debug

pause