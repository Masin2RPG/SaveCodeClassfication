# Masin 세이브코드 분류 - 배포 스크립트 (PowerShell)
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "     Masin 세이브코드 분류 - 배포 스크립트" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host

# 변수 설정
$ProjectName = "SaveCodeClassfication"
$OutputDir = ".\publish"
$ReleaseDir = ".\release"
$AppName = "MasinSaveCodeClassification"

# 함수: 단계 출력
function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host "[$Step/$Total] $Message..." -ForegroundColor Yellow
}

# 1. 기존 출력 디렉토리 정리
Write-Step "기존 배포 파일 정리 중" 1 7
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 2. .NET SDK 확인
Write-Step ".NET SDK 확인 중" 2 7
try {
    $dotnetVersion = dotnet --version
    Write-Host "   .NET SDK 버전: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   오류: .NET SDK가 설치되어 있지 않습니다." -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download 에서 다운로드하세요." -ForegroundColor Red
    exit 1
}

# 3. 아이콘 파일 생성
Write-Step "아이콘 파일 생성 중" 3 7
if (Test-Path "mainIcon.png") {
    if (-not (Test-Path "mainIcon.ico")) {
        Write-Host "   PNG를 ICO로 변환 중..." -ForegroundColor Cyan
        try {
            # PNG를 ICO로 변환
            Add-Type -AssemblyName System.Drawing
            $image = [System.Drawing.Image]::FromFile((Resolve-Path "mainIcon.png").Path)
            
            # 32x32 크기로 리사이즈
            $bitmap32 = New-Object System.Drawing.Bitmap(32, 32)
            $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32)
            $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics32.DrawImage($image, 0, 0, 32, 32)
            
            # ICO 파일로 저장
            $iconHandle = $bitmap32.GetHicon()
            $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
            
            $fileStream = [System.IO.File]::Create("mainIcon.ico")
            $icon.Save($fileStream)
            $fileStream.Close()
            
            # 리소스 정리
            $graphics32.Dispose()
            $bitmap32.Dispose()
            $icon.Dispose()
            $image.Dispose()
            
            Write-Host "   ICO 파일 생성 완료!" -ForegroundColor Green
        } catch {
            Write-Host "   ICO 변환 실패, PNG 파일만 사용합니다: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   기존 ICO 파일 사용" -ForegroundColor Green
    }
} else {
    Write-Host "   mainIcon.png 파일이 없습니다. 기본 아이콘을 사용합니다." -ForegroundColor Yellow
}

# 4. 프로젝트 정리 및 복원
Write-Step "프로젝트 정리 및 패키지 복원 중" 4 7
dotnet clean --configuration Release --verbosity quiet
dotnet restore --verbosity quiet

# 5. Release 빌드
Write-Step "Release 빌드 중" 5 7
$buildResult = dotnet build --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   오류: 빌드에 실패했습니다." -ForegroundColor Red
    exit 1
}
Write-Host "   빌드 성공!" -ForegroundColor Green

# 6. 단일 파일 배포
Write-Step "단일 파일 배포 생성 중" 6 7
$publishResult = dotnet publish --configuration Release --output $OutputDir --runtime win-x64 --self-contained true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   오류: 배포에 실패했습니다." -ForegroundColor Red
    exit 1
}
Write-Host "   배포 성공!" -ForegroundColor Green

# 7. 배포 파일 정리
Write-Step "배포 파일 정리 중" 7 7

# 실행 파일 복사
Copy-Item "$OutputDir\$ProjectName.exe" "$ReleaseDir\$AppName.exe"

# README 복사
if (Test-Path "README.md") {
    Copy-Item "README.md" $ReleaseDir
}

# 아이콘 파일들 복사
if (Test-Path "mainIcon.png") {
    Copy-Item "mainIcon.png" $ReleaseDir
}
if (Test-Path "mainIcon.ico") {
    Copy-Item "mainIcon.ico" $ReleaseDir
}

# 라이선스 파일 생성
@"
Copyright (C) 2024 Masin. All rights reserved.

이 소프트웨어는 개인적/상업적 사용을 위해 제공됩니다.
"@ | Out-File "$ReleaseDir\LICENSE.txt" -Encoding UTF8

# 배포 정보 파일 생성
$deployInfo = @"
애플리케이션: Masin 세이브코드 분류
버전: 1.0.0
빌드 날짜: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
대상 플랫폼: Windows x64
실행 파일: $AppName.exe

실행 방법:
1. $AppName.exe 더블클릭으로 실행
2. 세이브 코드 파일이 있는 폴더 선택
3. '파일 분석' 버튼 클릭
4. 캐릭터 선택 후 세이브 코드 복사

시스템 요구사항:
- Windows 10/11 (64-bit)
- 최소 512MB RAM
- 100MB 이상 여유 저장공간

아이콘 정보:
- 윈도우 아이콘: mainIcon.png (WPF 윈도우용)
- 실행 파일 아이콘: mainIcon.ico (익스플로러 표시용)
"@

$deployInfo | Out-File "$ReleaseDir\배포정보.txt" -Encoding UTF8

# 파일 크기 확인
$exeFile = Get-Item "$ReleaseDir\$AppName.exe"
$sizeMB = [math]::Round($exeFile.Length / 1MB, 2)

Write-Host
Write-Host "==========================================" -ForegroundColor Green
Write-Host "            배포 완료!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "배포 위치: $ReleaseDir" -ForegroundColor White
Write-Host "실행 파일: $AppName.exe" -ForegroundColor White
Write-Host "파일 크기: ${sizeMB}MB" -ForegroundColor White

# 아이콘 상태 확인
if (Test-Path "mainIcon.ico") {
    Write-Host "아이콘 설정: ? 실행 파일 아이콘 포함" -ForegroundColor Green
} else {
    Write-Host "아이콘 설정: ? 기본 아이콘 사용" -ForegroundColor Yellow
}

Write-Host

Write-Host "배포된 파일:" -ForegroundColor Cyan
Get-ChildItem $ReleaseDir | ForEach-Object {
    $itemSize = if ($_.PSIsContainer) { "폴더" } else { [math]::Round($_.Length / 1KB, 1).ToString() + "KB" }
    Write-Host "  $($_.Name) ($itemSize)" -ForegroundColor White
}

Write-Host
Write-Host "배포 파일을 테스트해보세요!" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Green

# 배포 폴더 열기
Start-Process explorer.exe -ArgumentList (Resolve-Path $ReleaseDir).Path

# 실행 확인
$response = Read-Host "`n배포된 애플리케이션을 지금 실행해보시겠습니까? (y/N)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host "애플리케이션을 실행합니다..." -ForegroundColor Green
    Start-Process "$ReleaseDir\$AppName.exe"
}