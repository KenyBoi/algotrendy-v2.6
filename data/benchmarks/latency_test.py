#!/usr/bin/env python3
"""
Latency Testing Script for AlgoTrendy
Compares monolith vs microservices performance
"""

import requests
import time
import statistics
import json
from typing import List, Dict, Tuple
from dataclasses import dataclass
from datetime import datetime

@dataclass
class LatencyResult:
    operation: str
    min_ms: float
    max_ms: float
    avg_ms: float
    p50_ms: float
    p95_ms: float
    p99_ms: float
    total_requests: int
    successful_requests: int
    failed_requests: int

class LatencyTester:
    def __init__(self, base_url: str, architecture: str):
        self.base_url = base_url
        self.architecture = architecture
        self.session = requests.Session()

    def measure_latency(self, method: str, endpoint: str, data=None, iterations=100) -> List[float]:
        """Measure latency for multiple requests"""
        latencies = []

        for _ in range(iterations):
            start = time.perf_counter()
            try:
                if method == "GET":
                    response = self.session.get(f"{self.base_url}{endpoint}", timeout=10)
                elif method == "POST":
                    response = self.session.post(f"{self.base_url}{endpoint}", json=data, timeout=10)
                else:
                    raise ValueError(f"Unsupported method: {method}")

                end = time.perf_counter()

                if response.status_code < 400:
                    latencies.append((end - start) * 1000)  # Convert to ms
            except Exception as e:
                # Failed request, don't add to latencies
                pass

        return latencies

    def calculate_statistics(self, latencies: List[float], operation: str) -> LatencyResult:
        """Calculate latency statistics"""
        if not latencies:
            return LatencyResult(
                operation=operation,
                min_ms=0, max_ms=0, avg_ms=0,
                p50_ms=0, p95_ms=0, p99_ms=0,
                total_requests=100,
                successful_requests=0,
                failed_requests=100
            )

        sorted_latencies = sorted(latencies)

        return LatencyResult(
            operation=operation,
            min_ms=min(latencies),
            max_ms=max(latencies),
            avg_ms=statistics.mean(latencies),
            p50_ms=sorted_latencies[len(sorted_latencies) // 2],
            p95_ms=sorted_latencies[int(len(sorted_latencies) * 0.95)],
            p99_ms=sorted_latencies[int(len(sorted_latencies) * 0.99)],
            total_requests=100,
            successful_requests=len(latencies),
            failed_requests=100 - len(latencies)
        )

def test_monolith(iterations=100) -> List[LatencyResult]:
    """Test monolith architecture"""
    tester = LatencyTester("http://localhost:5000", "monolith")
    results = []

    print("🔍 Testing Monolith Architecture...")

    # Test 1: Health check
    latencies = tester.measure_latency("GET", "/health", iterations=iterations)
    results.append(tester.calculate_statistics(latencies, "Health Check"))

    # Test 2: Get portfolio (simulated)
    latencies = tester.measure_latency("GET", "/api/portfolio", iterations=iterations)
    results.append(tester.calculate_statistics(latencies, "Get Portfolio"))

    # Test 3: Place order (simulated)
    order_data = {
        "symbol": "BTCUSDT",
        "side": "buy",
        "type": "market",
        "quantity": 0.01
    }
    latencies = tester.measure_latency("POST", "/api/orders", data=order_data, iterations=iterations)
    results.append(tester.calculate_statistics(latencies, "Place Order"))

    # Test 4: Get market data
    latencies = tester.measure_latency("GET", "/api/marketdata/BTCUSDT", iterations=iterations)
    results.append(tester.calculate_statistics(latencies, "Get Market Data"))

    return results

def test_microservices(iterations=100) -> List[LatencyResult]:
    """Test microservices architecture"""
    api_gateway = LatencyTester("http://localhost:5000", "microservices")
    results = []

    print("🔍 Testing Microservices Architecture...")

    # Test 1: API Gateway health
    latencies = api_gateway.measure_latency("GET", "/health", iterations=iterations)
    results.append(api_gateway.calculate_statistics(latencies, "API Gateway Health"))

    # Test 2: Get portfolio (API Gateway → Trading Service)
    latencies = api_gateway.measure_latency("GET", "/api/portfolio", iterations=iterations)
    results.append(api_gateway.calculate_statistics(latencies, "Get Portfolio (Gateway→Trading)"))

    # Test 3: Place order (API Gateway → Trading Service)
    order_data = {
        "symbol": "BTCUSDT",
        "side": "buy",
        "type": "market",
        "quantity": 0.01
    }
    latencies = api_gateway.measure_latency("POST", "/api/orders", data=order_data, iterations=iterations)
    results.append(api_gateway.calculate_statistics(latencies, "Place Order (Gateway→Trading)"))

    # Test 4: Get market data (API Gateway → Data Service)
    latencies = api_gateway.measure_latency("GET", "/api/marketdata/BTCUSDT", iterations=iterations)
    results.append(api_gateway.calculate_statistics(latencies, "Get Market Data (Gateway→Data)"))

    # Test 5: Direct trading service (bypassing gateway)
    trading_service = LatencyTester("http://localhost:5001", "trading-service")
    latencies = trading_service.measure_latency("GET", "/health", iterations=iterations)
    results.append(trading_service.calculate_statistics(latencies, "Trading Service (Direct)"))

    # Test 6: Direct data service (bypassing gateway)
    data_service = LatencyTester("http://localhost:5002", "data-service")
    latencies = data_service.measure_latency("GET", "/health", iterations=iterations)
    results.append(data_service.calculate_statistics(latencies, "Data Service (Direct)"))

    return results

def print_results(monolith_results: List[LatencyResult], microservices_results: List[LatencyResult]):
    """Print comparison results"""
    print("\n" + "="*100)
    print("📊 LATENCY COMPARISON REPORT - Monolith vs Microservices")
    print("="*100 + "\n")

    print("🏛️  MONOLITH ARCHITECTURE")
    print("-" * 100)
    print(f"{'Operation':<40} {'Min (ms)':<12} {'Avg (ms)':<12} {'P95 (ms)':<12} {'P99 (ms)':<12} {'Success':<10}")
    print("-" * 100)

    for result in monolith_results:
        success_rate = (result.successful_requests / result.total_requests) * 100
        print(f"{result.operation:<40} {result.min_ms:<12.2f} {result.avg_ms:<12.2f} {result.p95_ms:<12.2f} {result.p99_ms:<12.2f} {success_rate:<10.1f}%")

    print("\n🔬 MICROSERVICES ARCHITECTURE")
    print("-" * 100)
    print(f"{'Operation':<40} {'Min (ms)':<12} {'Avg (ms)':<12} {'P95 (ms)':<12} {'P99 (ms)':<12} {'Success':<10}")
    print("-" * 100)

    for result in microservices_results:
        success_rate = (result.successful_requests / result.total_requests) * 100
        print(f"{result.operation:<40} {result.min_ms:<12.2f} {result.avg_ms:<12.2f} {result.p95_ms:<12.2f} {result.p99_ms:<12.2f} {success_rate:<10.1f}%")

    # Calculate overhead
    if monolith_results and microservices_results:
        print("\n⚡ OVERHEAD ANALYSIS")
        print("-" * 100)

        # Compare similar operations
        monolith_avg = statistics.mean([r.avg_ms for r in monolith_results if r.successful_requests > 0])
        micro_avg = statistics.mean([r.avg_ms for r in microservices_results if r.successful_requests > 0])

        overhead_ms = micro_avg - monolith_avg
        overhead_pct = (overhead_ms / monolith_avg) * 100 if monolith_avg > 0 else 0

        print(f"Average Monolith Latency:       {monolith_avg:.2f} ms")
        print(f"Average Microservices Latency:  {micro_avg:.2f} ms")
        print(f"Network Overhead:               {overhead_ms:.2f} ms ({overhead_pct:.1f}% increase)")

        if overhead_pct < 10:
            print("✅ Verdict: Low overhead - microservices are viable")
        elif overhead_pct < 30:
            print("⚠️  Verdict: Moderate overhead - acceptable for most use cases")
        else:
            print("❌ Verdict: High overhead - consider optimizations or stick with monolith")

def save_results_json(monolith_results: List[LatencyResult], microservices_results: List[LatencyResult], filename: str):
    """Save results to JSON file"""
    data = {
        "test_date": datetime.now().isoformat(),
        "monolith": [
            {
                "operation": r.operation,
                "min_ms": r.min_ms,
                "avg_ms": r.avg_ms,
                "p95_ms": r.p95_ms,
                "p99_ms": r.p99_ms,
                "success_rate": (r.successful_requests / r.total_requests) * 100
            }
            for r in monolith_results
        ],
        "microservices": [
            {
                "operation": r.operation,
                "min_ms": r.min_ms,
                "avg_ms": r.avg_ms,
                "p95_ms": r.p95_ms,
                "p99_ms": r.p99_ms,
                "success_rate": (r.successful_requests / r.total_requests) * 100
            }
            for r in microservices_results
        ]
    }

    with open(filename, 'w') as f:
        json.dump(data, f, indent=2)

    print(f"\n💾 Results saved to: {filename}")

if __name__ == "__main__":
    print("🚀 AlgoTrendy Latency Testing Suite")
    print("=" * 100)
    print("\nℹ️  Prerequisites:")
    print("  - Monolith running on http://localhost:5000")
    print("  - Microservices running on http://localhost:5000-5003")
    print("\nℹ️  Each test runs 100 iterations\n")

    input("Press Enter to start testing...")

    # Test monolith
    monolith_results = test_monolith(iterations=100)

    print("\n" + "="*50)
    input("\nSwitch to microservices and press Enter to continue...")

    # Test microservices
    microservices_results = test_microservices(iterations=100)

    # Print comparison
    print_results(monolith_results, microservices_results)

    # Save to file
    save_results_json(monolith_results, microservices_results, "latency_test_results.json")

    print("\n✅ Testing complete!")
