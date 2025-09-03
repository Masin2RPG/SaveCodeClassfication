# PNG를 ICO로 변환하는 PowerShell 스크립트
param(
    [string]$InputFile = "mainIcon.png",
    [string]$OutputFile = "mainIcon.ico"
)

Write-Host "PNG를 ICO로 변환 중..." -ForegroundColor Yellow

# .NET의 System.Drawing을 사용하여 변환
Add-Type -AssemblyName System.Drawing

try {
    if (-not (Test-Path $InputFile)) {
        Write-Host "오류: $InputFile 파일을 찾을 수 없습니다." -ForegroundColor Red
        exit 1
    }
    
    # PNG 이미지 로드
    $image = [System.Drawing.Image]::FromFile((Resolve-Path $InputFile).Path)
    
    # 아이콘 크기들 (여러 크기 지원)
    $sizes = @(16, 32, 48, 64, 128, 256)
    
    Write-Host "다음 크기로 변환 중: $($sizes -join ', ')" -ForegroundColor Green
    
    # ICO 파일을 위한 임시 디렉토리
    $tempDir = [System.IO.Path]::GetTempPath() + [System.Guid]::NewGuid().ToString()
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    
    # 각 크기별로 이미지 생성
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
    
    # PNG to ICO 변환 (Windows API 사용)
    $iconPath = (Resolve-Path "." -Relative) + "\" + $OutputFile
    
    # 간단한 ICO 파일 생성 (32x32 크기)
    $bitmap32 = New-Object System.Drawing.Bitmap(32, 32)
    $graphics32 = [System.Drawing.Graphics]::FromImage($bitmap32)
    $graphics32.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics32.DrawImage($image, 0, 0, 32, 32)
    
    # ICO 파일로 저장 (System.Drawing.Icon 사용)
    $iconHandle = $bitmap32.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
    
    $fileStream = [System.IO.File]::Create($iconPath)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    # 리소스 정리
    $graphics32.Dispose()
    $bitmap32.Dispose()
    $icon.Dispose()
    $image.Dispose()
    
    # 임시 디렉토리 정리
    Remove-Item -Path $tempDir -Recurse -Force
    
    Write-Host "변환 완료: $OutputFile" -ForegroundColor Green
    Write-Host "파일 크기: $([math]::Round((Get-Item $OutputFile).Length / 1KB, 2)) KB" -ForegroundColor Cyan
    
} catch {
    Write-Host "변환 중 오류 발생: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}