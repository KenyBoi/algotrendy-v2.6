#!/usr/bin/env python3
"""
AlgoTrendy v2.6 - TimescaleDB to QuestDB Migration Script
Migrates market data with batch processing and validation
"""

import os
import sys
import time
import psycopg2
from psycopg2.extras import RealDictCursor
from datetime import datetime
from typing import List, Dict, Tuple
import json

# Color codes for output
RED = '\033[0;31m'
GREEN = '\033[0;32m'
YELLOW = '\033[1;33m'
BLUE = '\033[0;34m'
NC = '\033[0m'

# Database configurations
TIMESCALE_CONFIG = {
    'host': os.getenv('DB_HOST', 'localhost'),
    'port': int(os.getenv('DB_PORT', '5432')),
    'database': os.getenv('DB_NAME', 'algotrendy_v25'),
    'user': os.getenv('DB_USER', 'algotrendy'),
    'password': os.getenv('DB_PASSWORD', 'algotrendy_secure_2025')
}

QUESTDB_CONFIG = {
    'host': 'localhost',
    'port': 8812,
    'database': 'qdb',
    'user': 'admin',
    'password': 'quest'
}

BATCH_SIZE = 1000  # Records per batch


def log(message: str, color: str = NC):
    """Print colored log message with timestamp"""
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    print(f"{color}[{timestamp}] {message}{NC}")


def connect_timescale():
    """Connect to TimescaleDB"""
    try:
        conn = psycopg2.connect(**TIMESCALE_CONFIG)
        log(f"✓ Connected to TimescaleDB at {TIMESCALE_CONFIG['host']}:{TIMESCALE_CONFIG['port']}", GREEN)
        return conn
    except Exception as e:
        log(f"✗ Failed to connect to TimescaleDB: {e}", RED)
        sys.exit(1)


def connect_questdb():
    """Connect to QuestDB"""
    try:
        conn = psycopg2.connect(**QUESTDB_CONFIG)
        log(f"✓ Connected to QuestDB at {QUESTDB_CONFIG['host']}:{QUESTDB_CONFIG['port']}", GREEN)
        return conn
    except Exception as e:
        log(f"✗ Failed to connect to QuestDB: {e}", RED)
        sys.exit(1)


def get_timescale_record_count(conn) -> int:
    """Get total record count from TimescaleDB"""
    with conn.cursor() as cur:
        cur.execute("SELECT COUNT(*) FROM market_data_1m")
        count = cur.fetchone()[0]
        return count


def get_questdb_record_count(conn) -> int:
    """Get total record count from QuestDB"""
    with conn.cursor() as cur:
        cur.execute("SELECT COUNT(*) FROM market_data_1m")
        count = cur.fetchone()[0]
        return count


def migrate_market_data(ts_conn, qdb_conn) -> Tuple[int, int]:
    """
    Migrate market_data_1m table from TimescaleDB to QuestDB

    Returns:
        Tuple of (total_migrated, errors)
    """
    log("Starting market_data_1m migration...", BLUE)

    total_records = get_timescale_record_count(ts_conn)
    log(f"Total records to migrate: {total_records:,}", BLUE)

    migrated_count = 0
    error_count = 0
    batch_num = 0

    with ts_conn.cursor(cursor_factory=RealDictCursor) as ts_cur:
        # Fetch data in batches
        ts_cur.execute("""
            SELECT
                symbol,
                bucket as timestamp,
                open,
                high,
                low,
                close,
                volume,
                quote_volume,
                trades_count,
                source_id
            FROM market_data_1m
            ORDER BY bucket
        """)

        batch = []
        for row in ts_cur:
            # Prepare row for QuestDB
            # Map source_id to source name (1=binance, 2=okx, 3=coinbase, 4=kraken)
            source_map = {1: 'binance', 2: 'okx', 3: 'coinbase', 4: 'kraken'}
            source_name = source_map.get(row.get('source_id', 0), 'unknown')

            batch.append({
                'symbol': row['symbol'],
                'timestamp': row['timestamp'],
                'open': float(row['open']) if row['open'] else None,
                'high': float(row['high']) if row['high'] else None,
                'low': float(row['low']) if row['low'] else None,
                'close': float(row['close']) if row['close'] else None,
                'volume': float(row['volume']) if row['volume'] else None,
                'quote_volume': float(row['quote_volume']) if row['quote_volume'] else None,
                'trades_count': row['trades_count'],
                'source': source_name,
                'metadata_json': None  # Not present in v2.5 schema
            })

            # Insert batch when full
            if len(batch) >= BATCH_SIZE:
                batch_num += 1
                success = insert_batch_to_questdb(qdb_conn, batch, 'market_data_1m')
                if success:
                    migrated_count += len(batch)
                    log(f"Batch {batch_num}: Migrated {migrated_count:,}/{total_records:,} records ({migrated_count/total_records*100:.1f}%)", GREEN)
                else:
                    error_count += len(batch)
                    log(f"Batch {batch_num}: Failed to migrate {len(batch)} records", RED)
                batch = []

        # Insert remaining records
        if batch:
            batch_num += 1
            success = insert_batch_to_questdb(qdb_conn, batch, 'market_data_1m')
            if success:
                migrated_count += len(batch)
                log(f"Batch {batch_num} (final): Migrated {migrated_count:,}/{total_records:,} records (100.0%)", GREEN)
            else:
                error_count += len(batch)

    return migrated_count, error_count


