# =====================================================================
# Script PowerShell don dep tien trinh cu va khoi chay eShop AppHost
# Vi tri chay: Thu muc goc eShop-main
# =====================================================================

# 1. Kill toan bo cac tien trinh .NET và eShop cu dang chay de giai phong cong (Port)
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "   [1/4] Dang dung cac tien trinh .NET & eShop cu..." -ForegroundColor Yellow
Write-Host "--------------------------------------------------" -ForegroundColor Cyan

# Kill tien trinh dotnet compiler/host
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# Kill cac project eShop cu neu con sot lai trong bo nho
$projects = @("eShop.AppHost", "Catalog.API", "Ordering.API", "Discount.API", 
              "Identity.API", "Basket.API", "WebApp", "PaymentProcessor", "OrderProcessor")

foreach ($proj in $projects) {
    Get-Process -Name $proj -ErrorAction SilentlyContinue | Stop-Process -Force
}

Start-Sleep -Seconds 1
Write-Host "   [OK] Da dung sach cac tien trinh cu." -ForegroundColor Green

# 2. Tu dong xoa cac container Docker bi treo/khoa cua Aspire (Tranh loi DcpExecutor: Failed to delete before restart)
Write-Host ""
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "   [2/4] Dang kiem tra va don dep container Docker..." -ForegroundColor Yellow
Write-Host "--------------------------------------------------" -ForegroundColor Cyan

if (Get-Command "docker" -ErrorAction SilentlyContinue) {
    $containers = @("ollama", "postgres", "redis", "cosmos", "jaeger")
    foreach ($c in $containers) {
        $containerIds = docker ps -a --filter "name=$c" -q
        if ($containerIds) {
            Write-Host "   -> Dang xoa container treo: $c" -ForegroundColor Gray
            docker rm -f $containerIds | Out-Null
        }
    }
    Write-Host "   [OK] Da don dep cac container Docker treo." -ForegroundColor Green
} else {
    Write-Host "   [Warning] Docker CLI khong ton tai hoac chua bat." -ForegroundColor Yellow
}

# 3. Don dep thu muc backchannels cua Aspire tren Windows (Tranh loi Access Denied Socket)
Write-Host ""
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "   [3/4] Dang don dep backchannels cua Aspire..." -ForegroundColor Yellow
Write-Host "--------------------------------------------------" -ForegroundColor Cyan

$backchannelPath = "$env:USERPROFILE\.aspire\cli\backchannels"
if (Test-Path $backchannelPath) {
    Remove-Item -Path "$backchannelPath\*" -Force -Recurse -ErrorAction SilentlyContinue
    Write-Host "   [OK] Da don dep sach Aspire socket cache." -ForegroundColor Green
} else {
    Write-Host "   [OK] Khong co socket cache cua Aspire." -ForegroundColor Green
}

# 4. Khoi chay project AppHost bang dotnet run
Write-Host ""
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "   [4/4] Dang khoi chay eShop.AppHost..." -ForegroundColor Green
Write-Host "--------------------------------------------------" -ForegroundColor Cyan

# Su dung "dotnet run" vi day la lenh khoi chay on dinh va dung chuan nhat cho Aspire AppHost.
dotnet run --project src/eShop.AppHost
