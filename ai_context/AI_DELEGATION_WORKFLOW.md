# AI Delegation Workflow for TradingView Integration
## AlgoTrendy v2.6 - Rapid Deployment Guide

**Version:** 1.0
**Date:** 2025-10-19
**Purpose:** Instructions for delegating TradingView integration tasks to AI agents for parallel execution

---

## 🎯 Overview

This document provides ready-to-use prompts and workflows for delegating each phase of the TradingView integration to AI agents (Claude Code, GPT-4, or similar). Each section contains:

1. **Agent Configuration** - How to set up the AI agent
2. **Task Prompt** - Exact prompt to give the AI
3. **Success Validation** - How to verify completion
4. **Handoff Instructions** - How to pass results to next phase

---

## 🤖 Agent Configuration

### Prerequisites for All Agents

**Required Access:**
- Read/write access to `/root/AlgoTrendy_v2.6/`
- Read-only access to `/root/algotrendy_v2.5/` (reference)
- Ability to execute bash commands
- Ability to install Python packages
- Network access (for package installation)

**Initial Context to Provide:**
```
You are working on AlgoTrendy v2.6, a comprehensive algorithmic trading platform.
Your task is to help integrate TradingView webhook functionality.

Key directories:
- Project root: /root/AlgoTrendy_v2.6/
- TradingView integration: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Backend: /root/AlgoTrendy_v2.6/backend/
- Reference (v2.5): /root/algotrendy_v2.5/ (READ ONLY)

Master plan: /root/AlgoTrendy_v2.6/TRADINGVIEW_BUILD_INTEGRATION_TEST_PLAN.md

Work autonomously. Create files, run tests, and report findings.
Do NOT delete any files.
```

---

## 📋 Phase-by-Phase Delegation

### PHASE 1: Environment Setup

**Agent Name:** Environment-Builder
**Priority:** P0 (Critical)
**Dependencies:** None
**Estimated Time:** 30 minutes

#### Prompt for AI Agent:

```
TASK: Set up Python environment for TradingView integration

CONTEXT:
- Working directory: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Requirements file: requirements.txt (already exists)
- Backend requirements: /root/AlgoTrendy_v2.6/backend/requirements.txt

STEPS:
1. Create a Python virtual environment named 'tradingview_venv'
2. Activate the virtual environment
3. Install all packages from integrations/tradingview/requirements.txt
4. Install all packages from backend/requirements.txt
5. Verify Python version is 3.10 or higher
6. Check for any dependency conflicts
7. Create a requirements_frozen.txt with exact versions

SUCCESS CRITERIA:
- Virtual environment created successfully
- All packages installed without errors
- No version conflicts
- Python >= 3.10

DELIVERABLE:
Create a file: /root/AlgoTrendy_v2.6/reports/phase1_environment_setup_report.md

Include:
- Python version used
- List of installed packages with versions
- Any warnings or conflicts encountered
- Commands to activate environment
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
source /root/AlgoTrendy_v2.6/integrations/tradingview/tradingview_venv/bin/activate
python --version
pip list
cat /root/AlgoTrendy_v2.6/reports/phase1_environment_setup_report.md
```

---

### PHASE 2: Dependency Analysis

**Agent Name:** Dependency-Analyzer
**Priority:** P0 (Critical)
**Dependencies:** Phase 1
**Estimated Time:** 1 hour

#### Prompt for AI Agent:

```
TASK: Analyze all dependencies and imports for TradingView integration

CONTEXT:
- TradingView integration files: /root/AlgoTrendy_v2.6/integrations/tradingview/*.py
- Backend source: /root/AlgoTrendy_v2.6/backend/
- Need to understand all imports and dependencies

STEPS:
1. Scan all Python files in integrations/tradingview/
2. Extract all import statements (import X and from Y import Z)
3. Categorize imports:
   - Standard library
   - Third-party packages
   - Internal AlgoTrendy modules
4. For internal imports, find the source files
5. Create a dependency graph showing module relationships
6. Identify any circular dependencies
7. Check for missing dependencies

TOOLS TO USE:
- grep for finding imports
- modulefinder or similar static analysis
- Read backend source code to understand internal APIs

SUCCESS CRITERIA:
- All imports catalogued
- Dependency graph created
- No circular dependencies
- All internal modules identified

DELIVERABLE:
Create TWO files:
1. /root/AlgoTrendy_v2.6/reports/phase2_dependency_report.md
   - Summary of findings
   - List of all dependencies
   - Any issues found

2. /root/AlgoTrendy_v2.6/reports/dependency_map.json
   - Machine-readable dependency graph
   - Format:
     {
       "file": "tradingview_data_publisher.py",
       "imports": {
         "standard": ["json", "logging"],
         "third_party": ["flask", "redis"],
         "internal": ["backend.models.signal"]
       }
     }
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase2_dependency_report.md
python -c "import json; print(json.load(open('/root/AlgoTrendy_v2.6/reports/dependency_map.json')))"
```