def insert_batch_to_questdb(conn, batch: List[Dict], table_name: str) -> bool:
    """Insert a batch of records into QuestDB"""
    if not batch:
        return True

    try:
        with conn.cursor() as cur:
            # Prepare INSERT statement
            columns = list(batch[0].keys())

            # Build values clause with proper timestamp casting
            values_template = "(" + ",".join([
                "cast(%s as timestamp)" if col == 'timestamp' else "%s"
                for col in columns
            ]) + ")"

            insert_sql = f"INSERT INTO {table_name} ({','.join(columns)}) VALUES {values_template}"

            # Prepare batch data - convert timestamp to string format QuestDB accepts
            batch_data = []
            for row in batch:
                row_data = []
                for col in columns:
                    value = row[col]
                    if col == 'timestamp' and value is not None:
                        # Convert to ISO format string without timezone suffix
                        if hasattr(value, 'replace'):
                            value = value.replace(tzinfo=None).isoformat()
                    row_data.append(value)
                batch_data.append(tuple(row_data))

            # Execute batch insert
            from psycopg2.extras import execute_batch
            execute_batch(cur, insert_sql, batch_data, page_size=BATCH_SIZE)
            conn.commit()

        return True
    except Exception as e:
        log(f"Error inserting batch: {e}", RED)
        conn.rollback()
        return False


