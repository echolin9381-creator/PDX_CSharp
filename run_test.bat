@echo off
chcp 437 >nul
set CSC=C:\Program Files\dotnet\sdk\8.0.416\Roslyn\bincore\csc.dll
set FX=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

dotnet exec "%CSC%" /target:exe /out:SelfTest.exe /nostdlib /noconfig /r:"%FX%\mscorlib.dll" /r:"%FX%\System.dll" SelfTest.cs

if %ERRORLEVEL% equ 0 (
    SelfTest.exe
) else (
    echo Compile failed
)
