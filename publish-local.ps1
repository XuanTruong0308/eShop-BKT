Write-Host "=== 1. Cleaning old build artifacts ==_" -ForegroundColor Cyan
if (Test-Path "deploy/publish") {
    Remove-Item -Path "deploy/publish" -Recurse -Force
}

$projects = @(
    @{ Path = "src\Catalog.API\Catalog.API.csproj"; Name = "Catalog.API" },
    @{ Path = "src\Basket.API\Basket.API.csproj"; Name = "Basket.API" },
    @{ Path = "src\Identity.API\Identity.API.csproj"; Name = "Identity.API" },
    @{ Path = "src\Ordering.API\Ordering.API.csproj"; Name = "Ordering.API" },
    @{ Path = "src\Discount.API\Discount.API.csproj"; Name = "Discount.API" },
    @{ Path = "src\Webhooks.API\Webhooks.API.csproj"; Name = "Webhooks.API" },
    @{ Path = "src\WebhookClient\WebhookClient.csproj"; Name = "WebhookClient" },
    @{ Path = "src\WebApp\WebApp.csproj"; Name = "WebApp" },
    @{ Path = "src\OrderProcessor\OrderProcessor.csproj"; Name = "OrderProcessor" },
    @{ Path = "src\PaymentProcessor\PaymentProcessor.csproj"; Name = "PaymentProcessor" }
)

Write-Host "=== 2. Compiling and Publishing C# services for Linux x64 ===" -ForegroundColor Cyan
foreach ($project in $projects) {
    Write-Host "----------------------------------------" -ForegroundColor Green
    Write-Host "Publishing $($project.Name)..." -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor Green
    
    dotnet publish $project.Path -c Release -o "deploy\publish\$($project.Name)" --no-self-contained -r linux-x64 -p:UseArtifactsOutput=false
}

Write-Host "=== Bien dịch hoan tat! ===" -ForegroundColor Cyan
Write-Host "Tat ca file chay da duoc dat tai thu muc: deploy\publish\" -ForegroundColor Cyan