def validate_migration(ts_conn, qdb_conn) -> Dict[str, any]:
    """
    Validate the migration with 4-step verification:
    1. Record count match
    2. Per-symbol count validation
    3. Sample record comparison
    4. Query performance benchmark
    """
    log("Starting migration validation...", BLUE)
    validation_results = {}

    # Step 1: Record count validation
    log("Step 1/4: Validating record counts...", YELLOW)
    ts_count = get_timescale_record_count(ts_conn)
    qdb_count = get_questdb_record_count(qdb_conn)

    validation_results['total_count_match'] = (ts_count == qdb_count)
    validation_results['timescale_count'] = ts_count
    validation_results['questdb_count'] = qdb_count

    if ts_count == qdb_count:
        log(f"✓ Record counts match: {ts_count:,} records", GREEN)
    else:
        log(f"✗ Record count mismatch! TimescaleDB: {ts_count:,}, QuestDB: {qdb_count:,}", RED)

    # Step 2: Per-symbol count validation
    log("Step 2/4: Validating per-symbol counts...", YELLOW)
    with ts_conn.cursor(cursor_factory=RealDictCursor) as ts_cur:
        ts_cur.execute("SELECT symbol, COUNT(*) as count FROM market_data_1m GROUP BY symbol ORDER BY symbol")
        ts_symbol_counts = {row['symbol']: row['count'] for row in ts_cur}

    with qdb_conn.cursor(cursor_factory=RealDictCursor) as qdb_cur:
        qdb_cur.execute("SELECT symbol, COUNT(*) as count FROM market_data_1m GROUP BY symbol ORDER BY symbol")
        qdb_symbol_counts = {row['symbol']: row['count'] for row in qdb_cur}

    symbol_mismatches = []
    for symbol in ts_symbol_counts:
        if symbol not in qdb_symbol_counts:
            symbol_mismatches.append(f"{symbol}: missing in QuestDB")
        elif ts_symbol_counts[symbol] != qdb_symbol_counts[symbol]:
            symbol_mismatches.append(f"{symbol}: TS={ts_symbol_counts[symbol]}, QDB={qdb_symbol_counts[symbol]}")

    validation_results['per_symbol_match'] = (len(symbol_mismatches) == 0)
    validation_results['symbol_mismatches'] = symbol_mismatches

    if len(symbol_mismatches) == 0:
        log(f"✓ Per-symbol counts match for {len(ts_symbol_counts)} symbols", GREEN)
    else:
        log(f"✗ Symbol count mismatches found: {len(symbol_mismatches)}", RED)
        for mismatch in symbol_mismatches[:5]:  # Show first 5
            log(f"  - {mismatch}", RED)

    # Step 3: Sample record comparison
    log("Step 3/4: Comparing sample records...", YELLOW)
    sample_size = min(100, ts_count)
    mismatches = 0

    with ts_conn.cursor(cursor_factory=RealDictCursor) as ts_cur:
        ts_cur.execute(f"""
            SELECT * FROM market_data_1m
            ORDER BY RANDOM()
            LIMIT {sample_size}
        """)
        ts_samples = list(ts_cur)

    for ts_row in ts_samples:
        with qdb_conn.cursor(cursor_factory=RealDictCursor) as qdb_cur:
            qdb_cur.execute("""
                SELECT * FROM market_data_1m
                WHERE symbol = %s AND timestamp = %s
                LIMIT 1
            """, (ts_row['symbol'], ts_row['timestamp']))
            qdb_row = qdb_cur.fetchone()

            if not qdb_row:
                mismatches += 1
            elif abs(float(ts_row.get('close', 0) or 0) - float(qdb_row.get('close', 0) or 0)) > 0.0001:
                mismatches += 1

    validation_results['sample_match'] = (mismatches == 0)
    validation_results['sample_size'] = sample_size
    validation_results['sample_mismatches'] = mismatches

    if mismatches == 0:
        log(f"✓ All {sample_size} sample records match", GREEN)
    else:
        log(f"✗ {mismatches}/{sample_size} sample records don't match", RED)

    # Step 4: Query performance benchmark
    log("Step 4/4: Benchmarking query performance...", YELLOW)

    # TimescaleDB query benchmark
    query = "SELECT symbol, AVG(close) as avg_close FROM market_data_1m WHERE timestamp > NOW() - INTERVAL '7 days' GROUP BY symbol"

    start_time = time.time()
    with ts_conn.cursor() as cur:
        cur.execute(query)
        cur.fetchall()
    ts_duration = time.time() - start_time

    # QuestDB query benchmark
    start_time = time.time()
    with qdb_conn.cursor() as cur:
        cur.execute("SELECT symbol, AVG(close) as avg_close FROM market_data_1m WHERE timestamp > dateadd('d', -7, now()) GROUP BY symbol")
        cur.fetchall()
    qdb_duration = time.time() - start_time

    speedup = ts_duration / qdb_duration if qdb_duration > 0 else 0
    validation_results['timescale_query_ms'] = ts_duration * 1000
    validation_results['questdb_query_ms'] = qdb_duration * 1000
    validation_results['performance_improvement'] = speedup

    log(f"TimescaleDB query: {ts_duration*1000:.2f}ms", BLUE)
    log(f"QuestDB query: {qdb_duration*1000:.2f}ms", BLUE)
    log(f"✓ QuestDB is {speedup:.2f}x faster", GREEN)

    return validation_results


def main():
    log("=" * 80, BLUE)
    log("AlgoTrendy v2.6 - TimescaleDB to QuestDB Migration", BLUE)
    log("=" * 80, BLUE)
    log("", NC)

    # Connect to databases
    ts_conn = connect_timescale()
    qdb_conn = connect_questdb()

    try:
        # Perform migration
        log("", NC)
        migrated, errors = migrate_market_data(ts_conn, qdb_conn)

        log("", NC)
        log("=" * 80, BLUE)
        log("Migration Summary", BLUE)
        log("=" * 80, BLUE)
        log(f"Total records migrated: {migrated:,}", GREEN if errors == 0 else YELLOW)
        log(f"Errors: {errors}", RED if errors > 0 else GREEN)

        # Validate migration
        if errors == 0:
            log("", NC)
            validation = validate_migration(ts_conn, qdb_conn)

            log("", NC)
            log("=" * 80, BLUE)
            log("Validation Results", BLUE)
            log("=" * 80, BLUE)

            all_passed = (
                validation['total_count_match'] and
                validation['per_symbol_match'] and
                validation['sample_match']
            )

            if all_passed:
                log("✓ ALL VALIDATION CHECKS PASSED!", GREEN)
                log(f"✓ QuestDB is {validation['performance_improvement']:.2f}x faster than TimescaleDB", GREEN)
                log("", NC)
                log("Migration completed successfully!", GREEN)
                log("You can now update data channels to write to QuestDB", YELLOW)
            else:
                log("✗ Some validation checks failed", RED)
                log("Review the errors above before proceeding", RED)
                sys.exit(1)
        else:
            log("Migration completed with errors. Review logs above.", YELLOW)
            sys.exit(1)

    finally:
        ts_conn.close()
        qdb_conn.close()
        log("Database connections closed", BLUE)


if __name__ == "__main__":
    main()
