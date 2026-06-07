# ==============================================================
# Script tao branch structure cho eShop-BKT
# Repo: https://github.com/XuanTruong0308/eShop-BKT
# Chay bang: PowerShell (click phai -> Run with PowerShell)
# ==============================================================

$REPO_URL = "https://github.com/XuanTruong0308/eShop-BKT.git"
$PROJECT_DIR = "eShop-BKT"

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  eShop-BKT - Khoi tao Branch Structure" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# Buoc 1: Clone repo
Write-Host ""
Write-Host "[1/6] Clone repository..." -ForegroundColor Yellow
git clone $REPO_URL
Set-Location $PROJECT_DIR

# Buoc 2: Tao branch MAIN (chua tat ca file .md)
Write-Host ""
Write-Host "[2/6] Tao branch: main (chua tat ca file .md)..." -ForegroundColor Yellow
git checkout main 2>$null
if ($LASTEXITCODE -ne 0) {
    git checkout -b main
}
if (-not (Test-Path "README.md")) {
    "# eShop-BKT Documentation" | Out-File -Encoding utf8 README.md
    git add README.md
    git commit -m "init: khoi tao branch main - noi chua toan bo tai lieu .md"
}
git push -u origin main
Write-Host "   [OK] Branch 'main' da duoc tao" -ForegroundColor Green

# Buoc 3: Tao branch CORE (full source code)
Write-Host ""
Write-Host "[3/6] Tao branch: core (full source code)..." -ForegroundColor Yellow
git checkout -b core
git push -u origin core
Write-Host "   [OK] Branch 'core' da duoc tao" -ForegroundColor Green

# Buoc 4: Tao branch KHAI (tao tu core)
Write-Host ""
Write-Host "[4/6] Tao branch: Khai (dev branch - base tu core)..." -ForegroundColor Yellow
git checkout core
git checkout -b Khai
git push -u origin Khai
Write-Host "   [OK] Branch 'Khai' da duoc tao tu core" -ForegroundColor Green

# Buoc 5: Tao branch BAO (tao tu core)
Write-Host ""
Write-Host "[5/6] Tao branch: Bao (dev branch - base tu core)..." -ForegroundColor Yellow
git checkout core
git checkout -b Bao
git push -u origin Bao
Write-Host "   [OK] Branch 'Bao' da duoc tao tu core" -ForegroundColor Green

# Buoc 6: Tao branch TRUONG (tao tu core)
Write-Host ""
Write-Host "[6/6] Tao branch: Truong (dev branch - base tu core)..." -ForegroundColor Yellow
git checkout core
git checkout -b Truong
git push -u origin Truong
Write-Host "   [OK] Branch 'Truong' da duoc tao tu core" -ForegroundColor Green

# Tom tat
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  HOAN TAT - Cau truc branch da duoc tao:" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  main   -> Tat ca file tai lieu (.md)" -ForegroundColor White
Write-Host "  core   -> Full source code (goc)" -ForegroundColor White
Write-Host "  Khai   -> Dev Khai lam viec  (base tu core)" -ForegroundColor White
Write-Host "  Bao    -> Dev Bao lam viec   (base tu core)" -ForegroundColor White
Write-Host "  Truong -> Dev Truong lam viec (base tu core)" -ForegroundColor White
Write-Host ""
Write-Host "  Repo: https://github.com/XuanTruong0308/eShop-BKT" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Nhan phim bat ky de dong..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")