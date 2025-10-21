# AlgoTrendy API Documentation

## Postman Collection

### Quick Start

1. **Import the Collection**
   - Open Postman
   - Click "Import" → Select `AlgoTrendy_API.postman_collection.json`
   - Collection will appear in your workspace

2. **Configure Environment**
   - Set `baseUrl` variable (default: `http://localhost:5002`)
   - Set `apiVersion` variable (default: `v1`)

3. **Start Testing**
   - Expand the collection folders
   - Select an endpoint
   - Click "Send"

### Available Endpoints

The collection includes endpoints for:

- **Market Data** - Get historical and real-time market data
- **Trading** - Place orders, manage positions
- **Backtesting** - Run strategy backtests
- **Portfolio** - View portfolio performance
- **Strategies** - Manage trading strategies
- **Webhooks** - TradingView webhook integration
- **Admin** - System health and configuration

### Authentication

For protected endpoints:
- Add `Authorization` header with Bearer token
- Token can be obtained from login endpoint

### Interactive Documentation

For detailed API documentation with live testing:
- Start the API: `dotnet run --project backend/AlgoTrendy.API`
- Open Swagger UI: http://localhost:5002/swagger

### Additional Resources

- Full API Usage Examples: `docs/API_USAGE_EXAMPLES.md`
- Swagger Documentation: http://localhost:5002/swagger
- GitHub Repository: https://github.com/KenyBoi/algotrendy-v2.6

### Support

For issues or questions:
- Open an issue on GitHub
- Check the main documentation at `/docs/README.md`
