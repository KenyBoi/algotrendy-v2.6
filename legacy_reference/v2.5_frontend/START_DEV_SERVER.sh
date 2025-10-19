#!/bin/bash

echo "🚀 Starting AlgoTrendy Web Application..."
echo ""
echo "Step 1: Installing dependencies..."
npm install

echo ""
echo "Step 2: Building project..."
npm run build

echo ""
echo "Step 3: Starting development server..."
echo "📱 Application will be available at: http://localhost:3000"
echo ""
npm start
