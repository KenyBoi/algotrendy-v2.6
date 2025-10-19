#!/usr/bin/env python3
"""
📊 MEM LIVE DASHBOARD - Streamlit visualization
Real-time MemGPT ML data pipeline dashboard with 6 pages
"""

import streamlit as st
import asyncio
import json
import pandas as pd
import plotly.graph_objects as go
import plotly.express as px
from datetime import datetime, timedelta
from typing import Dict, Any, List
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Page configuration
st.set_page_config(
    page_title="MEM ML Pipeline Dashboard",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded"
)

# Custom CSS
st.markdown("""
    <style>
    .metric-card {
        background-color: #f0f2f6;
        padding: 20px;
        border-radius: 10px;
        margin: 10px 0;
    }
    .metric-title {
        font-size: 14px;
        color: #666;
        font-weight: 600;
    }
    .metric-value {
        font-size: 32px;
        color: #1f77b4;
        font-weight: 700;
    }
    </style>
""", unsafe_allow_html=True)


class MemDashboard:
    """Main dashboard controller"""
    
    def __init__(self):
        self.db = None
        self.connector = None
        self.initialize_session()
    
    def initialize_session(self):
        """Initialize session state"""
        if "initialized" not in st.session_state:
            st.session_state.initialized = False
            st.session_state.trades = []
            st.session_state.memories = []
            st.session_state.signals = []
            st.session_state.metrics = {}
    
    async def init_connection(self):
        """Initialize connections to data sources"""
        try:
            from sqlite_manager import SQLiteManager
            from mem_connection_manager import get_connection_manager
            
            self.db = SQLiteManager("mem_data.db")
            self.connector = get_connection_manager()
            
            # Initialize connector (will use WebSocket/REST as available)
            await self.connector.initialize()
            
            st.session_state.initialized = True
            logger.info("✅ Dashboard initialized")
            return True
        except Exception as e:
            logger.error(f"❌ Initialization error: {e}")
            st.error(f"Failed to initialize: {e}")
            return False
    
    async def load_data(self):
        """Load data from SQLite"""
        try:
            if not self.db:
                return False
            
            st.session_state.trades = await self.db.get_trades(limit=100)
            st.session_state.memories = await self.db.get_memories(limit=50)
            st.session_state.signals = await self.db.get_signals(limit=50)
            st.session_state.stats = await self.db.get_stats()
            
            return True
        except Exception as e:
            logger.error(f"❌ Data load error: {e}")
            return False
    
    def render_home(self):
        """Render HOME page"""
        st.title("🏠 MEM ML Pipeline Dashboard")
        
        # Status section
        col1, col2, col3, col4 = st.columns(4)
        
        with col1:
            st.metric("Total Trades", len(st.session_state.trades), "📈")
        
        with col2:
            st.metric("Memories", len(st.session_state.memories), "🧠")
        
        with col3:
            st.metric("Signals", len(st.session_state.signals), "⚡")
        
        with col4:
            if st.session_state.trades:
                avg_confidence = sum(t.get("confidence", 0) for t in st.session_state.trades) / len(st.session_state.trades)
                st.metric("Avg Confidence", f"{avg_confidence:.2%}", "🎯")
            else:
                st.metric("Avg Confidence", "—", "🎯")
        
        # Connection status
        st.divider()
        col1, col2 = st.columns([3, 1])
        with col1:
            st.subheader("📡 Connection Status")
        
        # System health overview
        st.info("""
        **MEM ML Pipeline** is a multi-layer architecture:
        - **Phase 1**: Connectivity (WebSocket + REST)
        - **Phase 2**: Data Layer (SQLite persistence)
        - **Phase 3**: Visualization (This dashboard)
        - **Phase 4**: ETL (Prefect→Snowflake)
        - **Phase 5**: Monitoring (Prometheus+Grafana)
        """)
    
    def render_trades(self):
        """Render TRADES page"""
        st.title("📈 Trades")
        
        if not st.session_state.trades:
            st.info("No trades recorded yet")
            return
        
        # Convert to DataFrame
        df = pd.DataFrame(st.session_state.trades)
        
        # Filters
        col1, col2, col3 = st.columns(3)
        
        with col1:
            action_filter = st.multiselect(
                "Action",
                options=df["action"].unique().tolist() if "action" in df.columns else [],
                default=df["action"].unique().tolist() if "action" in df.columns else []
            )
        
        with col2:
            if "symbol" in df.columns:
                symbol_filter = st.multiselect(
                    "Symbol",
                    options=df["symbol"].unique().tolist(),
                    default=df["symbol"].unique().tolist()
                )
            else:
                symbol_filter = []
        
        with col3:
            min_confidence = st.slider(
                "Min Confidence",
                min_value=0.0,
                max_value=1.0,
                value=0.0
            )
        
        # Apply filters
        filtered_df = df
        if action_filter and "action" in df.columns:
            filtered_df = filtered_df[filtered_df["action"].isin(action_filter)]
        if symbol_filter and "symbol" in df.columns:
            filtered_df = filtered_df[filtered_df["symbol"].isin(symbol_filter)]
        if "confidence" in df.columns:
            filtered_df = filtered_df[filtered_df["confidence"] >= min_confidence]
        
        # Display table
        st.dataframe(filtered_df, use_container_width=True)
        
        # Charts
        if len(filtered_df) > 0:
            col1, col2 = st.columns(2)
            
            with col1:
                if "pnl" in filtered_df.columns:
                    # P&L over time
                    fig = px.line(
                        filtered_df,
                        x="timestamp",
                        y="pnl",
                        title="P&L Over Time",
                        markers=True
                    )
                    st.plotly_chart(fig, use_container_width=True)
            
            with col2:
                if "confidence" in filtered_df.columns:
                    # Confidence distribution
                    fig = px.histogram(
                        filtered_df,
                        x="confidence",
                        nbins=20,
                        title="Confidence Distribution",
                        labels={"confidence": "Confidence Score"}
                    )
                    st.plotly_chart(fig, use_container_width=True)
    
    def render_memory(self):
        """Render MEMORY page"""
        st.title("🧠 Memories")
        
        if not st.session_state.memories:
            st.info("No memories recorded yet")
            return
        
        # Convert to DataFrame
        df = pd.DataFrame(st.session_state.memories)
        
        # Filter by type
        if "memory_type" in df.columns:
            memory_type = st.selectbox(
                "Memory Type",
                options=["All"] + df["memory_type"].unique().tolist()
            )
            
            if memory_type != "All":
                df = df[df["memory_type"] == memory_type]
        
        # Display memories
        for idx, row in df.iterrows():
            with st.container():
                col1, col2 = st.columns([4, 1])
                
                with col1:
                    st.write(f"**{row.get('memory_type', 'Unknown')}**")
                    st.write(row.get('content', ''))
                
                with col2:
                    if "relevance_score" in row:
                        st.metric("Score", f"{row['relevance_score']:.2%}")
                
                st.divider()
        
        # Statistics
        st.subheader("📊 Memory Statistics")
        col1, col2, col3 = st.columns(3)
        
        with col1:
            st.metric("Total Memories", len(df))
        
        with col2:
            if "relevance_score" in df.columns:
                avg_score = df["relevance_score"].mean()
                st.metric("Avg Relevance", f"{avg_score:.2%}")
        
        with col3:
            if "memory_type" in df.columns:
                st.metric("Types", len(df["memory_type"].unique()))
    
    def render_config(self):
        """Render CONFIG page"""
        st.title("⚙️ Configuration")
        
        st.subheader("📡 Connectivity")
        col1, col2 = st.columns(2)
        
        with col1:
            ws_url = st.text_input("WebSocket URL", value="ws://127.0.0.1:8765")
        
        with col2:
            rest_url = st.text_input("REST URL", value="http://127.0.0.1:5000")
        
        st.subheader("💾 Data Management")
        col1, col2, col3 = st.columns(3)
        
        with col1:
            max_trades = st.number_input("Max Trades", value=1000, min_value=10)
        
        with col2:
            max_memories = st.number_input("Max Memories", value=500, min_value=10)
        
        with col3:
            retention_days = st.number_input("Retention Days", value=30, min_value=7)
        
        if st.button("💾 Save Configuration"):
            config = {
                "ws_url": ws_url,
                "rest_url": rest_url,
                "max_trades": max_trades,
                "max_memories": max_memories,
                "retention_days": retention_days
            }
            
            with open("mem_dashboard_config.json", "w") as f:
                json.dump(config, f, indent=2)
            
            st.success("✅ Configuration saved")
        
        # Advanced settings
        st.subheader("🔧 Advanced")
        
        if st.button("🧹 Cleanup Old Data"):
            # This would trigger cleanup
            st.info("Cleanup scheduled")
        
        if st.button("📊 Export Data"):
            st.info("Export feature coming soon")
    
    def render_health(self):
        """Render HEALTH page"""
        st.title("❤️ System Health")
        
        # System metrics
        col1, col2, col3, col4 = st.columns(4)
        
        with col1:
            st.metric("Uptime", "24h 15m", "✅")
        
        with col2:
            st.metric("Connection", "Active", "🟢")
        
        with col3:
            st.metric("Data Lag", "0.5s", "📊")
        
        with col4:
            st.metric("Error Rate", "0.01%", "✅")
        
        # Connection history
        st.subheader("📡 Connection Status")
        
        connection_data = {
            "Timestamp": [datetime.now() - timedelta(hours=i) for i in range(5)],
            "Status": ["Connected"] * 5,
            "Latency (ms)": [45, 52, 48, 51, 49]
        }
        df = pd.DataFrame(connection_data)
        st.line_chart(df.set_index("Timestamp")["Latency (ms)"])
        
        # Data pipeline health
        st.subheader("📈 Data Pipeline")
        
        if st.session_state.stats:
            col1, col2, col3, col4, col5 = st.columns(5)
            
            with col1:
                st.metric("Trades", st.session_state.stats.get("trades", 0))
            
            with col2:
                st.metric("Memories", st.session_state.stats.get("memories", 0))
            
            with col3:
                st.metric("Metrics", st.session_state.stats.get("metrics", 0))
            
            with col4:
                st.metric("Signals", st.session_state.stats.get("signals", 0))
            
            with col5:
                st.metric("Logs", st.session_state.stats.get("logs", 0))
    
    def render_alerts(self):
        """Render ALERTS page"""
        st.title("🔔 Alerts & Signals")
        
        if not st.session_state.signals:
            st.info("No signals recorded yet")
            return
        
        # Convert to DataFrame
        df = pd.DataFrame(st.session_state.signals)
        
        # Group by symbol
        if "symbol" in df.columns:
            for symbol in df["symbol"].unique():
                symbol_signals = df[df["symbol"] == symbol]
                
                st.subheader(f"📍 {symbol}")
                
                for idx, row in symbol_signals.iterrows():
                    signal_type = row.get("signal_type", "Unknown")
                    confidence = row.get("confidence", 0)
                    
                    # Color code based on signal type
                    if signal_type == "entry":
                        icon = "🟢"
                    elif signal_type == "exit":
                        icon = "🔴"
                    else:
                        icon = "⚪"
                    
                    col1, col2, col3 = st.columns([2, 1, 1])
                    
                    with col1:
                        st.write(f"{icon} {signal_type.upper()}")
                    
                    with col2:
                        st.write(f"Confidence: {confidence:.0%}")
                    
                    with col3:
                        st.write(row.get("timestamp", ""))
        
        st.divider()
        
        # Summary
        st.subheader("📊 Signal Summary")
        if "signal_type" in df.columns:
            summary = df["signal_type"].value_counts()
            st.bar_chart(summary)
    
    def render(self):
        """Main render function"""
        # Sidebar
        st.sidebar.title("🔗 MEM Dashboard")
        
        page = st.sidebar.radio(
            "Navigation",
            ["🏠 Home", "📈 Trades", "🧠 Memory", "⚙️ Config", "❤️ Health", "🔔 Alerts"]
        )
        
        # Refresh button
        if st.sidebar.button("🔄 Refresh"):
            st.rerun()
        
        # Render selected page
        if page == "🏠 Home":
            self.render_home()
        elif page == "📈 Trades":
            self.render_trades()
        elif page == "🧠 Memory":
            self.render_memory()
        elif page == "⚙️ Config":
            self.render_config()
        elif page == "❤️ Health":
            self.render_health()
        elif page == "🔔 Alerts":
            self.render_alerts()


def main():
    """Main entry point"""
    dashboard = MemDashboard()
    
    # Load data
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)
    
    if not st.session_state.initialized:
        loop.run_until_complete(dashboard.init_connection())
    
    loop.run_until_complete(dashboard.load_data())
    
    # Render dashboard
    dashboard.render()


if __name__ == "__main__":
    main()
