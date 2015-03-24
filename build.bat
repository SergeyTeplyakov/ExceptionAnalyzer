@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)
 
set version=1.0.5

if not "%PackageVersion%" == "" (
   set version=%PackageVersion%
)
 
set nuget=".\src\packages\NuGet.CommandLine.2.8.2\tools\nuget"

if "%nuget%" == "" (
	set nuget=nuget
)
 
set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

msbuild src\ExceptionAnalyzer.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=diag /nr:false /t:Rebuild

COPY src\ExceptionAnalyzer\ExceptionAnalyzer.Vsix\bin\%config%\*.vsix BUILD
 
%nuget% pack "src\ExceptionAnalyzer\ExceptionAnalyzer\ExceptionAnalyzer.nuspec" -NoPackageAnalysis -verbosity detailed -o Build -Version %version% -p Configuration="%config%" -BasePath "src\\ExceptionAnalyzer\\ExceptionAnalyzer\\bin\\Release\\" -OutputDirectory "Build"