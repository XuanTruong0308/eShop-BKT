#!/bin/bash
set -e

echo "=== 1. Checking / Installing .NET 10 SDK ==="
if ! command -v dotnet &> /dev/null; then
    echo "dotnet CLI not found. Downloading and installing .NET 10 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 10.0
    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$DOTNET_ROOT
    # Add to shell profile so it's persistent
    if [ -f "$HOME/.bashrc" ]; then
        echo 'export DOTNET_ROOT=$HOME/.dotnet' >> $HOME/.bashrc
        echo 'export PATH=$PATH:$DOTNET_ROOT' >> $HOME/.bashrc
    fi
else
    echo "dotnet CLI already installed: $(dotnet --version)"
fi

# Ensure path is updated in current script context
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT

echo "=== 2. Building C# services into local Docker daemon ==="

services=(
    "src/Catalog.API/Catalog.API.csproj"
    "src/Basket.API/Basket.API.csproj"
    "src/Identity.API/Identity.API.csproj"
    "src/Ordering.API/Ordering.API.csproj"
    "src/Discount.API/Discount.API.csproj"
    "src/Webhooks.API/Webhooks.API.csproj"
    "src/WebhookClient/WebhookClient.csproj"
    "src/WebApp/WebApp.csproj"
    "src/OrderProcessor/OrderProcessor.csproj"
    "src/PaymentProcessor/PaymentProcessor.csproj"
)

for service in "${services[@]}"; do
    echo "----------------------------------------"
    echo "Publishing container for: $service"
    echo "----------------------------------------"
    dotnet publish "$service" -t:PublishContainer -p:ContainerImageTag=latest -c Release
done

echo "----------------------------------------"
echo "=== 3. Starting eShop Services via Docker Compose ==="
echo "----------------------------------------"
cd deploy
docker compose up -d

echo "----------------------------------------"
echo "=== 4. Cleaning up Docker Build Cache & Unused Images ==="
echo "----------------------------------------"
docker builder prune -f
docker image prune -f

echo "=== trien khai thanh cong! ==="
echo "WebApp dang chay tai: http://localhost:7298"
echo "Mobile BFF (YARP Gateway) dang chay tai: http://localhost:5222"
