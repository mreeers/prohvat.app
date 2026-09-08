#!/usr/bin/env bash
set -e

echo "=========================================="
echo "🚀 Deploying Prohvat App Infrastructure"
echo "=========================================="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [ -d /home/alex/prohvat.git ]; then
    echo "📥 Updating from local git bare repo..."
    git --git-dir=/home/alex/prohvat.git checkout -f main || true
elif [ -d .git ]; then
    echo "📥 Pulling latest git commits..."
    git pull origin main || true
fi

echo "🐳 Building and starting containers..."
docker compose up -d --build --remove-orphans

echo "🧹 Cleaning up dangling images..."
docker image prune -f

echo "=========================================="
echo "✅ Deployment completed successfully!"
echo "=========================================="
docker compose ps