---

### PHASE 3: Configuration Validation

**Agent Name:** Config-Validator
**Priority:** P1
**Dependencies:** Phase 1
**Estimated Time:** 45 minutes
**Can Run in Parallel With:** Phase 2

#### Prompt for AI Agent:

```
TASK: Validate and document all configuration files for TradingView integration

CONTEXT:
- Config file: /root/AlgoTrendy_v2.6/integrations/tradingview/project_config.json
- Environment file: /root/AlgoTrendy_v2.6/.env
- Webhook template: /root/AlgoTrendy_v2.6/integrations/tradingview/templates/webhook_template.json

STEPS:
1. Read and parse project_config.json
2. Validate JSON syntax
3. Check for required fields:
   - webhook_port
   - api_endpoints
   - broker_configs
   - authentication settings
4. Look for placeholder values (e.g., "YOUR_API_KEY", "REPLACE_ME")
5. Check port numbers:
   - Ensure ports are in valid range (1024-65535)
   - Check if ports are already in use: netstat -tuln
6. Read .env file and check for TradingView-related settings
7. Validate webhook_template.json structure
8. Document all configuration options

SUCCESS CRITERIA:
- All JSON files parse correctly
- No syntax errors
- Ports are available
- Required fields present
- No obvious placeholder values in production configs

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase3_config_validation_report.md

Include:
- Configuration file inventory
- Validation results for each file
- List of placeholder values that need updating
- Port allocation table
- Recommended configuration changes
- Status: PASS/FAIL/WARNINGS
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase3_config_validation_report.md
python -c "import json; json.load(open('/root/AlgoTrendy_v2.6/integrations/tradingview/project_config.json'))"
```

---

### PHASE 4: Code Compilation Check

**Agent Name:** Code-Compiler
**Priority:** P0 (Critical)
**Dependencies:** Phase 1, 2
**Estimated Time:** 1 hour

#### Prompt for AI Agent:

