"""
Pydantic models for backtesting configuration and results
"""

from typing import Dict, List, Optional, Any, Literal
from pydantic import BaseModel, Field, validator
from datetime import datetime
from enum import Enum


class BacktestStatus(str, Enum):
    """Backtest execution status"""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"


class AssetClass(str, Enum):
    """Supported asset classes"""
    CRYPTO = "crypto"
    FUTURES = "futures"
    EQUITIES = "equities"


class TimeframeType(str, Enum):
    """Supported timeframe types"""
    TICK = "tick"
    MINUTE = "min"
    HOUR = "hr"
    DAY = "day"
    WEEK = "wk"
    MONTH = "mo"
    RENKO = "renko"
    LINE = "line"
    RANGE = "range"


class BacktesterEngine(str, Enum):
    """Supported backtesting engines"""
    QUANTCONNECT = "quantconnect"
    BACKTESTER = "backtester"
    CUSTOM = "custom"


class IndicatorConfig(BaseModel):
    """Configuration for a single indicator"""
    name: str
    enabled: bool = False
    params: Dict[str, Any] = Field(default_factory=dict)


class BacktestConfig(BaseModel):
    """Configuration for running a backtest"""

    # AI and Engine Selection
    ai_name: str = Field(default="MemGPT AI v1", description="AI model to use")
    backtester: BacktesterEngine = Field(default=BacktesterEngine.CUSTOM, description="Backtesting engine")

    # Asset Selection
    asset_class: AssetClass = Field(description="Asset class to trade")
    symbol: str = Field(description="Trading symbol (e.g., BTCUSDT, ES, AAPL)")

    # Timeframe Configuration
    timeframe: TimeframeType = Field(description="Timeframe type")
    timeframe_value: Optional[int] = Field(default=1, description="Timeframe value (for min/hr)")

    # Date Range
    start_date: str = Field(description="Backtest start date (YYYY-MM-DD)")
    end_date: str = Field(description="Backtest end date (YYYY-MM-DD)")

    # Capital
    initial_capital: float = Field(default=10000.0, gt=0, description="Initial capital in USD")

    # Indicators
    indicators: Dict[str, bool] = Field(
        default_factory=lambda: {
            "sma": False,
            "ema": False,
            "rsi": False,
            "macd": False,
            "bollinger": False,
            "atr": False,
            "stochastic": False,
            "volume": False,
        },
        description="Enabled indicators"
    )

    indicator_params: Dict[str, Any] = Field(
        default_factory=lambda: {
            "sma_period": 20,
            "ema_period": 12,
            "rsi_period": 14,
            "macd_fast": 12,
            "macd_slow": 26,
            "macd_signal": 9,
            "bollinger_period": 20,
            "bollinger_std": 2,
            "atr_period": 14,
            "stochastic_k": 14,
            "stochastic_d": 3,
        },
        description="Indicator parameters"
    )

    # Advanced Options
    commission: float = Field(default=0.001, description="Commission per trade (0.1% = 0.001)")
    slippage: float = Field(default=0.0005, description="Slippage per trade (0.05% = 0.0005)")

    @validator('start_date', 'end_date')
    def validate_date_format(cls, v):
        """Validate date format"""
        try:
            datetime.strptime(v, '%Y-%m-%d')
        except ValueError:
            raise ValueError('Date must be in YYYY-MM-DD format')
        return v

    @validator('end_date')
    def validate_date_range(cls, v, values):
        """Ensure end_date is after start_date"""
        if 'start_date' in values:
            start = datetime.strptime(values['start_date'], '%Y-%m-%d')
            end = datetime.strptime(v, '%Y-%m-%d')
            if end <= start:
                raise ValueError('end_date must be after start_date')
        return v


class TradeResult(BaseModel):
    """Result of a single trade"""
    entry_time: datetime
    exit_time: Optional[datetime] = None
    entry_price: float
    exit_price: Optional[float] = None
    quantity: float
    side: Literal["long", "short"]
    pnl: Optional[float] = None
    pnl_percent: Optional[float] = None
    duration_minutes: Optional[int] = None
    exit_reason: Optional[str] = None


class EquityPoint(BaseModel):
    """Single point in equity curve"""
    timestamp: datetime
    equity: float
    cash: float
    positions_value: float
    drawdown: float = 0.0


