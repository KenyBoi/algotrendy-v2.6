#!/usr/bin/env python3
"""
Broker API Latency Test - Measure actual latencies from current VPS to broker APIs
Run from CDMX VPS to determine if geographic distribution would help
"""

import requests
import time
import statistics
import json
from typing import Dict, List
from dataclasses import dataclass
from datetime import datetime

@dataclass
class BrokerLatencyResult:
    broker: str
    endpoint: str
    datacenter_location: str
    min_ms: float
    avg_ms: float
    p50_ms: float
    p95_ms: float
    p99_ms: float
    max_ms: float
    success_rate: float
    recommended_vps: str

class BrokerLatencyTester:
    """Test latency to various broker APIs"""

    # Broker API endpoints (public endpoints for latency testing)
    BROKER_ENDPOINTS = {
        "binance": {
            "url": "https://api.binance.com/api/v3/ping",
            "location": "Tokyo/Singapore (primary), Also: US East",
            "recommended_vps": "New Jersey (US East datacenter)"
        },
        "binance_us": {
            "url": "https://api.binance.us/api/v3/ping",
            "location": "US East (AWS)",
            "recommended_vps": "New Jersey"
        },
        "coinbase": {
            "url": "https://api.coinbase.com/v2/time",
            "location": "US East (AWS Virginia)",
            "recommended_vps": "New Jersey"
        },
        "bybit": {
            "url": "https://api.bybit.com/v5/market/time",
            "location": "Hong Kong (primary), Also: US West",
            "recommended_vps": "Chicago or New Jersey (if US West available)"
        },
        "alpaca": {
            "url": "https://api.alpaca.markets/v2/clock",
            "location": "US East (near NYSE)",
            "recommended_vps": "New Jersey (ideal - same region as NYSE)"
        },
        "alpaca_data": {
            "url": "https://data.alpaca.markets/v2/stocks/bars/latest?symbols=AAPL",
            "location": "US East",
            "recommended_vps": "New Jersey"
        },
        "polygon": {
            "url": "https://api.polygon.io/v1/marketstatus/now",
            "location": "US East",
            "recommended_vps": "New Jersey"
        },
        "twelve_data": {
            "url": "https://api.twelvedata.com/time_series?symbol=AAPL&interval=1min&outputsize=1",
            "location": "Multiple (CDN)",
            "recommended_vps": "CDMX (CDN should be fast everywhere)"
        },
        "tiingo": {
            "url": "https://api.tiingo.com/api/test",
            "location": "US East",
            "recommended_vps": "New Jersey"
        },
        "coingecko": {
            "url": "https://api.coingecko.com/api/v3/ping",
            "location": "Singapore/Asia",
            "recommended_vps": "CDMX (acceptable) or Asia VPS if available"
        },
        "eodhd": {
            "url": "https://eodhd.com/api/exchanges-list/?api_token=demo&fmt=json",
            "location": "US East",
            "recommended_vps": "New Jersey"
        }
    }

    def measure_latency(self, broker: str, url: str, iterations: int = 50) -> List[float]:
        """Measure latency to a broker API endpoint"""
        latencies = []

        for i in range(iterations):
            try:
                start = time.perf_counter()
                response = requests.get(url, timeout=5)
                end = time.perf_counter()

                if response.status_code < 500:  # Accept 200, 401, 403 (auth required)
                    latency_ms = (end - start) * 1000
                    latencies.append(latency_ms)

                # Add small delay between requests to be respectful
                time.sleep(0.1)

            except requests.exceptions.Timeout:
                print(f"   ⏱️  Timeout on request {i+1}/{iterations} to {broker}")
            except Exception as e:
                print(f"   ❌ Error on request {i+1}/{iterations} to {broker}: {str(e)[:50]}")

        return latencies

    def calculate_statistics(self, broker: str, latencies: List[float],
                           location: str, recommended_vps: str) -> BrokerLatencyResult:
        """Calculate statistics from latency measurements"""

        if not latencies:
            return BrokerLatencyResult(
                broker=broker,
                endpoint=self.BROKER_ENDPOINTS[broker]["url"],
                datacenter_location=location,
                min_ms=0, avg_ms=0, p50_ms=0, p95_ms=0, p99_ms=0, max_ms=0,
                success_rate=0.0,
                recommended_vps=recommended_vps
            )

        sorted_latencies = sorted(latencies)

        return BrokerLatencyResult(
            broker=broker,
            endpoint=self.BROKER_ENDPOINTS[broker]["url"],
            datacenter_location=location,
            min_ms=min(latencies),
            avg_ms=statistics.mean(latencies),
            p50_ms=sorted_latencies[len(sorted_latencies) // 2],
            p95_ms=sorted_latencies[int(len(sorted_latencies) * 0.95)] if len(sorted_latencies) > 20 else max(latencies),
            p99_ms=sorted_latencies[int(len(sorted_latencies) * 0.99)] if len(sorted_latencies) > 20 else max(latencies),
            max_ms=max(latencies),
            success_rate=(len(latencies) / 50) * 100,
            recommended_vps=recommended_vps
        )

def print_results(results: List[BrokerLatencyResult]):
    """Print formatted results"""

    print("\n" + "="*120)
    print("🌍 BROKER API LATENCY TEST - From Current VPS (CDMX)")
    print("="*120 + "\n")

    print("ℹ️  This test measures actual latency to broker APIs to determine if geographic distribution would help\n")

    # Group by recommended VPS
    by_location = {}
    for result in results:
        if result.recommended_vps not in by_location:
            by_location[result.recommended_vps] = []
        by_location[result.recommended_vps].append(result)

    # Print results grouped by recommended location
    for location, location_results in sorted(by_location.items()):
        print(f"\n{'='*120}")
        print(f"📍 RECOMMENDED VPS: {location}")
        print(f"{'='*120}")
        print(f"{'Broker':<20} {'Datacenter':<30} {'Avg (ms)':<12} {'P95 (ms)':<12} {'P99 (ms)':<12} {'Success':<10}")
        print("-" * 120)

        for result in location_results:
            if result.success_rate > 0:
                print(f"{result.broker:<20} {result.datacenter_location:<30} "
                      f"{result.avg_ms:<12.1f} {result.p95_ms:<12.1f} {result.p99_ms:<12.1f} "
                      f"{result.success_rate:<10.0f}%")
            else:
                print(f"{result.broker:<20} {result.datacenter_location:<30} {'FAILED':<12} {'FAILED':<12} {'FAILED':<12} "
                      f"{result.success_rate:<10.0f}%")

    print("\n" + "="*120)
    print("📊 ANALYSIS & RECOMMENDATIONS")
    print("="*120 + "\n")

    # Calculate average latency by recommended VPS
    vps_averages = {}
    for location, location_results in by_location.items():
        successful_results = [r for r in location_results if r.success_rate > 0]
        if successful_results:
            avg = statistics.mean([r.avg_ms for r in successful_results])
            vps_averages[location] = avg

    print("Average Latency by Recommended VPS:")
    for location, avg_latency in sorted(vps_averages.items(), key=lambda x: x[1]):
        print(f"  • {location:<30} {avg_latency:>8.1f} ms")

    # Identify high-latency brokers (candidates for geographic optimization)
    print("\n🎯 High Latency Brokers (Good candidates for geographic optimization):")
    high_latency = [r for r in results if r.avg_ms > 100 and r.success_rate > 0]

    if high_latency:
        for result in sorted(high_latency, key=lambda x: x.avg_ms, reverse=True):
            potential_improvement = result.avg_ms - 20  # Assume 20ms if moved to optimal location
            print(f"  • {result.broker:<20} Current: {result.avg_ms:>6.1f}ms  →  "
                  f"Potential: ~20-40ms  (Save ~{potential_improvement:.0f}ms)")
            print(f"    Move to: {result.recommended_vps}")
    else:
        print("  ✅ All brokers have acceptable latency (<100ms) from CDMX")

    # Low-latency brokers (already good from CDMX)
    print("\n✅ Low Latency Brokers (Already good from CDMX, no need to move):")
    low_latency = [r for r in results if r.avg_ms <= 100 and r.success_rate > 0]

    if low_latency:
        for result in sorted(low_latency, key=lambda x: x.avg_ms):
            print(f"  • {result.broker:<20} {result.avg_ms:>6.1f}ms  (Keep in CDMX)")

    print("\n" + "="*120)
    print("💡 DEPLOYMENT RECOMMENDATION")
    print("="*120 + "\n")

    # Calculate potential improvement
    avg_current = statistics.mean([r.avg_ms for r in results if r.success_rate > 0])

    if avg_current > 100:
        print("✅ GEOGRAPHIC DISTRIBUTION RECOMMENDED")
        print(f"   Current average latency: {avg_current:.1f}ms")
        print(f"   Potential improvement: 50-70% latency reduction for high-latency brokers")
        print("\n   Suggested deployment:")
        print("   • CDMX:       ML, Backtesting, Low-latency brokers")
        print("   • New Jersey: Alpaca, Binance.US, Coinbase, Polygon, Tiingo, EODHD")
        print("   • Chicago:    Futures brokers (if added)")

    elif avg_current > 50:
        print("⚠️  GEOGRAPHIC DISTRIBUTION OPTIONAL")
        print(f"   Current average latency: {avg_current:.1f}ms (acceptable for most use cases)")
        print(f"   Improvement: 20-30% latency reduction possible")
        print("\n   Consider if:")
        print("   • High-frequency trading is a priority")
        print("   • Every millisecond matters for your strategy")
        print("   • You have specific brokers with >100ms latency")

    else:
        print("✅ KEEP EVERYTHING IN CDMX")
        print(f"   Current average latency: {avg_current:.1f}ms (excellent!)")
        print("   Geographic distribution not needed - latencies are already very good")
        print("\n   Your CDMX VPS has great connectivity to broker APIs!")

    print("\n" + "="*120 + "\n")

def save_results_json(results: List[BrokerLatencyResult], filename: str):
    """Save results to JSON file"""

    data = {
        "test_date": datetime.now().isoformat(),
        "test_location": "CDMX VPS",
        "brokers": [
            {
                "broker": r.broker,
                "endpoint": r.endpoint,
                "datacenter_location": r.datacenter_location,
                "min_ms": r.min_ms,
                "avg_ms": r.avg_ms,
                "p50_ms": r.p50_ms,
                "p95_ms": r.p95_ms,
                "p99_ms": r.p99_ms,
                "max_ms": r.max_ms,
                "success_rate": r.success_rate,
                "recommended_vps": r.recommended_vps
            }
            for r in results
        ]
    }

    with open(filename, 'w') as f:
        json.dump(data, f, indent=2)

    print(f"💾 Results saved to: {filename}")

def main():
    print("🚀 AlgoTrendy Broker API Latency Test")
    print("="*120)
    print(f"\n📍 Testing from: CDMX VPS")
    print(f"📊 Iterations per broker: 50 requests")
    print(f"⏱️  Estimated time: ~2-3 minutes\n")

    tester = BrokerLatencyTester()
    results = []

    total_brokers = len(tester.BROKER_ENDPOINTS)

    for i, (broker, config) in enumerate(tester.BROKER_ENDPOINTS.items(), 1):
        print(f"[{i}/{total_brokers}] Testing {broker}... ", end='', flush=True)

        latencies = tester.measure_latency(broker, config["url"], iterations=50)
        result = tester.calculate_statistics(
            broker,
            latencies,
            config["location"],
            config["recommended_vps"]
        )
        results.append(result)

        if result.success_rate > 0:
            print(f"✅ Avg: {result.avg_ms:.1f}ms, P95: {result.p95_ms:.1f}ms")
        else:
            print(f"❌ Failed (API might require authentication)")

    # Print comprehensive results
    print_results(results)

    # Save to JSON
    save_results_json(results, "broker_api_latency_results.json")

    print("\n📖 Next Steps:")
    print("  1. Review the results above")
    print("  2. Check broker_api_latency_results.json for detailed data")
    print("  3. Consult .dev/planning/MULTI_REGION_DEPLOYMENT_STRATEGY.md for deployment options")
    print("  4. Decide if geographic distribution is worth the operational complexity\n")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⏸️  Test interrupted by user")
    except Exception as e:
        print(f"\n\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
