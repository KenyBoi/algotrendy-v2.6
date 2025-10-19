"""
MEM Strategy Modules - Additional trading strategies for MEM to use

Add new strategy classes here that MEM can integrate into his trading system.
Each strategy should follow the standard interface for easy integration.
"""

class StrategyTemplate:
    """Template for creating new strategies for MEM"""
    
    def __init__(self, parameters=None):
        self.parameters = parameters or {}
        self.name = "Template Strategy"
        
    def generate_signal(self, market_data):
        """
        Generate trading signal based on market data
        
        Args:
            market_data: Dict containing OHLCV and indicator data
            
        Returns:
            Dict: {"action": "buy/sell/hold", "confidence": 0.0-1.0, "reason": "explanation"}
        """
        return {"action": "hold", "confidence": 0.0, "reason": "Template - no logic implemented"}
        
    def calculate_position_size(self, account_balance, confidence):
        """Calculate position size based on confidence and account balance"""
        base_size = account_balance * 0.02  # 2% risk
        return base_size * confidence
        
    def get_stop_loss(self, entry_price, direction):
        """Calculate stop loss price"""
        stop_distance = 0.02  # 2% stop loss
        if direction == "buy":
            return entry_price * (1 - stop_distance)
        else:
            return entry_price * (1 + stop_distance)

# Example strategy implementations go below:

class SmartMoneyStrategy(StrategyTemplate):
    """Detect institutional order flow patterns"""
    
    def __init__(self, parameters=None):
        super().__init__(parameters)
        self.name = "Smart Money Strategy"
        
    def generate_signal(self, market_data):
        # Example: Detect large volume spikes indicating institutional activity
        volume = market_data.get('volume', 0)
        avg_volume = market_data.get('avg_volume_20', volume)
        
        if volume > avg_volume * 2:  # Volume spike
            return {
                "action": "buy", 
                "confidence": 0.8, 
                "reason": "Institutional volume spike detected"
            }
        return {"action": "hold", "confidence": 0.0, "reason": "No institutional activity"}

# Add more strategies here as you discover them in articles!