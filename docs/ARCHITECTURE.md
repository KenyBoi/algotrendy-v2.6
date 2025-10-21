# AlgoTrendy v2.6 - System Architecture

This document provides visual diagrams and explanations of the AlgoTrendy system architecture.

## Table of Contents

- [System Overview](#system-overview)
- [Component Architecture](#component-architecture)
- [Data Flow Diagrams](#data-flow-diagrams)
- [Sequence Diagrams](#sequence-diagrams)
- [Deployment Architecture](#deployment-architecture)
- [Database Schema](#database-schema)

---

## System Overview

```mermaid
graph TB
    subgraph "Client Layer"
        WEB[Web Browser]
        MOBILE[Mobile App]
        API_CLIENT[API Clients]
    end

    subgraph "Load Balancer"
        NGINX[Nginx<br/>SSL Termination<br/>Reverse Proxy]
    end

    subgraph "Application Layer"
        API[.NET 8 API<br/>AlgoTrendy.API]
        SIGNALR[SignalR Hub<br/>Real-time WebSocket]
    end

    subgraph "Service Layer"
        BROKER_SVC[Broker Service]
        MARKET_SVC[Market Data Service]
        ORDER_SVC[Order Service]
        BACKTEST_SVC[Backtest Service]
        PORT_SVC[Portfolio Service]
    end

    subgraph "Integration Layer"
        BYBIT[Bybit Broker]
        BINANCE[Binance Broker]
        MEXC[MEXC Broker]
        IB[Interactive Brokers]
        QC[QuantConnect API]
        BTPY[Backtesting.py Service]
        ML[ML Service]
    end

    subgraph "Data Layer"
        QUESTDB[(QuestDB<br/>Time-Series Data)]
        REDIS[(Redis<br/>Caching)]
        SEQ[Seq<br/>Structured Logging]
    end

    WEB --> NGINX
    MOBILE --> NGINX
    API_CLIENT --> NGINX

    NGINX --> API
    NGINX --> SIGNALR

    API --> BROKER_SVC
    API --> MARKET_SVC
    API --> ORDER_SVC
    API --> BACKTEST_SVC
    API --> PORT_SVC

    SIGNALR --> MARKET_SVC

    BROKER_SVC --> BYBIT
    BROKER_SVC --> BINANCE
    BROKER_SVC --> MEXC
    BROKER_SVC --> IB

    BACKTEST_SVC --> QC
    BACKTEST_SVC --> BTPY

    MARKET_SVC --> ML

    API --> QUESTDB
    API --> REDIS
    API --> SEQ

    BYBIT -.Market Data.-> QUESTDB
    BINANCE -.Market Data.-> QUESTDB

    style WEB fill:#e1f5ff
    style NGINX fill:#ffe1cc
    style API fill:#d4edda
    style QUESTDB fill:#fff3cd
```

---

## Component Architecture

### Backend Components

```mermaid
graph LR
    subgraph "AlgoTrendy Backend"
        API[AlgoTrendy.API<br/>Web API Layer]
        CORE[AlgoTrendy.Core<br/>Business Logic]
        BROKERS[AlgoTrendy.Brokers<br/>Broker Integrations]
        BACKTEST[AlgoTrendy.Backtesting<br/>Backtest Engine]
        CHANNELS[AlgoTrendy.DataChannels<br/>Market Data Providers]
        TESTS[AlgoTrendy.Tests<br/>Unit & Integration Tests]
    end

    API --> CORE
    API --> BROKERS
    API --> BACKTEST
    API --> CHANNELS

    CORE --> BROKERS
    CORE --> CHANNELS

    BACKTEST --> CHANNELS

    TESTS -.Tests.-> API
    TESTS -.Tests.-> CORE
    TESTS -.Tests.-> BROKERS

    style API fill:#d4edda
    style CORE fill:#cfe2ff
    style BROKERS fill:#f8d7da
    style BACKTEST fill:#fff3cd
```

### Service Architecture

```mermaid
graph TB
    subgraph "Core Services"
        AUTH[Authentication<br/>Service]
        USER[User Management<br/>Service]
        PORT[Portfolio<br/>Service]
    end

    subgraph "Trading Services"
        ORDER[Order<br/>Service]
        RISK[Risk Management<br/>Service]
        STRATEGY[Strategy<br/>Service]
    end

    subgraph "Data Services"
        MARKET[Market Data<br/>Service]
        HIST[Historical Data<br/>Service]
        STREAM[Streaming Data<br/>Service]
    end

    subgraph "Analysis Services"
        BACKTEST[Backtesting<br/>Service]
        ANALYTICS[Analytics<br/>Service]
        ML_SVC[ML Prediction<br/>Service]
    end

    AUTH --> USER
    PORT --> ORDER
    ORDER --> RISK
    RISK --> STRATEGY

    MARKET --> HIST
    MARKET --> STREAM

    BACKTEST --> HIST
    ANALYTICS --> ML_SVC

    ORDER --> MARKET
    STRATEGY --> MARKET

    style AUTH fill:#d4edda
    style ORDER fill:#fff3cd
    style MARKET fill:#cfe2ff
    style BACKTEST fill:#f8d7da
```

---

## Data Flow Diagrams

### Market Data Flow

```mermaid
flowchart LR
    EXCHANGE1[Binance<br/>Exchange]
    EXCHANGE2[Bybit<br/>Exchange]
    EXCHANGE3[OKX<br/>Exchange]

    REST[REST API<br/>Channels]
    WS[WebSocket<br/>Channels]

    AGGREGATOR[Data<br/>Aggregator]
    PROCESSOR[Data<br/>Processor]

    CACHE[(Redis<br/>Cache)]
    DB[(QuestDB<br/>Database)]

    SIGNALR[SignalR<br/>Hub]
    CLIENT[Web<br/>Clients]

    EXCHANGE1 --> REST
    EXCHANGE2 --> REST
    EXCHANGE3 --> REST

    EXCHANGE1 --> WS
    EXCHANGE2 --> WS

    REST --> AGGREGATOR
    WS --> AGGREGATOR

    AGGREGATOR --> PROCESSOR
    PROCESSOR --> CACHE
    PROCESSOR --> DB

    PROCESSOR --> SIGNALR
    SIGNALR --> CLIENT

    CACHE -.Read.-> CLIENT

    style EXCHANGE1 fill:#ffe1cc
    style PROCESSOR fill:#d4edda
    style DB fill:#fff3cd
    style CLIENT fill:#e1f5ff
```

### Order Execution Flow

```mermaid
flowchart TD
    START([User Places Order])

    VALIDATE{Validate<br/>Order}
    RISK{Risk<br/>Check}
    BROKER{Select<br/>Broker}

    SUBMIT[Submit to<br/>Broker]
    WAIT[Wait for<br/>Confirmation]

    CONFIRM{Order<br/>Confirmed?}
    UPDATE[Update<br/>Portfolio]
    NOTIFY[Notify<br/>User]

    ERROR_VALID[Validation<br/>Error]
    ERROR_RISK[Risk<br/>Rejected]
    ERROR_BROKER[Broker<br/>Error]

    END([Complete])

    START --> VALIDATE
    VALIDATE -->|Valid| RISK
    VALIDATE -->|Invalid| ERROR_VALID

    RISK -->|Pass| BROKER
    RISK -->|Fail| ERROR_RISK

    BROKER --> SUBMIT
    SUBMIT --> WAIT

    WAIT --> CONFIRM
    CONFIRM -->|Yes| UPDATE
    CONFIRM -->|No| ERROR_BROKER

    UPDATE --> NOTIFY
    NOTIFY --> END

    ERROR_VALID --> END
    ERROR_RISK --> END
    ERROR_BROKER --> END

    style START fill:#d4edda
    style VALIDATE fill:#fff3cd
    style RISK fill:#fff3cd
    style UPDATE fill:#cfe2ff
    style ERROR_VALID fill:#f8d7da
    style ERROR_RISK fill:#f8d7da
    style ERROR_BROKER fill:#f8d7da
```

---

## Sequence Diagrams

### User Login & Authentication

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant AuthService
    participant Database
    participant TokenService

    User->>Frontend: Enter credentials
    Frontend->>API: POST /api/auth/login
    activate API

    API->>AuthService: Authenticate(username, password)
    activate AuthService

    AuthService->>Database: Query user
    Database-->>AuthService: User data

    AuthService->>AuthService: Verify password hash

    alt Credentials Valid
        AuthService->>TokenService: Generate JWT token
        TokenService-->>AuthService: JWT token
        AuthService-->>API: Authentication success + token
        API-->>Frontend: 200 OK + token
        Frontend->>Frontend: Store token
        Frontend-->>User: Login successful
    else Credentials Invalid
        AuthService-->>API: Authentication failed
        API-->>Frontend: 401 Unauthorized
        Frontend-->>User: Show error
    end

    deactivate AuthService
    deactivate API
```

### Place Market Order

```mermaid
sequenceDiagram
    actor Trader
    participant Frontend
    participant API
    participant OrderService
    participant RiskService
    participant BrokerService
    participant Bybit
    participant Database
    participant SignalR

    Trader->>Frontend: Place market order
    Frontend->>API: POST /api/orders/market
    activate API

    API->>OrderService: ProcessMarketOrder()
    activate OrderService

    OrderService->>RiskService: ValidateOrder()
    activate RiskService
    RiskService->>Database: Check portfolio limits
    Database-->>RiskService: Current positions
    RiskService-->>OrderService: Risk check passed
    deactivate RiskService

    OrderService->>BrokerService: ExecuteOrder()
    activate BrokerService
    BrokerService->>Bybit: Submit order
    Bybit-->>BrokerService: Order confirmed
    BrokerService-->>OrderService: Execution result
    deactivate BrokerService

    OrderService->>Database: Save order
    OrderService->>Database: Update portfolio

    OrderService->>SignalR: Broadcast order update
    SignalR-->>Frontend: Real-time update

    OrderService-->>API: Order result
    deactivate OrderService

    API-->>Frontend: 200 OK + order details
    deactivate API

    Frontend-->>Trader: Order confirmation
```

### Backtest Execution

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant BacktestService
    participant QuantConnect
    participant HistoricalData
    participant MLService
    participant Database

    User->>Frontend: Submit backtest
    Frontend->>API: POST /api/backtest/run
    activate API

    API->>BacktestService: RunBacktest()
    activate BacktestService

    BacktestService->>QuantConnect: Create project
    QuantConnect-->>BacktestService: Project ID

    BacktestService->>QuantConnect: Upload algorithm
    BacktestService->>HistoricalData: Fetch historical data
    HistoricalData-->>BacktestService: Market data

    BacktestService->>QuantConnect: Start backtest
    activate QuantConnect

    Note over QuantConnect: Running backtest...<br/>This may take several minutes

    QuantConnect-->>BacktestService: Backtest complete
    deactivate QuantConnect

    BacktestService->>QuantConnect: Get results
    QuantConnect-->>BacktestService: Results data

    BacktestService->>MLService: Analyze results
    activate MLService
    MLService-->>BacktestService: AI insights
    deactivate MLService

    BacktestService->>Database: Save results
    BacktestService-->>API: Complete results
    deactivate BacktestService

    API-->>Frontend: 200 OK + results
    deactivate API

    Frontend-->>User: Display results & analysis
```

---

## Deployment Architecture

### Production Deployment (Docker)

```mermaid
graph TB
    subgraph "Internet"
        USERS[Users]
        DNS[DNS<br/>algotrendy.com]
    end

    subgraph "VPS Server - Ubuntu 22.04"
        subgraph "Docker Containers"
            NGINX[Nginx<br/>:80, :443]
            FRONTEND[Frontend<br/>React App]
            API[API<br/>:5002]
            QUESTDB[QuestDB<br/>:9000, :8812]
            SEQ[Seq<br/>:5341]
            ML[ML Service<br/>:5003]
            BTPY[Backtest.py<br/>:5004]
            CERTBOT[Certbot<br/>SSL Renewal]
        end

        subgraph "Volumes"
            DB_VOL[questdb_data]
            SSL_VOL[ssl_certs]
            LOG_VOL[api_logs]
        end
    end

    subgraph "External Services"
        BYBIT_EX[Bybit Exchange]
        BINANCE_EX[Binance Exchange]
        QC_CLOUD[QuantConnect Cloud]
    end

    USERS --> DNS
    DNS --> NGINX

    NGINX --> FRONTEND
    NGINX --> API

    API --> QUESTDB
    API --> SEQ
    API --> ML
    API --> BTPY

    QUESTDB --> DB_VOL
    NGINX --> SSL_VOL
    API --> LOG_VOL

    API --> BYBIT_EX
    API --> BINANCE_EX
    API --> QC_CLOUD

    CERTBOT -.Renews.-> SSL_VOL

    style USERS fill:#e1f5ff
    style NGINX fill:#ffe1cc
    style API fill:#d4edda
    style QUESTDB fill:#fff3cd
```

### Network Topology

```mermaid
graph TB
    subgraph "Docker Network: algotrendy-network"
        subgraph "172.20.0.0/16"
            QUESTDB["QuestDB<br/>172.20.0.10"]
            API["API<br/>172.20.0.20"]
            FRONTEND["Frontend<br/>172.20.0.25"]
            NGINX["Nginx<br/>172.20.0.30"]
            SEQ["Seq<br/>172.20.0.40"]
            ML["ML Service<br/>172.20.0.50"]
            BTPY["Backtest.py<br/>172.20.0.55"]
        end
    end

    subgraph "Host Network"
        HOST["Host<br/>216.238.90.131"]
    end

    subgraph "Internet"
        CLIENT[Clients]
    end

    CLIENT -->|:80, :443| HOST
    HOST -->|Port Mapping| NGINX

    NGINX -->|Internal| FRONTEND
    NGINX -->|Internal| API

    API -->|Internal| QUESTDB
    API -->|Internal| SEQ
    API -->|Internal| ML
    API -->|Internal| BTPY

    style CLIENT fill:#e1f5ff
    style HOST fill:#ffe1cc
    style NGINX fill:#d4edda
```

---

## Database Schema

### QuestDB Tables

```mermaid
erDiagram
    MARKET_DATA_1M {
        timestamp timestamp PK
        symbol string PK
        open double
        high double
        low double
        close double
        volume double
        quote_volume double
        trades_count int
        source string
        metadata_json string
    }

    MARKET_DATA_5M {
        timestamp timestamp PK
        symbol string PK
        open double
        high double
        low double
        close double
        volume double
        quote_volume double
        trades_count int
        source string
        metadata_json string
    }

    MARKET_DATA_1H {
        timestamp timestamp PK
        symbol string PK
        open double
        high double
        low double
        close double
        volume double
        quote_volume double
        trades_count int
        source string
        metadata_json string
    }

    ORDERS {
        order_id string PK
        user_id string
        symbol string
        side string
        order_type string
        quantity double
        price double
        status string
        filled_quantity double
        avg_fill_price double
        broker string
        created_at timestamp
        updated_at timestamp
        metadata_json string
    }

    POSITIONS {
        position_id string PK
        user_id string
        symbol string
        quantity double
        avg_entry_price double
        current_price double
        unrealized_pnl double
        realized_pnl double
        broker string
        opened_at timestamp
        updated_at timestamp
    }

    BACKTEST_RESULTS {
        backtest_id string PK
        user_id string
        algorithm_name string
        start_date date
        end_date date
        initial_capital double
        final_capital double
        total_return double
        sharpe_ratio double
        max_drawdown double
        win_rate double
        total_trades int
        engine_type string
        created_at timestamp
        results_json string
    }

    ORDERS ||--o{ POSITIONS : creates
    MARKET_DATA_1M ||--o{ ORDERS : informs
```

---

## Technology Stack

```mermaid
mindmap
  root((AlgoTrendy<br/>Tech Stack))
    Backend
      .NET 8.0
      ASP.NET Core
      Entity Framework
      SignalR
      Serilog
    Frontend
      React 18
      Vite
      TypeScript
      Tailwind CSS
      Shadcn/UI
    Data Storage
      QuestDB
      Redis
      PostgreSQL
    DevOps
      Docker
      Docker Compose
      Nginx
      Let's Encrypt
      GitHub Actions
    External APIs
      Binance.Net
      Bybit.Net
      QuantConnect
      Various Brokers
    Testing
      xUnit
      Moq
      Testcontainers
      Integration Tests
```

---

## Security Architecture

```mermaid
flowchart TB
    subgraph "Client Security"
        HTTPS[HTTPS/TLS 1.3]
        CSRF[CSRF Protection]
        XSS[XSS Prevention]
    end

    subgraph "API Security"
        JWT[JWT Authentication]
        AUTHZ[Role-Based Authorization]
        RATELIMIT[Rate Limiting]
        VALIDATION[Input Validation]
    end

    subgraph "Data Security"
        ENCRYPT[Data Encryption<br/>at Rest]
        SECRETS[User Secrets<br/>Management]
        VAULT[Secure Credential<br/>Storage]
    end

    subgraph "Network Security"
        FIREWALL[Firewall Rules]
        CORS[CORS Policy]
        SSL[SSL Certificates]
        IPWHITE[IP Whitelisting]
    end

    subgraph "Monitoring"
        LOGGING[Structured Logging]
        AUDIT[Audit Trail]
        ALERTS[Security Alerts]
    end

    HTTPS --> JWT
    CSRF --> JWT
    XSS --> VALIDATION

    JWT --> AUTHZ
    AUTHZ --> RATELIMIT
    RATELIMIT --> VALIDATION

    VALIDATION --> ENCRYPT
    SECRETS --> VAULT

    FIREWALL --> CORS
    CORS --> SSL
    SSL --> IPWHITE

    VALIDATION --> LOGGING
    AUTHZ --> AUDIT
    AUDIT --> ALERTS

    style HTTPS fill:#d4edda
    style JWT fill:#cfe2ff
    style ENCRYPT fill:#fff3cd
    style FIREWALL fill:#ffe1cc
```

---

## Scaling Strategy

```mermaid
graph LR
    subgraph "Current (Single Server)"
        SINGLE[All Services<br/>on One VPS]
    end

    subgraph "Phase 1: Vertical Scaling"
        BIGGER[Larger VPS<br/>More CPU/RAM]
    end

    subgraph "Phase 2: Service Separation"
        DB_SERVER[Database Server]
        API_SERVER[API Server]
        CACHE_SERVER[Cache Server]
    end

    subgraph "Phase 3: Horizontal Scaling"
        LB[Load Balancer]
        API1[API Instance 1]
        API2[API Instance 2]
        API3[API Instance 3]
        DB_CLUSTER[DB Cluster]
    end

    subgraph "Phase 4: Cloud Migration"
        CDN[CDN]
        K8S[Kubernetes Cluster]
        MANAGED_DB[Managed Database]
        AUTO_SCALE[Auto-scaling]
    end

    SINGLE --> BIGGER
    BIGGER --> DB_SERVER
    DB_SERVER --> LB
    LB --> CDN

    style SINGLE fill:#f8d7da
    style BIGGER fill:#fff3cd
    style DB_SERVER fill:#cfe2ff
    style LB fill:#d4edda
    style CDN fill:#e1f5ff
```

---

## Monitoring & Observability

```mermaid
graph TB
    subgraph "Application Metrics"
        API_METRICS[API Latency<br/>Request Rate<br/>Error Rate]
        ORDER_METRICS[Order Success Rate<br/>Execution Time<br/>Slippage]
        DATA_METRICS[Data Freshness<br/>Source Availability<br/>Quote Count]
    end

    subgraph "Infrastructure Metrics"
        CPU[CPU Usage]
        MEMORY[Memory Usage]
        DISK[Disk I/O]
        NETWORK[Network Traffic]
    end

    subgraph "Business Metrics"
        TRADES[Daily Trades]
        VOLUME[Trading Volume]
        PNL[P&L Tracking]
        USERS[Active Users]
    end

    subgraph "Logging"
        SEQ_LOG[Seq Dashboard]
        FILE_LOG[File Logs]
        STRUCTURED[Structured JSON]
    end

    subgraph "Alerting"
        ERRORS[Error Alerts]
        PERF[Performance Alerts]
        SECURITY[Security Alerts]
        BUSINESS_ALERT[Business Alerts]
    end

    API_METRICS --> SEQ_LOG
    ORDER_METRICS --> SEQ_LOG
    DATA_METRICS --> SEQ_LOG

    CPU --> PERF
    MEMORY --> PERF

    API_METRICS --> ERRORS
    ORDER_METRICS --> BUSINESS_ALERT

    SEQ_LOG --> STRUCTURED
    STRUCTURED --> FILE_LOG

    ERRORS --> SECURITY

    style SEQ_LOG fill:#cfe2ff
    style ERRORS fill:#f8d7da
    style BUSINESS_ALERT fill:#fff3cd
```

---

## Summary

This architecture provides:

✅ **Scalability** - Horizontal scaling capabilities
✅ **Reliability** - Multiple data sources and fallbacks
✅ **Security** - Multi-layer security implementation
✅ **Performance** - Caching and optimized data flow
✅ **Maintainability** - Clean separation of concerns
✅ **Observability** - Comprehensive logging and monitoring

For implementation details, see:
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines
- [API_USAGE_EXAMPLES.md](API_USAGE_EXAMPLES.md) - API integration examples
- [SWAGGER_ENHANCEMENT_GUIDE.md](SWAGGER_ENHANCEMENT_GUIDE.md) - API documentation

---

*Last Updated: October 21, 2025*
