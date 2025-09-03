@echo off
echo ==========================================
echo     Masin ���̺��ڵ� �з� - ���� ��ũ��Ʈ
echo ==========================================
echo.

:: ���� ����
set PROJECT_NAME=SaveCodeClassfication
set OUTPUT_DIR=.\publish
set RELEASE_DIR=.\release
set APP_NAME=MasinSaveCodeClassification

:: ���� ��� ���丮 ����
echo [1/6] ���� ���� ���� ���� ��...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
mkdir "%OUTPUT_DIR%"
mkdir "%RELEASE_DIR%"

:: .NET ���� ���� Ȯ��
echo [2/6] .NET SDK Ȯ�� ��...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ����: .NET SDK�� ��ġ�Ǿ� ���� �ʽ��ϴ�.
    echo https://dotnet.microsoft.com/download ���� �ٿ�ε��ϼ���.
    pause
    exit /b 1
)

:: ������Ʈ ���� �� ����
echo [3/6] ������Ʈ ���� �� ��Ű�� ���� ��...
dotnet clean --configuration Release
dotnet restore

:: Release ����
echo [4/6] Release ���� ��...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo ����: ���忡 �����߽��ϴ�.
    pause
    exit /b 1
)

:: ���� ���� ����
echo [5/6] ���� ���� ���� ���� ��...
dotnet publish --configuration Release --output "%OUTPUT_DIR%" --runtime win-x64 --self-contained true --verbosity minimal
if errorlevel 1 (
    echo ����: ������ �����߽��ϴ�.
    pause
    exit /b 1
)

:: ���� ���� ����
echo [6/6] ���� ���� ���� ��...
copy "%OUTPUT_DIR%\%PROJECT_NAME%.exe" "%RELEASE_DIR%\%APP_NAME%.exe"
copy "README.md" "%RELEASE_DIR%\"

:: mainIcon.png ������ �ִٸ� ����
if exist "mainIcon.png" (
    copy "mainIcon.png" "%RELEASE_DIR%\"
)

:: ���̼��� ���� ����
echo Copyright (C) 2024 Masin. All rights reserved. > "%RELEASE_DIR%\LICENSE.txt"
echo. >> "%RELEASE_DIR%\LICENSE.txt"
echo �� ����Ʈ����� ������/����� ����� ���� �����˴ϴ�. >> "%RELEASE_DIR%\LICENSE.txt"

:: ���� ���� ���� ����
echo ���ø����̼�: Masin ���̺��ڵ� �з� > "%RELEASE_DIR%\��������.txt"
echo ����: 1.0.0 >> "%RELEASE_DIR%\��������.txt"
echo ���� ��¥: %date% %time% >> "%RELEASE_DIR%\��������.txt"
echo ��� �÷���: Windows x64 >> "%RELEASE_DIR%\��������.txt"
echo ���� ����: %APP_NAME%.exe >> "%RELEASE_DIR%\��������.txt"
echo. >> "%RELEASE_DIR%\��������.txt"
echo ���� ���: >> "%RELEASE_DIR%\��������.txt"
echo 1. %APP_NAME%.exe ����Ŭ������ ���� >> "%RELEASE_DIR%\��������.txt"
echo 2. ���̺� �ڵ� ������ �ִ� ���� ���� >> "%RELEASE_DIR%\��������.txt"
echo 3. '���� �м�' ��ư Ŭ�� >> "%RELEASE_DIR%\��������.txt"
echo 4. ĳ���� ���� �� ���̺� �ڵ� ���� >> "%RELEASE_DIR%\��������.txt"

:: ���� ũ�� Ȯ��
for %%F in ("%RELEASE_DIR%\%APP_NAME%.exe") do set size=%%~zF
set /a size_mb=size/1024/1024

echo.
echo ==========================================
echo            ���� �Ϸ�!
echo ==========================================
echo ���� ��ġ: %RELEASE_DIR%
echo ���� ����: %APP_NAME%.exe
echo ���� ũ��: %size_mb%MB
echo.
echo ������ ����:
dir "%RELEASE_DIR%" /b
echo.
echo ���� ������ �׽�Ʈ�غ�����!
echo ==========================================

:: ���� ���� ����
start explorer "%RELEASE_DIR%"

pause