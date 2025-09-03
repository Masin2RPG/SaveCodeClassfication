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
Write-Step "���� ���� ���� ���� ��" 1 7
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 2. .NET SDK Ȯ��
Write-Step ".NET SDK Ȯ�� ��" 2 7
try {
    $dotnetVersion = dotnet --version
    Write-Host "   .NET SDK ����: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   ����: .NET SDK�� ��ġ�Ǿ� ���� �ʽ��ϴ�." -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download ���� �ٿ�ε��ϼ���." -ForegroundColor Red
    exit 1
}

# 3. ������ ���� ����
Write-Step "������ ���� ���� ��" 3 7
if (Test-Path "mainIcon.png") {
    if (-not (Test-Path "mainIcon.ico")) {
        Write-Host "   PNG�� ICO�� ��ȯ ��..." -ForegroundColor Cyan
        try {
            # PNG�� ICO�� ��ȯ
            Add-Type -AssemblyName System.Drawing
            $image = [System.Drawing.Image]::FromFile((Resolve-Path "mainIcon.png").Path)
            
            # 32x32 ũ��� ��������
            $bitmap32 = New-Object System.Drawing.Bitmap(32, 32)
            $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32)
            $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics32.DrawImage($image, 0, 0, 32, 32)
            
            # ICO ���Ϸ� ����
            $iconHandle = $bitmap32.GetHicon()
            $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
            
            $fileStream = [System.IO.File]::Create("mainIcon.ico")
            $icon.Save($fileStream)
            $fileStream.Close()
            
            # ���ҽ� ����
            $graphics32.Dispose()
            $bitmap32.Dispose()
            $icon.Dispose()
            $image.Dispose()
            
            Write-Host "   ICO ���� ���� �Ϸ�!" -ForegroundColor Green
        } catch {
            Write-Host "   ICO ��ȯ ����, PNG ���ϸ� ����մϴ�: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ���� ICO ���� ���" -ForegroundColor Green
    }
} else {
    Write-Host "   mainIcon.png ������ �����ϴ�. �⺻ �������� ����մϴ�." -ForegroundColor Yellow
}

# 4. ������Ʈ ���� �� ����
Write-Step "������Ʈ ���� �� ��Ű�� ���� ��" 4 7
dotnet clean --configuration Release --verbosity quiet
dotnet restore --verbosity quiet

# 5. Release ����
Write-Step "Release ���� ��" 5 7
$buildResult = dotnet build --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ����: ���忡 �����߽��ϴ�." -ForegroundColor Red
    exit 1
}
Write-Host "   ���� ����!" -ForegroundColor Green

# 6. ���� ���� ����
Write-Step "���� ���� ���� ���� ��" 6 7
$publishResult = dotnet publish --configuration Release --output $OutputDir --runtime win-x64 --self-contained true --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ����: ������ �����߽��ϴ�." -ForegroundColor Red
    exit 1
}
Write-Host "   ���� ����!" -ForegroundColor Green

# 7. ���� ���� ����
Write-Step "���� ���� ���� ��" 7 7

# ���� ���� ����
Copy-Item "$OutputDir\$ProjectName.exe" "$ReleaseDir\$AppName.exe"

# README ����
if (Test-Path "README.md") {
    Copy-Item "README.md" $ReleaseDir
}

# ������ ���ϵ� ����
if (Test-Path "mainIcon.png") {
    Copy-Item "mainIcon.png" $ReleaseDir
}
if (Test-Path "mainIcon.ico") {
    Copy-Item "mainIcon.ico" $ReleaseDir
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

������ ����:
- ������ ������: mainIcon.png (WPF �������)
- ���� ���� ������: mainIcon.ico (�ͽ��÷η� ǥ�ÿ�)
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

# ������ ���� Ȯ��
if (Test-Path "mainIcon.ico") {
    Write-Host "������ ����: ? ���� ���� ������ ����" -ForegroundColor Green
} else {
    Write-Host "������ ����: ? �⺻ ������ ���" -ForegroundColor Yellow
}

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