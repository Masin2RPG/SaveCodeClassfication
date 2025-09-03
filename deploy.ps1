# Masin ���̺��ڵ� �з� - ���� ��ũ��Ʈ (PowerShell)
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "     Masin ���̺��ڵ� �з� - ���� ��ũ��Ʈ" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host

# ���� ����
$ProjectName = "SaveCodeClassfication"
$OutputDir = ".\publish"
$ReleaseDir = ".\release"
$AppName = "MasinSaveCodeClassification"

# �Լ�: �ܰ� ���
function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host "[$Step/$Total] $Message..." -ForegroundColor Yellow
}

# 1. ���� ��� ���丮 ����
Write-Step "���� ���� ���� ���� ��" 1 6
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 2. .NET SDK Ȯ��
Write-Step ".NET SDK Ȯ�� ��" 2 6
try {
    $dotnetVersion = dotnet --version
    Write-Host "   .NET SDK ����: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   ����: .NET SDK�� ��ġ�Ǿ� ���� �ʽ��ϴ�." -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download ���� �ٿ�ε��ϼ���." -ForegroundColor Red
    exit 1
}

# 3. ������Ʈ ���� �� ����
Write-Step "������Ʈ ���� �� ��Ű�� ���� ��" 3 6
dotnet clean --configuration Release --verbosity quiet
dotnet restore --verbosity quiet

# 4. Release ����
Write-Step "Release ���� ��" 4 6
$buildResult = dotnet build --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ����: ���忡 �����߽��ϴ�." -ForegroundColor Red
    exit 1
}
Write-Host "   ���� ����!" -ForegroundColor Green

# 5. ���� ���� ����
Write-Step "���� ���� ���� ���� ��" 5 6
$publishResult = dotnet publish --configuration Release --output $OutputDir --runtime win-x64 --self-contained true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ����: ������ �����߽��ϴ�." -ForegroundColor Red
    exit 1
}
Write-Host "   ���� ����!" -ForegroundColor Green

# 6. ���� ���� ����
Write-Step "���� ���� ���� ��" 6 6

# ���� ���� ����
Copy-Item "$OutputDir\$ProjectName.exe" "$ReleaseDir\$AppName.exe"

# README ����
if (Test-Path "README.md") {
    Copy-Item "README.md" $ReleaseDir
}

# ������ ���� ����
if (Test-Path "mainIcon.png") {
    Copy-Item "mainIcon.png" $ReleaseDir
}

# ���̼��� ���� ����
@"
Copyright (C) 2024 Masin. All rights reserved.

�� ����Ʈ����� ������/����� ����� ���� �����˴ϴ�.
"@ | Out-File "$ReleaseDir\LICENSE.txt" -Encoding UTF8

# ���� ���� ���� ����
$deployInfo = @"
���ø����̼�: Masin ���̺��ڵ� �з�
����: 1.0.0
���� ��¥: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
��� �÷���: Windows x64
���� ����: $AppName.exe

���� ���:
1. $AppName.exe ����Ŭ������ ����
2. ���̺� �ڵ� ������ �ִ� ���� ����
3. '���� �м�' ��ư Ŭ��
4. ĳ���� ���� �� ���̺� �ڵ� ����

�ý��� �䱸����:
- Windows 10/11 (64-bit)
- �ּ� 512MB RAM
- 100MB �̻� ���� �������
"@

$deployInfo | Out-File "$ReleaseDir\��������.txt" -Encoding UTF8

# ���� ũ�� Ȯ��
$exeFile = Get-Item "$ReleaseDir\$AppName.exe"
$sizeMB = [math]::Round($exeFile.Length / 1MB, 2)

Write-Host
Write-Host "==========================================" -ForegroundColor Green
Write-Host "            ���� �Ϸ�!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "���� ��ġ: $ReleaseDir" -ForegroundColor White
Write-Host "���� ����: $AppName.exe" -ForegroundColor White
Write-Host "���� ũ��: ${sizeMB}MB" -ForegroundColor White
Write-Host

Write-Host "������ ����:" -ForegroundColor Cyan
Get-ChildItem $ReleaseDir | ForEach-Object {
    $itemSize = if ($_.PSIsContainer) { "����" } else { [math]::Round($_.Length / 1KB, 1).ToString() + "KB" }
    Write-Host "  $($_.Name) ($itemSize)" -ForegroundColor White
}

Write-Host
Write-Host "���� ������ �׽�Ʈ�غ�����!" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Green

# ���� ���� ����
Start-Process explorer.exe -ArgumentList (Resolve-Path $ReleaseDir).Path

# ���� Ȯ��
$response = Read-Host "`n������ ���ø����̼��� ���� �����غ��ðڽ��ϱ�? (y/N)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host "���ø����̼��� �����մϴ�..." -ForegroundColor Green
    Start-Process "$ReleaseDir\$AppName.exe"
}