```
TASK: Verify all TradingView Python code compiles and passes quality checks

CONTEXT:
- Source directory: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Virtual environment: tradingview_venv (from Phase 1)

STEPS:
1. Activate virtual environment from Phase 1
2. Compile all Python files using py_compile
3. Run pylint on all Python files (errors only)
4. Run flake8 for PEP8 compliance
5. Check for deprecated Python features (Python 3.10+)
6. Look for common code smells:
   - Unused imports
   - Undefined variables
   - Print statements (should use logging)
   - Hardcoded credentials
7. Generate code quality report

COMMANDS TO USE:
```bash
source tradingview_venv/bin/activate
python -m py_compile integrations/tradingview/*.py
python -m pylint integrations/tradingview/*.py --errors-only
python -m flake8 integrations/tradingview/*.py --max-line-length=120
```

SUCCESS CRITERIA:
- All files compile without syntax errors
- No critical pylint errors
- Code quality acceptable (pylint score > 7.0)

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase4_compilation_report.md

Include:
- Compilation results for each file
- Pylint scores
- List of errors/warnings
- Code quality metrics
- Recommended fixes
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase4_compilation_report.md
grep "PASS\|FAIL" /root/AlgoTrendy_v2.6/reports/phase4_compilation_report.md
```

---

### PHASE 5: Backend Integration

**Agent Name:** Backend-Integrator
**Priority:** P0 (Critical - BLOCKING)
**Dependencies:** Phase 1, 2, 3, 4
**Estimated Time:** 3 hours

#### Prompt for AI Agent:

```
TASK: Integrate TradingView webhook receiver with AlgoTrendy v2.6 backend

CONTEXT:
- Backend architecture: /root/AlgoTrendy_v2.6/backend/
- TradingView source: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Database: Check backend/models/ for schema
- API framework: Likely FastAPI or Flask (check backend code)

CRITICAL: This is a BLOCKING task. Other phases depend on this.

STEPS:
1. Study the backend architecture:
   - Identify API framework (FastAPI/Flask/Django)
   - Find route registration mechanism
   - Understand database ORM (SQLAlchemy/Django ORM)
   - Check authentication middleware

2. Create TradingView webhook endpoint:
   - Route: POST /api/tradingview/webhook
   - Accept JSON payload
   - Validate webhook signature/API key
   - Parse TradingView alert data

3. Database integration:
   - Create migration for tradingview_signals table (if needed)
   - Add models for:
     - Signal (timestamp, symbol, action, price, strategy)
     - WebhookLog (for debugging)
   - Run migrations

4. Add additional endpoints:
   - GET /api/tradingview/status (health check)
   - GET /api/tradingview/strategies (list active strategies)
   - GET /api/tradingview/signals (retrieve recent signals)

5. Authentication:
   - Implement API key validation for webhooks
   - Add CSRF protection if needed
   - Rate limiting (prevent spam)

6. Message Queue (if applicable):
   - Connect to Redis/RabbitMQ
   - Queue signals for async processing
   - Implement worker to process queued signals

7. Testing:
   - Create test webhook payload
   - Send test request to webhook endpoint
   - Verify database record created
   - Check logs

IMPORTANT:
- DO NOT modify existing backend code unnecessarily
- Follow existing patterns and conventions
- Add comprehensive error handling
- Log all webhook activity

SUCCESS CRITERIA:
- Webhook endpoint responds to POST requests
- Valid webhooks are stored in database
- Invalid webhooks are rejected with proper error codes
- Authentication works
- No existing functionality broken

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase5_backend_integration_report.md

Include:
- Backend framework identified
- Files created/modified (with paths)
- API endpoint documentation
- Database schema changes
- Test results (with example payload)
- Integration points mapped
- Status: PASS/FAIL

ALSO CREATE:
- API documentation: /root/AlgoTrendy_v2.6/docs/tradingview_api.md
- Test payload: /root/AlgoTrendy_v2.6/integrations/tradingview/test_webhook.json
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase5_backend_integration_report.md

# Test webhook endpoint (if backend running)
curl -X POST http://localhost:8000/api/tradingview/webhook \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test_key" \
  -d @/root/AlgoTrendy_v2.6/integrations/tradingview/test_webhook.json
```

---

### PHASE 6: Broker Integration

**Agent Name:** Broker-Integrator
**Priority:** P0 (Critical)
**Dependencies:** Phase 5
**Estimated Time:** 2 hours

#### Prompt for AI Agent:

```
TASK: Connect TradingView signals to broker execution layer

CONTEXT:
- TradingView integration: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Broker integrations: /root/AlgoTrendy_v2.6/backend/brokers/ (or similar)
- Signal format: Defined in webhook_template.json
- Target brokers: Check BROKERS_IMPLEMENTATION_COMPLETE.md in project root

STEPS:
1. Identify broker abstraction layer:
   - Find broker interface/base class
   - List supported brokers (IB, TradeStation, Bybit, etc.)
   - Understand order submission API

2. Create signal-to-order translation:
   - Map TradingView alert fields to broker order fields
   - Handle different order types (market, limit, stop)
   - Implement position sizing logic
   - Add leverage calculations (if applicable)

3. Implement order router:
   - Function: route_signal_to_broker(signal, broker_name)
   - Parse signal data
   - Apply risk checks (account balance, position limits)
   - Format order for specific broker
   - Submit order
   - Return execution result

4. Add paper trading support:
   - Check for paper_trading flag in signal
   - If paper mode, simulate order execution
   - Record simulated trades in database
   - Calculate simulated P&L

5. Error handling:
   - Broker API errors
   - Invalid signal data
   - Insufficient funds
   - Market closed
   - Implement retry logic with exponential backoff

6. Create broker-specific implementations:
   - File: tradingview_broker_router.py
   - Support at least 2 brokers initially
   - Test with paper accounts

7. Testing:
   - Test signal routing to each broker
   - Verify order format is correct
   - Test error scenarios
   - Verify paper trading mode

SUCCESS CRITERIA:
- Signals successfully route to brokers
- Orders formatted correctly for each broker
- Paper trading mode works
- Risk checks applied
- Error handling comprehensive

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase6_broker_integration_report.md

Include:
- Broker abstraction layer identified
- List of supported brokers
- Signal-to-order mapping table
- Risk checks implemented
- Test results for each broker
- Paper trading test results
- Status: PASS/FAIL

ALSO CREATE:
- /root/AlgoTrendy_v2.6/integrations/tradingview/broker_router.py (if needed)
- Test results: /root/AlgoTrendy_v2.6/tests/tradingview_broker_tests.md
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase6_broker_integration_report.md
cat /root/AlgoTrendy_v2.6/tests/tradingview_broker_tests.md
```

---

### PHASE 7: MEM/AI Agent Integration

**Agent Name:** MEM-Integrator
**Priority:** P1
**Dependencies:** Phase 5
**Estimated Time:** 2 hours
**Can Run in Parallel With:** Phase 6

#### Prompt for AI Agent:

```
TASK: Integrate TradingView with MemGPT AI agents for intelligent signal analysis

CONTEXT:
- MemGPT companion: /root/AlgoTrendy_v2.6/integrations/tradingview/memgpt_tradingview_companion.py
- MemGPT plotter: /root/AlgoTrendy_v2.6/integrations/tradingview/memgpt_tradingview_plotter.py
- MEM toolbox: /root/AlgoTrendy_v2.6/MEM/MEM_Modules_toolbox/Tradingview_x_Algotrendy/

STEPS:
1. Check if MemGPT server is available:
   - Look for MemGPT configuration
   - Check if server is running
   - Verify API endpoint

2. Review MemGPT companion code:
   - Understand how it processes TradingView signals
   - Check what insights it generates
   - Identify required environment variables

3. Create integration workflow:
   - When TradingView signal received → send to MemGPT
   - MemGPT analyzes signal context
   - MemGPT provides trading insights/confirmation
   - Store MemGPT analysis in database
   - Option to require MemGPT approval before execution

4. Implement chart analysis:
   - Use memgpt_tradingview_plotter.py
   - Generate chart analysis for signals
   - Store analysis results

5. Configure memory persistence:
   - Ensure MemGPT remembers previous signals
   - Track strategy performance over time
   - Learn from successful/failed trades

6. Test integration:
   - Send test signal
   - Verify MemGPT responds
   - Check analysis quality
   - Verify memory persistence

NOTE: If MemGPT server is not available, document the setup requirements for future implementation.

SUCCESS CRITERIA:
- MemGPT integration working OR detailed setup plan created
- Signals sent to MemGPT for analysis
- Insights stored and retrievable
- Memory persists between sessions (if running)

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase7_mem_integration_report.md

Include:
- MemGPT availability status
- Integration architecture
- Analysis workflow diagram
- Test results (if MemGPT available)
- Setup instructions (if not available)
- Configuration requirements
- Status: PASS/FAIL/PENDING_SETUP
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase7_mem_integration_report.md
# Check if MemGPT service is running
ps aux | grep memgpt
```

---

### PHASE 8: OpenAlgo Integration

**Agent Name:** OpenAlgo-Integrator
**Priority:** P2
**Dependencies:** Phase 5
**Estimated Time:** 1.5 hours
**Can Run in Parallel With:** Phase 6, 7

#### Prompt for AI Agent:

```
TASK: Integrate OpenAlgo strategy platform with TradingView

CONTEXT:
- OpenAlgo source: /root/AlgoTrendy_v2.6/integrations/strategies_external/external_strategies/openalgo/
- OpenAlgo TradingView files:
  - templates/tradingview.html
  - static/js/tradingview.js
  - static/css/tradingview.css
  - test/test_tradingview_csrf.py

STEPS:
1. Understand OpenAlgo architecture:
   - Check if it's a Flask app
   - Find main application file
   - Understand routing structure

2. Verify TradingView UI components:
   - Check if tradingview.html template exists
   - Verify JavaScript charting library
   - Check CSS styling

3. Test CSRF protection:
   - Run test_tradingview_csrf.py
   - Verify CSRF tokens working
   - Document any issues

4. Start OpenAlgo server:
   - Find startup script
   - Check required environment variables
   - Start server
   - Verify it runs without errors

5. Test UI:
   - Access TradingView interface
   - Verify charts render
   - Test strategy CRUD operations
   - Check webhook configuration UI

6. Integration testing:
   - Send TradingView webhook through OpenAlgo
   - Verify it reaches AlgoTrendy backend
   - Test strategy activation/deactivation

SUCCESS CRITERIA:
- OpenAlgo server starts successfully
- TradingView UI accessible
- Charts render correctly
- CSRF tests pass
- Strategy management functional

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase8_openalgo_integration_report.md

Include:
- OpenAlgo architecture summary
- Server startup instructions
- UI screenshots (describe what you see)
- CSRF test results
- Integration test results
- Known issues
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase8_openalgo_integration_report.md

# Check if OpenAlgo is running
ps aux | grep openalgo
netstat -tuln | grep 5000  # or whatever port OpenAlgo uses
```

---

### PHASE 9: Unit Testing

**Agent Name:** Unit-Tester
**Priority:** P0 (Critical)
**Dependencies:** Phase 5, 6, 7, 8
**Estimated Time:** 2 hours
**Parallelizable:** Yes (can split by module)

#### Prompt for AI Agent:

```
TASK: Create comprehensive unit tests for TradingView integration

CONTEXT:
- Testing framework: pytest (install if needed)
- Source code: /root/AlgoTrendy_v2.6/integrations/tradingview/
- Test directory: /root/AlgoTrendy_v2.6/tests/tradingview/

STEPS:
1. Create test directory structure:
   ```
   tests/tradingview/
   ├── __init__.py
   ├── test_webhook_parsing.py
   ├── test_signal_validation.py
   ├── test_order_translation.py
   ├── test_risk_checks.py
   ├── test_authentication.py
   └── conftest.py (pytest fixtures)
   ```

2. Write tests for webhook parsing:
   - Test valid webhook payload
   - Test malformed JSON
   - Test missing required fields
   - Test invalid data types
   - Test edge cases (extreme prices, negative values)

3. Write tests for signal validation:
   - Test valid signals
   - Test invalid symbols
   - Test invalid actions (buy/sell/close)
   - Test quantity validation
   - Test price validation

4. Write tests for order translation:
   - Test market order creation
   - Test limit order creation
   - Test stop order creation
   - Test position sizing calculations
   - Test leverage calculations

5. Write tests for risk checks:
   - Test account balance check
   - Test position size limits
   - Test maximum loss limits
   - Test concurrent position limits

6. Write tests for authentication:
   - Test valid API key
   - Test invalid API key
   - Test missing API key
   - Test expired tokens (if applicable)

7. Run tests:
   ```bash
   pytest tests/tradingview/ -v --cov=integrations/tradingview --cov-report=html
   ```

8. Analyze coverage:
   - Aim for > 80% code coverage
   - Identify untested code paths
   - Add tests for critical paths

SUCCESS CRITERIA:
- All tests pass
- Code coverage > 80%
- Edge cases covered
- Critical paths tested

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase9_unit_test_report.md

Include:
- Number of tests created
- Test results summary
- Code coverage percentage
- Coverage report location
- Untested code paths
- Status: PASS/FAIL

ALSO CREATE:
- Test suite: /root/AlgoTrendy_v2.6/tests/tradingview/*.py
- Coverage report: /root/AlgoTrendy_v2.6/tests/coverage_html/
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase9_unit_test_report.md
pytest /root/AlgoTrendy_v2.6/tests/tradingview/ -v
```

---

### PHASE 10: Integration Testing

**Agent Name:** Integration-Tester
**Priority:** P0 (Critical - BLOCKING)
**Dependencies:** Phase 9
**Estimated Time:** 3 hours

#### Prompt for AI Agent:

```
TASK: Test end-to-end integration flows for TradingView

CONTEXT:
- Full system must be operational
- This is a BLOCKING task - validates entire integration

TEST SCENARIOS:

SCENARIO 1: Basic Webhook Flow
1. Start backend server
2. Send mock TradingView webhook:
   ```json
   {
     "strategy": "test_strategy",
     "symbol": "BTCUSDT",
     "action": "buy",
     "price": 45000,
     "quantity": 0.01
   }
   ```
3. Verify:
   - Webhook received (check logs)
   - Signal parsed correctly
   - Stored in database
   - Response code 200

SCENARIO 2: Paper Trading Execution
1. Send buy signal with paper_trading flag
2. Verify paper trade executed
3. Check position in database
4. Send sell signal
5. Verify position closed
6. Check P&L calculated

SCENARIO 3: Multi-Broker Routing
1. Send signal specifying "broker": "bybit"
2. Verify routed to Bybit
3. Check order format is Bybit-specific
4. Send signal specifying "broker": "interactive_brokers"
5. Verify routed to IB
6. Check order format is IB-specific

SCENARIO 4: Error Handling
1. Send malformed webhook (invalid JSON)
2. Verify error logged
3. Verify response code 400
4. Verify system remains stable
5. Send valid webhook after error
6. Verify system recovered

SCENARIO 5: MemGPT Integration (if available)
1. Send signal
2. Verify MemGPT receives signal
3. Check MemGPT analysis generated
4. Verify analysis stored

SCENARIO 6: OpenAlgo UI (if available)
1. Access OpenAlgo TradingView page
2. Create new strategy
3. Configure webhook
4. Activate strategy
5. Send webhook for that strategy
6. Verify execution

STEPS:
1. Set up test environment
2. Start all required services
3. Execute each scenario
4. Document results
5. Screenshot any UI (describe what you see)
6. Check logs for errors
7. Verify database state

SUCCESS CRITERIA:
- At least 4/6 scenarios pass
- Webhook flow works reliably
- Paper trading works
- Error handling robust
- No data corruption

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase10_integration_test_report.md

Include:
- Test environment setup
- Scenario results (PASS/FAIL for each)
- Screenshots/descriptions of UI
- Log excerpts
- Database state verification
- Issues encountered
- Overall status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase10_integration_test_report.md
```

---

### PHASE 11: Performance Testing

**Agent Name:** Performance-Tester
**Priority:** P1
**Dependencies:** Phase 10
**Estimated Time:** 2 hours
**Can Run in Parallel With:** Phase 12

#### Prompt for AI Agent:

```
TASK: Test TradingView integration performance under load

CONTEXT:
- Target: Handle 100 webhooks/second
- Latency target: < 100ms (p95)
- No memory leaks
- Graceful degradation under load

TOOLS TO USE:
- Apache Bench (ab) for HTTP load testing
- Python cProfile for profiling
- memory_profiler for memory analysis
- Locust (if available)

TESTS TO PERFORM:

TEST 1: Latency Test
- Send 100 webhooks sequentially
- Measure time from send to database insert
- Calculate: min, max, avg, p50, p95, p99
- Target: p95 < 100ms

TEST 2: Throughput Test
- Send webhooks at increasing rates: 10, 50, 100, 200 req/sec
- Measure successful processing rate
- Check for dropped requests
- Target: 100 req/sec sustained

TEST 3: Stress Test
- Sustain 100 req/sec for 10 minutes
- Monitor: CPU, memory, disk I/O
- Check for memory leaks (memory should stabilize)
- Verify no degradation over time

TEST 4: Spike Test
- Normal load: 10 req/sec
- Sudden spike: 500 req/sec for 30 seconds
- Return to normal: 10 req/sec
- Verify: System recovers, no data loss

COMMANDS:
```bash
# Latency test
ab -n 100 -c 1 -p test_webhook.json -T application/json http://localhost:8000/api/tradingview/webhook

# Throughput test
ab -n 1000 -c 100 -p test_webhook.json -T application/json http://localhost:8000/api/tradingview/webhook

# Profile code
python -m cProfile -o profile.stats tradingview_integration.py
python -c "import pstats; p = pstats.Stats('profile.stats'); p.sort_stats('cumulative'); p.print_stats(20)"

# Memory profiling
python -m memory_profiler tradingview_integration.py
```

MONITORING:
- Use top/htop to monitor resources
- Check database query times
- Monitor network I/O

SUCCESS CRITERIA:
- Latency p95 < 100ms
- Sustained 100 req/sec
- No memory leaks
- System recovers from spikes
- No data loss under load

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase11_performance_test_report.md

Include:
- Latency statistics table
- Throughput test results
- Resource usage graphs (describe trends)
- Bottlenecks identified
- Optimization recommendations
- Profile output (top 20 functions)
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase11_performance_test_report.md
```

---

### PHASE 12: Security Testing

**Agent Name:** Security-Tester
**Priority:** P0 (Critical)
**Dependencies:** Phase 10
**Estimated Time:** 2 hours
**Can Run in Parallel With:** Phase 11

#### Prompt for AI Agent:

```
TASK: Perform security audit of TradingView integration

CONTEXT:
- Focus: Input validation, authentication, secrets management
- Standards: OWASP Top 10
- Tools: Bandit, Safety, manual testing

SECURITY CHECKS:

CHECK 1: Authentication
- Test valid API key → should succeed
- Test invalid API key → should return 401
- Test missing API key → should return 401
- Test replay attack (same webhook twice)
- Test token expiration (if applicable)

CHECK 2: Input Validation
- SQL injection attempts in symbol field
- XSS attempts in strategy name
- Command injection in parameters
- Buffer overflow (very long strings)
- Negative numbers where not expected
- Special characters

CHECK 3: CSRF Protection
- Run: python test/test_tradingview_csrf.py
- Verify CSRF tokens required for state-changing operations
- Test token validation

CHECK 4: Secrets Management
- Check logs for exposed API keys
- Verify credentials not in source code
- Check .env file not in git
- Verify encrypted storage of sensitive data

CHECK 5: Rate Limiting
- Send 1000 webhooks in 1 second
- Verify rate limiting kicks in
- Check error message doesn't expose system details

CHECK 6: Error Messages
- Trigger various errors
- Verify error messages don't expose:
  - Stack traces to users
  - Database structure
  - Internal paths
  - Version numbers

CHECK 7: Dependency Vulnerabilities
```bash
# Install safety
pip install safety

# Check for known vulnerabilities
safety check --file requirements.txt

# Run Bandit security linter
pip install bandit
bandit -r integrations/tradingview/
```

CHECK 8: HTTPS/TLS
- Verify webhook endpoint enforces HTTPS in production
- Check TLS version (should be 1.2+)
- Verify certificate validation

SUCCESS CRITERIA:
- No high/critical vulnerabilities
- Authentication works correctly
- Input validation comprehensive
- Secrets properly managed
- CSRF protection active
- Rate limiting functional

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase12_security_audit_report.md

Include:
- Authentication test results
- Input validation test cases
- CSRF test results
- Secrets management audit
- Dependency vulnerability scan results
- Bandit scan results
- Findings summary (Critical/High/Medium/Low)
- Remediation recommendations
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase12_security_audit_report.md
safety check --file /root/AlgoTrendy_v2.6/integrations/tradingview/requirements.txt
```

---

### PHASE 13: End-to-End Testing

**Agent Name:** E2E-Tester
**Priority:** P0 (Critical - FINAL VALIDATION)
**Dependencies:** Phase 10, 11, 12
**Estimated Time:** 2 hours

#### Prompt for AI Agent:

```
TASK: Perform end-to-end user workflow testing

CONTEXT:
- Simulate real user journeys
- Test complete workflows from setup to execution
- Final validation before production

WORKFLOW 1: Strategy Setup & Execution
STEPS:
1. User creates TradingView Pine Script strategy
2. User configures webhook in TradingView alert
3. TradingView sends test alert
4. AlgoTrendy receives webhook
5. Signal appears in dashboard/logs
6. Signal triggers paper trade
7. User sees execution confirmation

VALIDATE:
- Each step completes successfully
- Data flows through entire stack
- UI updates reflect backend state
- Timing is acceptable

WORKFLOW 2: Live Trading (Paper Mode)
STEPS:
1. User enables paper trading mode in settings
2. User connects broker (paper account)
3. TradingView sends BUY signal
4. System processes signal
5. Risk checks pass
6. Paper order placed
7. Position recorded in database
8. TradingView sends SELL signal
9. Position closed
10. P&L calculated and displayed

VALIDATE:
- All steps execute in order
- No steps skipped
- P&L calculation correct
- Position tracking accurate

WORKFLOW 3: Multi-Strategy Management
STEPS:
1. User creates Strategy A (BTCUSDT, 15min)
2. User creates Strategy B (ETHUSDT, 1hour)
3. Both strategies send signals simultaneously
4. System processes both independently
5. Both execute without conflicts
6. User views both in dashboard

VALIDATE:
- Concurrent processing works
- No race conditions
- Strategies isolated from each other

WORKFLOW 4: Error Recovery
STEPS:
1. User sets up strategy
2. TradingView sends signal
3. Simulate broker API failure
4. System logs error
5. System retries (exponential backoff)
6. Broker comes back online
7. Order eventually executes
8. User notified of delay

VALIDATE:
- Error handled gracefully
- Retry logic works
- User informed of status
- No data corruption

WORKFLOW 5: MemGPT-Assisted Trading (if available)
STEPS:
1. User enables MemGPT analysis
2. TradingView sends signal
3. MemGPT analyzes signal
4. MemGPT provides insight/recommendation
5. User sees analysis in dashboard
6. User decides to proceed or cancel
7. Trade executes based on decision

VALIDATE:
- MemGPT analysis arrives
- Analysis is helpful/relevant
- User can override MemGPT
- Decision flow works

TESTING METHOD:
- Use actual TradingView webhooks (or realistic mocks)
- Check database state at each step
- Monitor logs for errors
- Screenshot/describe UI at key points
- Measure timing for each workflow

SUCCESS CRITERIA:
- At least 3/5 workflows complete successfully
- No critical errors
- User experience is smooth
- Data integrity maintained
- Response times acceptable

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/reports/phase13_e2e_test_report.md

Include:
- Workflow results (PASS/FAIL for each)
- Step-by-step validation
- Screenshots/UI descriptions
- Timing for each workflow
- User experience observations
- Issues encountered
- Overall assessment
- Production readiness recommendation
- Status: PASS/FAIL
```

#### Validation Commands:
```bash
cat /root/AlgoTrendy_v2.6/reports/phase13_e2e_test_report.md
```

---

## 🔄 Parallel Execution Strategy

### Optimal Execution Order:

**Wave 1** (Sequential):
1. Phase 1: Environment-Builder (30 min)

**Wave 2** (Parallel after Wave 1):
2. Phase 2: Dependency-Analyzer (1 hour)
3. Phase 3: Config-Validator (45 min)

**Wave 3** (Sequential - Phase 4, then Phase 5):
4. Phase 4: Code-Compiler (1 hour) - requires Phase 2
5. Phase 5: Backend-Integrator (3 hours) - BLOCKING, requires Phase 1-4

**Wave 4** (Parallel after Phase 5):
6. Phase 6: Broker-Integrator (2 hours)
7. Phase 7: MEM-Integrator (2 hours)
8. Phase 8: OpenAlgo-Integrator (1.5 hours)

**Wave 5** (Parallel after Wave 4):
9. Phase 9: Unit-Tester (2 hours) - can split by module

**Wave 6** (Sequential - Phase 10):
10. Phase 10: Integration-Tester (3 hours) - BLOCKING

**Wave 7** (Parallel after Phase 10):
11. Phase 11: Performance-Tester (2 hours)
12. Phase 12: Security-Tester (2 hours)

**Wave 8** (Sequential - Final):
13. Phase 13: E2E-Tester (2 hours)

**Total Time:**
- Sequential: ~22 hours
- Parallel: ~12 hours

---

## 📊 Progress Tracking

Create a status file to track progress:

**File:** `/root/AlgoTrendy_v2.6/AI_DELEGATION_STATUS.md`

```markdown
# AI Delegation Status

## Phase Completion

| Phase | Agent | Status | Started | Completed | Issues |
|-------|-------|--------|---------|-----------|--------|
| 1 | Environment-Builder | ✅ COMPLETE | 2025-10-19 10:00 | 2025-10-19 10:30 | None |
| 2 | Dependency-Analyzer | 🔄 IN PROGRESS | 2025-10-19 10:35 | - | - |
| ... | ... | ... | ... | ... | ... |

## Current Blockers

- None

## Next Steps

1. Wait for Phase 2 completion
2. Start Phase 4 when Phase 2 completes
3. ...
```

---

## ✅ Final Consolidation

After all phases complete, create final report:

**Agent Name:** Report-Consolidator
**Task:** Consolidate all phase reports into final comprehensive report

#### Prompt:

```
TASK: Create final consolidated report for TradingView integration

CONTEXT:
- All phase reports in: /root/AlgoTrendy_v2.6/reports/
- Create comprehensive summary

STEPS:
1. Read all phase reports (phase1 through phase13)
2. Extract key findings from each
3. Compile overall statistics:
   - Total files created/modified
   - Total tests created
   - Test pass rate
   - Performance metrics
   - Security findings
4. Create executive summary
5. List all deliverables
6. Document known issues
7. Provide recommendations
8. Production deployment checklist

DELIVERABLE:
Create file: /root/AlgoTrendy_v2.6/TRADINGVIEW_INTEGRATION_FINAL_REPORT.md

Include:
- Executive summary
- Phase-by-phase results
- Overall statistics
- Test coverage summary
- Performance summary
- Security summary
- Known issues
- Production readiness assessment
- Deployment checklist
- Recommendations for optimization
```

---

## 🚨 Troubleshooting

### Common Issues:

**Issue:** Phase dependency not met
**Solution:** Check dependency phase completed successfully, review that phase's report

**Issue:** Environment conflicts
**Solution:** Use isolated virtual environments, check Python version

**Issue:** Tests failing
**Solution:** Check logs, verify services running, review configuration

**Issue:** Performance below target
**Solution:** Profile code, optimize database queries, add caching

**Issue:** Security vulnerabilities found
**Solution:** Update dependencies, patch code, implement recommended fixes

---

## 📞 Support

For issues during execution:
1. Check phase-specific report for details
2. Review master plan: `TRADINGVIEW_BUILD_INTEGRATION_TEST_PLAN.md`
3. Reference v2.5 implementation: `/root/algotrendy_v2.5/` (read-only)

---

**End of AI Delegation Workflow**
**Version:** 1.0
**Last Updated:** 2025-10-19
