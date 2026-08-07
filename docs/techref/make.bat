@ECHO OFF
pushd %~dp0
REM Command file for Sphinx documentation (GERG-2008 Technical Reference)

if "%SPHINXBUILD%" == "" ( set SPHINXBUILD=sphinx-build )
set SOURCEDIR=.
set BUILDDIR=_build

%SPHINXBUILD% >NUL 2>NUL
if errorlevel 9009 (
	echo.
	echo The 'sphinx-build' command was not found. Install Sphinx, or set
	echo the SPHINXBUILD environment variable to its full path.
	exit /b 1
)

if "%1" == "" goto help
%SPHINXBUILD% -M %1 %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%
goto end

:help
%SPHINXBUILD% -M help %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%

:end
popd