class BacktestMetrics(BaseModel):
    """Performance metrics from backtest"""
    total_return: float = Field(description="Total return %")
    annual_return: float = Field(description="Annualized return %")
    sharpe_ratio: float = Field(description="Sharpe ratio")
    sortino_ratio: float = Field(description="Sortino ratio")
    max_drawdown: float = Field(description="Maximum drawdown %")
    win_rate: float = Field(description="Win rate %")
    profit_factor: float = Field(description="Profit factor")
    total_trades: int = Field(description="Total number of trades")
    winning_trades: int = Field(description="Number of winning trades")
    losing_trades: int = Field(description="Number of losing trades")
    avg_win: float = Field(description="Average winning trade")
    avg_loss: float = Field(description="Average losing trade")
    largest_win: float = Field(description="Largest winning trade")
    largest_loss: float = Field(description="Largest losing trade")
    avg_trade_duration: float = Field(description="Average trade duration in hours")


class BacktestResults(BaseModel):
    """Complete backtest results"""

    # Identification
    backtest_id: str = Field(description="Unique backtest ID")
    status: BacktestStatus = Field(description="Backtest status")

    # Configuration Used
    config: BacktestConfig = Field(description="Configuration used for this backtest")

    # Execution Info
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
    execution_time_seconds: Optional[float] = None

    # Results
    metrics: Optional[BacktestMetrics] = None
    equity_curve: List[EquityPoint] = Field(default_factory=list)
    trades: List[TradeResult] = Field(default_factory=list)

    # Indicators Used
    indicators_used: List[str] = Field(default_factory=list)

    # Error Information (if failed)
    error_message: Optional[str] = None
    error_details: Optional[Dict[str, Any]] = None

    # Additional Data
    metadata: Dict[str, Any] = Field(default_factory=dict)


class BacktestConfigOptions(BaseModel):
    """Available configuration options for backtesting"""

    ai_models: List[Dict[str, str]] = Field(
        default_factory=lambda: [
            {"value": "memgpt_v1", "label": "MemGPT AI v1", "status": "available"},
            {"value": "memgpt_v2", "label": "MemGPT AI v2", "status": "coming_soon"},
        ]
    )

    backtesting_engines: List[Dict[str, str]] = Field(
        default_factory=lambda: [
            {"value": "quantconnect", "label": "QuantConnect", "status": "available"},
            {"value": "backtester", "label": "Backtester.com", "status": "available"},
            {"value": "custom", "label": "Custom Engine", "status": "available"},
        ]
    )

    asset_classes: List[Dict[str, Any]] = Field(
        default_factory=lambda: [
            {
                "value": "crypto",
                "label": "Cryptocurrency",
                "symbols": ["BTCUSDT", "ETHUSDT", "SOLUSDT", "ADAUSDT", "XRPUSDT", "BNBUSDT", "DOGEUSDT", "MATICUSDT", "LINKUSDT", "AVAXUSDT"]
            },
            {
                "value": "futures",
                "label": "Futures",
                "symbols": ["ES", "NQ", "YM", "RTY", "CL", "GC", "SI", "ZB", "ZN", "ZF"]
            },
            {
                "value": "equities",
                "label": "Equities",
                "symbols": ["AAPL", "GOOGL", "MSFT", "AMZN", "TSLA", "NVDA", "META", "NFLX", "AMD", "INTC"]
            },
        ]
    )

    timeframe_types: List[Dict[str, Any]] = Field(
        default_factory=lambda: [
            {"value": "tick", "label": "Tick", "needs_value": False},
            {"value": "min", "label": "Minute", "needs_value": True},
            {"value": "hr", "label": "Hour", "needs_value": True},
            {"value": "day", "label": "Day", "needs_value": False},
            {"value": "wk", "label": "Week", "needs_value": False},
            {"value": "mo", "label": "Month", "needs_value": False},
            {"value": "renko", "label": "Renko", "needs_value": True, "value_label": "Brick Size"},
            {"value": "line", "label": "Line Break", "needs_value": True, "value_label": "Lines"},
            {"value": "range", "label": "Range", "needs_value": True, "value_label": "Range Size"},
        ]
    )


class BacktestHistoryItem(BaseModel):
    """Summary of a past backtest"""
    backtest_id: str
    symbol: str
    asset_class: str
    timeframe: str
    start_date: str
    end_date: str
    status: BacktestStatus
    total_return: Optional[float] = None
    sharpe_ratio: Optional[float] = None
    total_trades: Optional[int] = None
    created_at: datetime
