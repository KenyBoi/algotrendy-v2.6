#!/bin/bash
# Script to add RequireCredentials() to remaining integration test methods

for file in AlgoTrendy.Tests/Integration/Brokers/InteractiveBrokersBrokerIntegrationTests.cs \
            AlgoTrendy.Tests/Integration/Brokers/NinjaTraderBrokerIntegrationTests.cs; do
  if [ -f "$file" ]; then
    # Replace _broker with _broker! (nullable)
    sed -i 's/_broker\./_broker!./g' "$file"
    
    # Add nullable marker to field declaration
    sed -i 's/private readonly \(.*Broker\) _broker;/private readonly \1? _broker;/' "$file"
    
    echo "Fixed $file"
  fi
done
