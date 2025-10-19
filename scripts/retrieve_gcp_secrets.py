#!/usr/bin/env python3
"""
Google Secret Manager - API Credentials Retrieval
Retrieves TradeStation, NinjaTrader, and Interactive Brokers API credentials
"""

import os
import json
from pathlib import Path

def retrieve_secrets_from_gcp():
    """
    Retrieve API credentials from Google Secret Manager

    Prerequisites:
    1. Install google-cloud-secret-manager: pip install google-cloud-secret-manager
    2. Set GOOGLE_APPLICATION_CREDENTIALS environment variable
    3. Service account must have Secret Manager Secret Accessor role
    """

    try:
        from google.cloud import secretmanager
    except ImportError:
        print("❌ google-cloud-secret-manager not installed")
        print("Install with: pip install google-cloud-secret-manager")
        return None

    # Get project ID from environment or prompt
    project_id = os.getenv('GCP_PROJECT_ID')
    if not project_id:
        print("⚠️  GCP_PROJECT_ID environment variable not set")
        project_id = input("Enter your GCP Project ID: ").strip()

    # Check for service account credentials
    creds_path = os.getenv('GOOGLE_APPLICATION_CREDENTIALS')
    if not creds_path or not os.path.exists(creds_path):
        print("⚠️  GOOGLE_APPLICATION_CREDENTIALS not set or file not found")
        print("Please set the path to your service account JSON key file")
        return None

    print(f"✅ Using credentials: {creds_path}")
    print(f"✅ Using project: {project_id}")

    # Create the Secret Manager client
    client = secretmanager.SecretManagerServiceClient()

    # Define secret names to retrieve
    secrets_to_retrieve = {
        'tradestation': [
            'tradestation-api-key',
            'tradestation-api-secret',
            'tradestation-account-id'
        ],
        'ninjatrader': [
            'ninjatrader-username',
            'ninjatrader-password',
            'ninjatrader-account-id',
            'ninjatrader-connection-type'
        ],
        'interactive-brokers': [
            'ibkr-username',
            'ibkr-password',
            'ibkr-account-id',
            'ibkr-gateway-port'
        ]
    }

    retrieved_credentials = {}

    # Retrieve each secret
    for broker, secret_names in secrets_to_retrieve.items():
        print(f"\n🔍 Retrieving {broker} credentials...")
        retrieved_credentials[broker] = {}

        for secret_name in secret_names:
            try:
                # Build the resource name
                name = f"projects/{project_id}/secrets/{secret_name}/versions/latest"

                # Access the secret version
                response = client.access_secret_version(request={"name": name})
                secret_value = response.payload.data.decode('UTF-8')

                # Store in results
                key_name = secret_name.replace(f'{broker.replace("interactive-brokers", "ibkr")}-', '')
                retrieved_credentials[broker][key_name] = secret_value

                print(f"  ✅ Retrieved: {secret_name}")

            except Exception as e:
                print(f"  ⚠️  Could not retrieve {secret_name}: {e}")

    return retrieved_credentials


def save_to_env_file(credentials, env_file_path='/root/AlgoTrendy_v2.6/.env'):
    """Save retrieved credentials to .env file"""

    if not credentials:
        print("❌ No credentials to save")
        return False

    # Read existing .env file
    env_path = Path(env_file_path)
    existing_lines = []

    if env_path.exists():
        with open(env_path, 'r') as f:
            existing_lines = f.readlines()

    # Prepare new credential lines
    new_lines = ["\n# ============================================\n"]
    new_lines.append("# BROKER API CREDENTIALS (from GCP Secret Manager)\n")
    new_lines.append("# ============================================\n\n")

    # TradeStation
    if 'tradestation' in credentials and credentials['tradestation']:
        new_lines.append("# TradeStation\n")
        ts = credentials['tradestation']
        if 'api-key' in ts:
            new_lines.append(f"TRADESTATION_API_KEY={ts['api-key']}\n")
        if 'api-secret' in ts:
            new_lines.append(f"TRADESTATION_API_SECRET={ts['api-secret']}\n")
        if 'account-id' in ts:
            new_lines.append(f"TRADESTATION_ACCOUNT_ID={ts['account-id']}\n")
        new_lines.append("TRADESTATION_USE_PAPER=true\n\n")

    # NinjaTrader
    if 'ninjatrader' in credentials and credentials['ninjatrader']:
        new_lines.append("# NinjaTrader\n")
        nt = credentials['ninjatrader']
        if 'username' in nt:
            new_lines.append(f"NINJATRADER_USERNAME={nt['username']}\n")
        if 'password' in nt:
            new_lines.append(f"NINJATRADER_PASSWORD={nt['password']}\n")
        if 'account-id' in nt:
            new_lines.append(f"NINJATRADER_ACCOUNT_ID={nt['account-id']}\n")
        if 'connection-type' in nt:
            new_lines.append(f"NINJATRADER_CONNECTION_TYPE={nt['connection-type']}\n")
        new_lines.append("\n")

    # Interactive Brokers
    if 'interactive-brokers' in credentials and credentials['interactive-brokers']:
        new_lines.append("# Interactive Brokers\n")
        ib = credentials['interactive-brokers']
        if 'username' in ib:
            new_lines.append(f"IBKR_USERNAME={ib['username']}\n")
        if 'password' in ib:
            new_lines.append(f"IBKR_PASSWORD={ib['password']}\n")
        if 'account-id' in ib:
            new_lines.append(f"IBKR_ACCOUNT_ID={ib['account-id']}\n")
        if 'gateway-port' in ib:
            new_lines.append(f"IBKR_GATEWAY_PORT={ib['gateway-port']}\n")
        else:
            new_lines.append("IBKR_GATEWAY_PORT=4001\n")
        new_lines.append("\n")

    # Append to existing .env file
    with open(env_path, 'a') as f:
        f.writelines(new_lines)

    print(f"\n✅ Credentials saved to {env_path}")
    return True


def main():
    """Main execution"""
    print("=" * 60)
    print("Google Secret Manager - API Credentials Retrieval")
    print("=" * 60)

    # Check prerequisites
    print("\n📋 Checking prerequisites...")

    # Install library if needed
    try:
        import google.cloud.secretmanager
        print("✅ google-cloud-secret-manager installed")
    except ImportError:
        print("⚠️  google-cloud-secret-manager not installed")
        print("\nInstalling...")
        os.system("pip install google-cloud-secret-manager")

    # Retrieve secrets
    credentials = retrieve_secrets_from_gcp()

    if credentials:
        print("\n" + "=" * 60)
        print("📊 Retrieved Credentials Summary")
        print("=" * 60)

        for broker, creds in credentials.items():
            print(f"\n{broker.upper()}:")
            for key in creds.keys():
                print(f"  ✅ {key}")

        # Ask to save
        save = input("\n💾 Save to .env file? (y/n): ").strip().lower()
        if save == 'y':
            save_to_env_file(credentials)
        else:
            print("\n📄 Credentials retrieved but not saved")
            print("You can manually add them to /root/AlgoTrendy_v2.6/.env")
    else:
        print("\n❌ Failed to retrieve credentials")
        print("\nSetup Instructions:")
        print("1. Download your GCP service account JSON key")
        print("2. Set environment variable:")
        print("   export GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json")
        print("3. Set your project ID:")
        print("   export GCP_PROJECT_ID=your-project-id")
        print("4. Run this script again")


if __name__ == "__main__":
    main()
