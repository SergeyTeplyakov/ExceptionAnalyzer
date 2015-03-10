@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)
 
set version=1.0.0
if not "%PackageVersion%" == "" (
   set version=%PackageVersion%
)
 
set nuget=".\src\packages\NuGet.CommandLine.2.8.2\tools\nuget"

if "%nuget%" == "" (
	set nuget=nuget
)
 
"%programfiles(x86)%\MSBuild\14.0\bin\msbuild" src\ExceptionAnalyzer.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=diag /nr:false
 
mkdir Build
mkdir Build\tools
mkdir Build\tools\analyzers\
 
%nuget% pack "src\ExceptionAnalyzer\ExceptionAnalyzer\ExceptionAnalyzer.nuspec" -NoPackageAnalysis -verbosity detailed -o Build -Version %version% -p Configuration="%config%"
