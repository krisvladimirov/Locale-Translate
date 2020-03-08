REM Create a 'GeneratedReports' folder if it does not exist
if not exist "%~dp0GeneratedReports" mkdir "%~dp0GeneratedReports"

REM Remove any previous test execution files to prevent issues overwriting
IF EXIST "%~dp0LocalisationTranslator.trx" del "%~dp0LocalisationTranslator.trx%"

REM Remove any previously created test output directories
CD %~dp0
FOR /D /R %%X IN (%USERNAME%*) DO RD /S /Q "%%X"

REM Run the tests against the targeted output
call :RunOpenCoverUnitTestMetrics

REM Generate the report output based on the test results
if %errorlevel% equ 0 ( 
	call :RunReportGeneratorOutput	
)

REM Launch the report
if %errorlevel% equ 0 ( 
	call :RunLaunchReport	
)
exit /b %errorlevel%


:RunOpenCoverUnitTestMetrics
"%~dp0..\packages\OpenCover.4.7.922\tools\OpenCover.Console.exe" ^
-register:user ^
-target:"%VS120COMNTOOLS%\..\IDE\mstest.exe" ^
-targetargs:"/testcontainer:\"%~dp0..\LocalisationTranslator.Tests\bin\Debug\LocalisationTranslator.Tests.dll\" /resultsfile:\"%~dp0LocalisationTranslator.trx\"" ^
-filter:"+[LocalisationTranslator*]* -[LocalisationTranslator.Tests]* -[*]LocalisationTranslator.WebAPI.Areas.HelpPage.* -[*]LocalisationTranslator.BundleConfig -[*]LocalisationTranslator.FilterConfig -[*]LocalisationTranslator.RouteConfig -[*]LocalisationTranslator.WebAPI.App_Start.Bootstrapper -[*]LocalisationTranslator.WebAPI.UnityConfig -[*]LocalisationTranslator.WebAPI.WebApiApplication -[*]LocalisationTranslator.WebApiConfig" ^
-mergebyhash ^
-skipautoprops ^
-output:"%~dp0\GeneratedReports\LocalisationTranslatorReport.xml"
exit /b %errorlevel%

:RunReportGeneratorOutput
"%~dp0..\packages\ReportGenerator.4.5.0\ReportGenerator.exe" ^
-reports:"%~dp0\GeneratedReports\LocalisationTranslatorReport.xml" ^
-targetdir:"%~dp0\GeneratedReports\ReportGenerator Output"
exit /b %errorlevel%

:RunLaunchReport
start "report" "%~dp0\GeneratedReports\ReportGenerator Output\index.html"
exit /b %errorlevel%