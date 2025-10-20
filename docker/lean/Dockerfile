# QuantConnect LEAN Local Backtesting Engine
# Based on official QuantConnect LEAN engine
FROM quantconnect/lean:latest

# Set working directory
WORKDIR /Lean/Launcher/bin/Debug

# Create directories for algorithms and results
RUN mkdir -p /algorithms /results /data

# Copy configuration
COPY config.json /Lean/Launcher/bin/Debug/config.json

# Set permissions
RUN chmod -R 777 /algorithms /results /data

# Expose ports (optional, for monitoring)
EXPOSE 5678

# Default command - run LEAN engine
# This will be overridden with specific algorithm paths
ENTRYPOINT ["dotnet", "QuantConnect.Lean.Launcher.dll"]
CMD ["--data-folder", "/data", "--results-destination-folder", "/results"]
