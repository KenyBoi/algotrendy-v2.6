#!/usr/bin/env python3
"""
Quick Latency Estimate - Calculates theoretical microservices overhead
No services need to be running - this is a theoretical analysis
"""

import statistics

class LatencyCalculator:
    """Calculate theoretical latency differences"""

    # Typical latencies (in milliseconds)
    MONOLITH_LATENCIES = {
        "method_call": 0.001,  # Direct C# method call
        "object_creation": 0.01,  # Create object in memory
        "database_query": 5.0,  # QuestDB query
        "json_serialization": 0.5,  # Serialize object to JSON
        "json_deserialization": 0.5,  # Deserialize JSON to object
    }

    MICROSERVICES_LATENCIES = {
        "method_call": 0.001,  # Direct C# method call
        "object_creation": 0.01,  # Create object in memory
        "database_query": 5.0,  # QuestDB query (same as monolith)
        "json_serialization": 0.5,  # Serialize object to JSON
        "json_deserialization": 0.5,  # Deserialize JSON to object
        "http_request": 1.5,  # HTTP request overhead (localhost)
        "network_latency": 0.1,  # TCP/IP overhead (localhost)
        "gateway_routing": 0.2,  # API gateway routing logic
    }

    def calculate_operation_latency(self, operation: str, architecture: str) -> dict:
        """Calculate latency for common operations"""

        if architecture == "monolith":
            return self._calc_monolith(operation)
        else:
            return self._calc_microservices(operation)

    def _calc_monolith(self, operation: str) -> dict:
        """Calculate monolith latencies"""
        latencies = {
            "place_order": {
                "components": [
                    ("HTTP request handling", 0.3),
                    ("Request deserialization", 0.5),
                    ("Order validation", 0.001),
                    ("Risk check", 0.001),
                    ("Database query (check balance)", 5.0),
                    ("Broker API call", 50.0),  # External API
                    ("Database insert (order)", 5.0),
                    ("Response serialization", 0.5),
                ],
                "description": "Place market order"
            },
            "get_portfolio": {
                "components": [
                    ("HTTP request handling", 0.3),
                    ("Database query (positions)", 5.0),
                    ("Calculate PnL (10 positions)", 0.01),
                    ("Response serialization", 0.5),
                ],
                "description": "Fetch portfolio data"
            },
            "get_market_data": {
                "components": [
                    ("HTTP request handling", 0.3),
                    ("Database query (recent data)", 5.0),
                    ("Data aggregation", 0.1),
                    ("Response serialization", 0.5),
                ],
                "description": "Get market data for symbol"
            },
            "run_backtest": {
                "components": [
                    ("HTTP request handling", 0.3),
                    ("Request deserialization", 0.5),
                    ("Database query (historical data)", 50.0),
                    ("Backtest execution (1000 trades)", 500.0),
                    ("Calculate metrics", 1.0),
                    ("Response serialization", 1.0),
                ],
                "description": "Run strategy backtest"
            },
        }

        operation_data = latencies.get(operation, {})
        components = operation_data.get("components", [])
        total = sum(latency for _, latency in components)

        return {
            "operation": operation,
            "architecture": "monolith",
            "total_ms": total,
            "components": components,
            "description": operation_data.get("description", "")
        }

    def _calc_microservices(self, operation: str) -> dict:
        """Calculate microservices latencies"""
        http = self.MICROSERVICES_LATENCIES["http_request"]
        network = self.MICROSERVICES_LATENCIES["network_latency"]
        gateway = self.MICROSERVICES_LATENCIES["gateway_routing"]

        latencies = {
            "place_order": {
                "components": [
                    ("Client → API Gateway", http + network),
                    ("API Gateway routing", gateway),
                    ("Gateway → Trading Service", http + network),
                    ("Request deserialization", 0.5),
                    ("Order validation", 0.001),
                    ("Risk check", 0.001),
                    ("Database query (check balance)", 5.0),
                    ("Broker API call", 50.0),  # External API (same as monolith)
                    ("Database insert (order)", 5.0),
                    ("Trading Service → Gateway", http + network),
                    ("Response serialization", 0.5),
                    ("Gateway → Client", http + network),
                ],
                "description": "Place market order (Gateway→Trading→Broker)"
            },
            "get_portfolio": {
                "components": [
                    ("Client → API Gateway", http + network),
                    ("API Gateway routing", gateway),
                    ("Gateway → Trading Service", http + network),
                    ("Database query (positions)", 5.0),
                    ("Calculate PnL (10 positions)", 0.01),
                    ("Trading Service → Gateway", http + network),
                    ("Response serialization", 0.5),
                    ("Gateway → Client", http + network),
                ],
                "description": "Fetch portfolio data (Gateway→Trading)"
            },
            "get_market_data": {
                "components": [
                    ("Client → API Gateway", http + network),
                    ("API Gateway routing", gateway),
                    ("Gateway → Data Service", http + network),
                    ("Database query (recent data)", 5.0),
                    ("Data aggregation", 0.1),
                    ("Data Service → Gateway", http + network),
                    ("Response serialization", 0.5),
                    ("Gateway → Client", http + network),
                ],
                "description": "Get market data (Gateway→Data)"
            },
            "run_backtest": {
                "components": [
                    ("Client → API Gateway", http + network),
                    ("API Gateway routing", gateway),
                    ("Gateway → Backtesting Service", http + network),
                    ("Request deserialization", 0.5),
                    ("Database query (historical data)", 50.0),
                    ("Backtest execution (1000 trades)", 500.0),
                    ("Calculate metrics", 1.0),
                    ("Backtesting Service → Gateway", http + network),
                    ("Response serialization", 1.0),
                    ("Gateway → Client", http + network),
                ],
                "description": "Run strategy backtest (Gateway→Backtesting)"
            },
        }

        operation_data = latencies.get(operation, {})
        components = operation_data.get("components", [])
        total = sum(latency for _, latency in components)

        return {
            "operation": operation,
            "architecture": "microservices",
            "total_ms": total,
            "components": components,
            "description": operation_data.get("description", "")
        }

