# PNG�� ICO�� ��ȯ�ϴ� PowerShell ��ũ��Ʈ
param(
    [string]$InputFile = "mainIcon.png",
    [string]$OutputFile = "mainIcon.ico"
)

Write-Host "PNG�� ICO�� ��ȯ ��..." -ForegroundColor Yellow

# .NET�� System.Drawing�� ����Ͽ� ��ȯ
Add-Type -AssemblyName System.Drawing

try {
    if (-not (Test-Path $InputFile)) {
        Write-Host "����: $InputFile ������ ã�� �� �����ϴ�." -ForegroundColor Red
        exit 1
    }
    
    # PNG �̹��� �ε�
    $image = [System.Drawing.Image]::FromFile((Resolve-Path $InputFile).Path)
    
    # ������ ũ��� (���� ũ�� ����)
    $sizes = @(16, 32, 48, 64, 128, 256)
    
    Write-Host "���� ũ��� ��ȯ ��: $($sizes -join ', ')" -ForegroundColor Green
    
    # ICO ������ ���� �ӽ� ���丮
    $tempDir = [System.IO.Path]::GetTempPath() + [System.Guid]::NewGuid().ToString()
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    
    # �� ũ�⺰�� �̹��� ����
    $iconFiles = @()
    foreach ($size in $sizes) {
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($image, 0, 0, $size, $size)
        
        $tempFile = Join-Path $tempDir "$size.png"
        $resized.Save($tempFile, [System.Drawing.Imaging.ImageFormat]::Png)
        $iconFiles += $tempFile
        
        $graphics.Dispose()
        $resized.Dispose()
    }
    
    # PNG to ICO ��ȯ (Windows API ���)
    $iconPath = (Resolve-Path "." -Relative) + "\" + $OutputFile
    
    # ������ ICO ���� ���� (32x32 ũ��)
    $bitmap32 = New-Object System.Drawing.Bitmap(32, 32)
    $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32)
    $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics32.DrawImage($image, 0, 0, 32, 32)
    
    # ICO ���Ϸ� ���� (System.Drawing.Icon ���)
    $iconHandle = $bitmap32.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
    
    $fileStream = [System.IO.File]::Create($iconPath)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    # ���ҽ� ����
    $graphics32.Dispose()
    $bitmap32.Dispose()
    $icon.Dispose()
    $image.Dispose()
    
    # �ӽ� ���丮 ����
    Remove-Item -Path $tempDir -Recurse -Force
    
    Write-Host "��ȯ �Ϸ�: $OutputFile" -ForegroundColor Green
    Write-Host "���� ũ��: $([math]::Round((Get-Item $OutputFile).Length / 1KB, 2)) KB" -ForegroundColor Cyan
    
} catch {
    Write-Host "��ȯ �� ���� �߻�: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}