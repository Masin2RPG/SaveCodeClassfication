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
echo [1/7] ���� ���� ���� ���� ��...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
mkdir "%OUTPUT_DIR%"
mkdir "%RELEASE_DIR%"

:: .NET ���� ���� Ȯ��
echo [2/7] .NET SDK Ȯ�� ��...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ����: .NET SDK�� ��ġ�Ǿ� ���� �ʽ��ϴ�.
    echo https://dotnet.microsoft.com/download ���� �ٿ�ε��ϼ���.
    pause
    exit /b 1
)

:: ������ ���� Ȯ��
echo [3/7] ������ ���� Ȯ�� ��...
if exist "mainIcon.png" (
    echo    mainIcon.png ������ ã�ҽ��ϴ�.
    if not exist "mainIcon.ico" (
        echo    ICO ������ �����ϴ�. PowerShell�� ��ȯ�� �õ��մϴ�...
        powershell -Command "& { Add-Type -AssemblyName System.Drawing; try { $image = [System.Drawing.Image]::FromFile((Resolve-Path 'mainIcon.png').Path); $bitmap32 = New-Object System.Drawing.Bitmap(32, 32); $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32); $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic; $graphics32.DrawImage($image, 0, 0, 32, 32); $iconHandle = $bitmap32.GetHicon(); $icon = [System.Drawing.Icon]::FromHandle($iconHandle); $fileStream = [System.IO.File]::Create('mainIcon.ico'); $icon.Save($fileStream); $fileStream.Close(); $graphics32.Dispose(); $bitmap32.Dispose(); $icon.Dispose(); $image.Dispose(); Write-Host 'ICO ���� ���� �Ϸ�!' } catch { Write-Host 'ICO ��ȯ ����, PNG�� ����մϴ�.' } }"
    ) else (
        echo    ���� ICO ������ ����մϴ�.
    )
) else (
    echo    mainIcon.png ������ �����ϴ�. �⺻ �������� ����մϴ�.
)

:: ������Ʈ ���� �� ����
echo [4/7] ������Ʈ ���� �� ��Ű�� ���� ��...
dotnet clean --configuration Release
dotnet restore

:: Release ����
echo [5/7] Release ���� ��...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo ����: ���忡 �����߽��ϴ�.
    pause
    exit /b 1
)

:: ���� ���� ����
echo [6/7] ���� ���� ���� ���� ��...
dotnet publish --configuration Release --output "%OUTPUT_DIR%" --runtime win-x64 --self-contained true --verbosity minimal
if errorlevel 1 (
    echo ����: ������ �����߽��ϴ�.
    pause
    exit /b 1
)

:: ���� ���� ����
echo [7/7] ���� ���� ���� ��...
copy "%OUTPUT_DIR%\%PROJECT_NAME%.exe" "%RELEASE_DIR%\%APP_NAME%.exe"
copy "README.md" "%RELEASE_DIR%\"

:: ������ ���ϵ� ����
if exist "mainIcon.png" (
    copy "mainIcon.png" "%RELEASE_DIR%\\"
)
if exist "mainIcon.ico" (
    copy "mainIcon.ico" "%RELEASE_DIR%\\"
)

:: charName.json ���� ����
if exist "charName.json" (
    copy "charName.json" "%RELEASE_DIR%\\"
    echo    charName.json ���� ���� �Ϸ�
) else (
    echo    charName.json ������ �����ϴ�. ���� ĳ���͸��� ���˴ϴ�.
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
echo. >> "%RELEASE_DIR%\��������.txt"
echo ������ ����: >> "%RELEASE_DIR%\��������.txt"
echo - ������ ������: mainIcon.png (WPF �������) >> "%RELEASE_DIR%\��������.txt"
echo - ���� ���� ������: mainIcon.ico (�ͽ��÷η� ǥ�ÿ�) >> "%RELEASE_DIR%\��������.txt"

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

:: ������ ���� Ȯ��
if exist "mainIcon.ico" (
    echo ������ ����: ? ���� ���� ������ ����
) else (
    echo ������ ����: ? �⺻ ������ ���
)

echo.
echo ������ ����:
dir "%RELEASE_DIR%" /b
echo.
echo ���� ������ �׽�Ʈ�غ�����!
echo ==========================================

:: ���� ���� ����
start explorer "%RELEASE_DIR%"

pause