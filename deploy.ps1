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
Write-Step "기존 배포 파일 정리 중" 1 6
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 2. .NET SDK 확인
Write-Step ".NET SDK 확인 중" 2 6
try {
    $dotnetVersion = dotnet --version
    Write-Host "   .NET SDK 버전: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   오류: .NET SDK가 설치되어 있지 않습니다." -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download 에서 다운로드하세요." -ForegroundColor Red
    exit 1
}

# 3. 프로젝트 정리 및 복원
Write-Step "프로젝트 정리 및 패키지 복원 중" 3 6
dotnet clean --configuration Release --verbosity quiet
dotnet restore --verbosity quiet

# 4. Release 빌드
Write-Step "Release 빌드 중" 4 6
$buildResult = dotnet build --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   오류: 빌드에 실패했습니다." -ForegroundColor Red
    exit 1
}
Write-Host "   빌드 성공!" -ForegroundColor Green

# 5. 단일 파일 배포
Write-Step "단일 파일 배포 생성 중" 5 6
$publishResult = dotnet publish --configuration Release --output $OutputDir --runtime win-x64 --self-contained true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   오류: 배포에 실패했습니다." -ForegroundColor Red
    exit 1
}
Write-Host "   배포 성공!" -ForegroundColor Green

# 6. 배포 파일 정리
Write-Step "배포 파일 정리 중" 6 6

# 실행 파일 복사
Copy-Item "$OutputDir\$ProjectName.exe" "$ReleaseDir\$AppName.exe"

# README 복사
if (Test-Path "README.md") {
    Copy-Item "README.md" $ReleaseDir
}

# 아이콘 파일 복사
if (Test-Path "mainIcon.png") {
    Copy-Item "mainIcon.png" $ReleaseDir
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