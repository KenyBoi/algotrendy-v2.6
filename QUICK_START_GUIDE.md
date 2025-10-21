# AlgoTrendy v2.6 - Quick Start Guide

**Get started with AlgoTrendy in under 5 minutes!**

---

## 🚀 Fastest Way to Start (Recommended)

### Docker (30 seconds)

```bash
git clone https://github.com/KenyBoi/algotrendy-v2.6.git
cd algotrendy-v2.6
cp .env.example .env
docker-compose up -d
```

**Access the application:**
- Frontend: http://localhost:3000
- API: http://localhost:5002
- Swagger Docs: http://localhost:5002/swagger
- QuestDB Console: http://localhost:9000
- Logs (Seq): http://localhost:5341

📘 **Full Docker Guide**: [DOCKER_SETUP.md](DOCKER_SETUP.md)

---

## 🛠️ Development Setup (5 minutes)

### Automated Setup Script

```bash
./scripts/dev-setup.sh
```

This script automatically:
- ✅ Checks prerequisites (.NET, Docker, Node.js, Python)
- ✅ Restores dependencies
- ✅ Builds projects
- ✅ Sets up databases
- ✅ Configures environment
- ✅ Runs migrations
- ✅ Sets up user secrets

---

## 📖 Common Tasks

### Running the Application

**Option 1: Docker**
```bash
docker-compose up -d
docker-compose logs -f api  # View logs
```

**Option 2: Direct .NET Run**
```bash
cd backend/AlgoTrendy.API
dotnet run
```

### Running Tests

```bash
cd backend
dotnet test
```

### Building the Project

```bash
cd backend
dotnet build
```

### Viewing Logs

```bash
# Real-time logs from Seq
open http://localhost:5341

# Docker container logs
docker-compose logs -f api
docker-compose logs -f questdb
```

---

## 🔑 Configuration

### Quick Credential Setup

```bash
./scripts/setup/quick_setup_credentials.sh
```

### Manual Credential Setup

Edit `.env` file:

```bash
# Required for trading
BINANCE_API_KEY=your_key_here
BINANCE_API_SECRET=your_secret_here
BINANCE_TESTNET=true

# Optional for backtesting
QUANTCONNECT_USER_ID=your_user_id
QUANTCONNECT_API_TOKEN=your_token
```

📘 **Full Setup Guide**: [docs/deployment/credentials-setup-guide.md](docs/deployment/credentials-setup-guide.md)

---

## 💻 API Integration

### Python Example

```python
import requests

# Get market data
response = requests.get('http://localhost:5002/api/marketdata/BTCUSDT')
data = response.json()
print(f"BTC Price: ${data[0]['close']}")

# Place order
order = {
    'symbol': 'BTCUSDT',
    'side': 'Buy',
    'type': 'Market',
    'quantity': 0.001
}
response = requests.post('http://localhost:5002/api/orders', json=order)
print(f"Order ID: {response.json()['orderId']}")
```

### cURL Example

```bash
# Get market data
curl http://localhost:5002/api/marketdata/BTCUSDT

# Place order
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "type": "Market",
    "quantity": 0.001
  }'
```

📘 **Complete Examples**: [docs/API_USAGE_EXAMPLES.md](docs/API_USAGE_EXAMPLES.md) (Python, JavaScript, C#, cURL)

---

## 🔧 Troubleshooting

### Service Won't Start

```bash
# Check Docker is running
docker ps

# View error logs
docker-compose logs service-name

# Restart services
docker-compose restart
```

### Port Already in Use

```bash
# Find process using port
sudo lsof -i :5002

# Kill process or change port in docker-compose.yml
```

### Database Connection Failed

```bash
# Verify QuestDB is running
curl http://localhost:9000

# Restart database
docker-compose restart questdb
```

📘 **Full Troubleshooting**: [DOCKER_SETUP.md#troubleshooting](DOCKER_SETUP.md#troubleshooting)

---

## 📚 Next Steps

### For Users
1. ✅ Application running
2. Configure broker credentials in `.env`
3. Explore the API at http://localhost:5002/swagger
4. View real-time logs at http://localhost:5341
5. Start trading!

### For Developers
1. ✅ Development environment set up
2. Read [CONTRIBUTING.md](CONTRIBUTING.md)
3. Check [TODO Tree](docs/developer/todo-tree.md) for tasks
4. Review [Architecture](docs/ARCHITECTURE.md)
5. Submit your first PR!

### For API Integrators
1. ✅ API accessible
2. Review [API Usage Examples](docs/API_USAGE_EXAMPLES.md)
3. Test with Python/JavaScript/C# examples
4. Build your integration
5. Share your implementation!

---

## 📖 Essential Documentation

| Guide | Purpose | Time |
|-------|---------|------|
| [DOCKER_SETUP.md](DOCKER_SETUP.md) | Complete Docker deployment | 5 min |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Development guidelines | 10 min |
| [API_USAGE_EXAMPLES.md](docs/API_USAGE_EXAMPLES.md) | API integration examples | 15 min |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture | 20 min |
| [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) | Production deployment | 30 min |

---

## 🆘 Get Help

### Resources
- **Documentation**: [docs/](docs/)
- **API Docs**: http://localhost:5002/swagger (when running)
- **Issues**: https://github.com/KenyBoi/algotrendy-v2.6/issues
- **Discussions**: https://github.com/KenyBoi/algotrendy-v2.6/discussions

### Common Questions

**Q: How do I add a new broker?**  
A: See [CONTRIBUTING.md#adding-new-components](CONTRIBUTING.md#adding-new-components)

**Q: How do I run backtests?**  
A: Use the `/api/backtest/run` endpoint. See [API_USAGE_EXAMPLES.md](docs/API_USAGE_EXAMPLES.md#10-run-backtest)

**Q: How do I deploy to production?**  
A: See [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)

**Q: Where are my logs?**  
A: View in Seq at http://localhost:5341 or `docker-compose logs`

---

## ⚡ Quick Commands Reference

```bash
# Start everything
docker-compose up -d

# Stop everything
docker-compose down

# View logs
docker-compose logs -f

# Restart API
docker-compose restart api

# Run tests
dotnet test

# Build project
dotnet build

# Format code
dotnet format

# Update dependencies
dotnet restore

# Access database
psql -h localhost -p 8812 -U admin -d qdb

# Clean and rebuild
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

---

**Ready to start?** Run `docker-compose up -d` or `./scripts/dev-setup.sh` 🚀

*Last Updated: October 21, 2025*