def print_comparison(calculator: LatencyCalculator):
    """Print detailed comparison"""

    operations = ["place_order", "get_portfolio", "get_market_data", "run_backtest"]

    print("="*120)
    print("📊 THEORETICAL LATENCY ANALYSIS - Monolith vs Microservices")
    print("="*120)
    print("\nℹ️  Note: These are theoretical estimates based on typical latencies")
    print("    Actual results may vary based on hardware, network, and implementation\n")

    for operation in operations:
        monolith = calculator.calculate_operation_latency(operation, "monolith")
        microservices = calculator.calculate_operation_latency(operation, "microservices")

        overhead_ms = microservices["total_ms"] - monolith["total_ms"]
        overhead_pct = (overhead_ms / monolith["total_ms"]) * 100 if monolith["total_ms"] > 0 else 0

        print(f"\n{'─'*120}")
        print(f"📝 {operation.replace('_', ' ').title()}: {monolith['description']}")
        print(f"{'─'*120}")

        print(f"\n🏛️  MONOLITH:")
        for component, latency in monolith["components"]:
            print(f"   {component:<50} {latency:>8.2f} ms")
        print(f"   {'─'*60}")
        print(f"   {'TOTAL':<50} {monolith['total_ms']:>8.2f} ms")

        print(f"\n🔬 MICROSERVICES:")
        for component, latency in microservices["components"]:
            print(f"   {component:<50} {latency:>8.2f} ms")
        print(f"   {'─'*60}")
        print(f"   {'TOTAL':<50} {microservices['total_ms']:>8.2f} ms")

        print(f"\n⚡ OVERHEAD:")
        print(f"   Network/HTTP overhead:                              {overhead_ms:>8.2f} ms ({overhead_pct:>5.1f}% increase)")

        # Verdict
        if overhead_pct < 5:
            verdict = "✅ Negligible - microservices overhead is minimal"
        elif overhead_pct < 15:
            verdict = "✅ Low - acceptable for most use cases"
        elif overhead_pct < 30:
            verdict = "⚠️  Moderate - consider if latency is critical"
        else:
            verdict = "❌ High - may impact user experience"

        print(f"   Verdict: {verdict}")

    # Summary table
    print(f"\n\n{'='*120}")
    print("📊 SUMMARY TABLE")
    print(f"{'='*120}")
    print(f"{'Operation':<25} {'Monolith (ms)':<20} {'Microservices (ms)':<20} {'Overhead':<20} {'Verdict':<20}")
    print(f"{'─'*120}")

    total_monolith = 0
    total_micro = 0

    for operation in operations:
        monolith = calculator.calculate_operation_latency(operation, "monolith")
        microservices = calculator.calculate_operation_latency(operation, "microservices")

        total_monolith += monolith["total_ms"]
        total_micro += microservices["total_ms"]

        overhead_ms = microservices["total_ms"] - monolith["total_ms"]
        overhead_pct = (overhead_ms / monolith["total_ms"]) * 100 if monolith["total_ms"] > 0 else 0

        verdict = "✅" if overhead_pct < 15 else "⚠️ " if overhead_pct < 30 else "❌"

        print(f"{operation.replace('_', ' ').title():<25} {monolith['total_ms']:<20.2f} {microservices['total_ms']:<20.2f} +{overhead_ms:.2f} ms ({overhead_pct:.1f}%){'':<5} {verdict}")

    avg_monolith = total_monolith / len(operations)
    avg_micro = total_micro / len(operations)
    avg_overhead = avg_micro - avg_monolith
    avg_overhead_pct = (avg_overhead / avg_monolith) * 100

    print(f"{'─'*120}")
    print(f"{'AVERAGE':<25} {avg_monolith:<20.2f} {avg_micro:<20.2f} +{avg_overhead:.2f} ms ({avg_overhead_pct:.1f}%)")

    print(f"\n{'='*120}")
    print("🎯 KEY INSIGHTS:")
    print(f"{'='*120}")

    print(f"\n1. Network Overhead per Request:")
    print(f"   - Typical HTTP round-trip: ~{2 * (calculator.MICROSERVICES_LATENCIES['http_request'] + calculator.MICROSERVICES_LATENCIES['network_latency']):.1f} ms")
    print(f"   - API Gateway routing: ~{calculator.MICROSERVICES_LATENCIES['gateway_routing']:.1f} ms")
    print(f"   - Total per service hop: ~{2 * (calculator.MICROSERVICES_LATENCIES['http_request'] + calculator.MICROSERVICES_LATENCIES['network_latency']) + calculator.MICROSERVICES_LATENCIES['gateway_routing']:.1f} ms")

    print(f"\n2. When Microservices Make Sense:")
    print(f"   ✅ Operations dominated by I/O (database, external APIs)")
    print(f"   ✅ Long-running operations (backtesting: {microservices['total_ms']:.0f} ms)")
    print(f"   ✅ Independent scaling needs")
    print(f"   ✅ Need for fault isolation")

    print(f"\n3. When Monolith Might Be Better:")
    print(f"   ⚠️  Ultra-low latency requirements (<10ms)")
    print(f"   ⚠️  High-frequency trading (microsecond precision)")
    print(f"   ⚠️  Operations with many service-to-service calls")

    print(f"\n4. Recommendation for AlgoTrendy:")
    if avg_overhead_pct < 15:
        print(f"   ✅ MICROSERVICES VIABLE - {avg_overhead_pct:.1f}% overhead is acceptable")
        print(f"   ✅ Network latency is small compared to I/O operations")
        print(f"   ✅ Benefits (scaling, isolation) outweigh the overhead")
    else:
        print(f"   ⚠️  MONOLITH PREFERRED - {avg_overhead_pct:.1f}% overhead may impact UX")
        print(f"   ⚠️  Consider hybrid: keep latency-critical paths in monolith")

    print(f"\n{'='*120}\n")

if __name__ == "__main__":
    calculator = LatencyCalculator()
    print_comparison(calculator)

    print("💡 Want real measurements? Run:")
    print("   1. Start monolith: docker-compose up")
    print("   2. Run: python3 benchmarks/latency_test.py")
    print("   3. Switch to microservices: docker-compose -f docker-compose.modular.yml up")
    print("   4. Run: python3 benchmarks/latency_test.py")
    print()
