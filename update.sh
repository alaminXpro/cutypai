#!/bin/bash
set -e

echo "🚀 Pulling latest code..."
git reset --hard
git pull origin main

echo "📦 Installing npm dependencies..."
npm install

echo "🛠️ Building..."
dotnet publish cutypai.csproj -c Release -o /home/main/apps/cutypai/publish

echo "🔄 Restarting service..."
sudo systemctl restart cutypai

echo "✅ Deploy complete!"
