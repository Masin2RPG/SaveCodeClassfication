@echo off
echo ==========================================
echo     Masin 세이브코드 분류 - 배포 스크립트
echo ==========================================
echo.

:: 변수 설정
set PROJECT_NAME=SaveCodeClassfication
set OUTPUT_DIR=.\publish
set RELEASE_DIR=.\release
set APP_NAME=MasinSaveCodeClassification

:: 기존 출력 디렉토리 정리
echo [1/7] 기존 배포 파일 정리 중...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
mkdir "%OUTPUT_DIR%"
mkdir "%RELEASE_DIR%"

:: .NET 빌드 도구 확인
echo [2/7] .NET SDK 확인 중...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo 오류: .NET SDK가 설치되어 있지 않습니다.
    echo https://dotnet.microsoft.com/download 에서 다운로드하세요.
    pause
    exit /b 1
)

:: 아이콘 파일 확인
echo [3/7] 아이콘 파일 확인 중...
if exist "mainIcon.png" (
    echo    mainIcon.png 파일을 찾았습니다.
    if not exist "mainIcon.ico" (
        echo    ICO 파일이 없습니다. PowerShell로 변환을 시도합니다...
        powershell -Command "& { Add-Type -AssemblyName System.Drawing; try { $image = [System.Drawing.Image]::FromFile((Resolve-Path 'mainIcon.png').Path); $bitmap32 = New-Object System.Drawing.Bitmap(32, 32); $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32); $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic; $graphics32.DrawImage($image, 0, 0, 32, 32); $iconHandle = $bitmap32.GetHicon(); $icon = [System.Drawing.Icon]::FromHandle($iconHandle); $fileStream = [System.IO.File]::Create('mainIcon.ico'); $icon.Save($fileStream); $fileStream.Close(); $graphics32.Dispose(); $bitmap32.Dispose(); $icon.Dispose(); $image.Dispose(); Write-Host 'ICO 파일 생성 완료!' } catch { Write-Host 'ICO 변환 실패, PNG만 사용합니다.' } }"
    ) else (
        echo    기존 ICO 파일을 사용합니다.
    )
) else (
    echo    mainIcon.png 파일이 없습니다. 기본 아이콘을 사용합니다.
)

:: 프로젝트 정리 및 복원
echo [4/7] 프로젝트 정리 및 패키지 복원 중...
dotnet clean --configuration Release
dotnet restore

:: Release 빌드
echo [5/7] Release 빌드 중...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo 오류: 빌드에 실패했습니다.
    pause
    exit /b 1
)

:: 단일 파일 배포
echo [6/7] 단일 파일 배포 생성 중...
dotnet publish --configuration Release --output "%OUTPUT_DIR%" --runtime win-x64 --self-contained true --verbosity minimal
if errorlevel 1 (
    echo 오류: 배포에 실패했습니다.
    pause
    exit /b 1
)

:: 배포 파일 정리
echo [7/7] 배포 파일 정리 중...
copy "%OUTPUT_DIR%\%PROJECT_NAME%.exe" "%RELEASE_DIR%\%APP_NAME%.exe"
copy "README.md" "%RELEASE_DIR%\"

:: 아이콘 파일들 복사
if exist "mainIcon.png" (
    copy "mainIcon.png" "%RELEASE_DIR%\\"
)
if exist "mainIcon.ico" (
    copy "mainIcon.ico" "%RELEASE_DIR%\\"
)

:: charName.json 파일 복사
if exist "charName.json" (
    copy "charName.json" "%RELEASE_DIR%\\"
    echo    charName.json 파일 복사 완료
) else (
    echo    charName.json 파일이 없습니다. 원본 캐릭터명이 사용됩니다.
)

:: 라이선스 파일 생성
echo Copyright (C) 2024 Masin. All rights reserved. > "%RELEASE_DIR%\LICENSE.txt"
echo. >> "%RELEASE_DIR%\LICENSE.txt"
echo 이 소프트웨어는 개인적/상업적 사용을 위해 제공됩니다. >> "%RELEASE_DIR%\LICENSE.txt"

:: 배포 정보 파일 생성
echo 애플리케이션: Masin 세이브코드 분류 > "%RELEASE_DIR%\배포정보.txt"
echo 버전: 1.0.0 >> "%RELEASE_DIR%\배포정보.txt"
echo 빌드 날짜: %date% %time% >> "%RELEASE_DIR%\배포정보.txt"
echo 대상 플랫폼: Windows x64 >> "%RELEASE_DIR%\배포정보.txt"
echo 실행 파일: %APP_NAME%.exe >> "%RELEASE_DIR%\배포정보.txt"
echo. >> "%RELEASE_DIR%\배포정보.txt"
echo 실행 방법: >> "%RELEASE_DIR%\배포정보.txt"
echo 1. %APP_NAME%.exe 더블클릭으로 실행 >> "%RELEASE_DIR%\배포정보.txt"
echo 2. 세이브 코드 파일이 있는 폴더 선택 >> "%RELEASE_DIR%\배포정보.txt"
echo 3. '파일 분석' 버튼 클릭 >> "%RELEASE_DIR%\배포정보.txt"
echo 4. 캐릭터 선택 후 세이브 코드 복사 >> "%RELEASE_DIR%\배포정보.txt"
echo. >> "%RELEASE_DIR%\배포정보.txt"
echo 아이콘 정보: >> "%RELEASE_DIR%\배포정보.txt"
echo - 윈도우 아이콘: mainIcon.png (WPF 윈도우용) >> "%RELEASE_DIR%\배포정보.txt"
echo - 실행 파일 아이콘: mainIcon.ico (익스플로러 표시용) >> "%RELEASE_DIR%\배포정보.txt"

:: 파일 크기 확인
for %%F in ("%RELEASE_DIR%\%APP_NAME%.exe") do set size=%%~zF
set /a size_mb=size/1024/1024

echo.
echo ==========================================
echo            배포 완료!
echo ==========================================
echo 배포 위치: %RELEASE_DIR%
echo 실행 파일: %APP_NAME%.exe
echo 파일 크기: %size_mb%MB

:: 아이콘 상태 확인
if exist "mainIcon.ico" (
    echo 아이콘 설정: ? 실행 파일 아이콘 포함
) else (
    echo 아이콘 설정: ? 기본 아이콘 사용
)

echo.
echo 배포된 파일:
dir "%RELEASE_DIR%" /b
echo.
echo 배포 파일을 테스트해보세요!
echo ==========================================

:: 배포 폴더 열기
start explorer "%RELEASE_DIR%"

pause