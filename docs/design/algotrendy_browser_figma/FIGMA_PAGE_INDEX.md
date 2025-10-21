# AlgoTrendy Page Index - Figma AI Design Brief

**Trading Platform**: 13 pages total (6 exist, 7 to build)
**Design Style**: Dark mode, professional Bloomberg Terminal aesthetic, data-dense but clean
**Color Scheme**: Dark navy backgrounds (#0B0E13), professional blue accent (#3B82F6), green (profit), red (loss)

---

## PRIMARY PAGES (Main Navigation)

### 1. DASHBOARD `/dashboard` ✅ EXISTS
**Purpose**: Main overview and command center
**Key Sections**:
- Portfolio summary card (total value, today's P&L, buying power)
- Active positions table (5 rows max, show symbol, P&L, size)
- Recent orders list (last 10 orders with status badges)
- Market watchlist (6-8 symbols with live prices)
- Performance chart (line chart, 30-day P&L)
- Quick action buttons (place order, close positions, pause trading)
- Alert notifications panel (margin warnings, order fills)

---

### 2. TRADING `/trading` ⏳ TO BUILD - PRIORITY 1
**Purpose**: Active trading interface to place and monitor orders
**Key Sections**:
- **Left Panel**: Order entry form
  - Symbol input with autocomplete
  - Buy/Sell toggle buttons
  - Order type dropdown (Market, Limit, Stop)
  - Quantity input with calculator
  - Price inputs (limit/stop price)
  - Submit order button (large, prominent)
  - Risk info display (position size, margin required)
- **Center**: Price chart (TradingView style, full height)
- **Right Panel**:
  - Order book (bids/asks ladder)
  - Recent trades (time & sales)
- **Bottom Panel**: Open orders table with quick cancel buttons

---

### 3. ORDERS `/orders` ✅ EXISTS
**Purpose**: View and manage all orders
**Key Sections**:
- Tab navigation (Active | History | Failed)
- **Active Orders Tab**:
  - Filterable data table (symbol, side, type, quantity, price, status)
  - Cancel button per row
  - Bulk actions toolbar (cancel all, export)
- **History Tab**:
  - Completed orders table
  - Date range filter
  - Export to CSV button
- **Failed Tab**:
  - Rejected orders with error messages
  - Retry button

---

### 4. POSITIONS `/positions` ✅ EXISTS
**Purpose**: Monitor open positions and P&L
**Key Sections**:
- Summary cards (total unrealized P&L, total exposure, margin used)
- Open positions table
  - Columns: Symbol, Side, Size, Entry Price, Current Price, Unrealized P&L, P&L%
  - Live updating P&L (green/red numbers)
  - Close button per row
  - Leverage badge (if leveraged)
- Position details modal (on row click)
  - Entry details
  - Current stats
  - Liquidation price warning (if applicable)
  - Close position form

---

### 5. STRATEGIES `/strategies` ✅ EXISTS
**Purpose**: Build, backtest, and manage trading algorithms
**Key Sections**:
- Tab navigation (My Strategies | Builder | Backtest Results | Live)
- **My Strategies Tab**:
  - Card grid of saved strategies
  - Each card: name, description, status badge, quick actions
- **Builder Tab**:
  - Strategy configuration form
  - Indicator selector (drag-drop or dropdown)
  - Parameter inputs
  - Code editor option (toggle view)
  - Save/Load buttons
  - Run backtest button
- **Backtest Results Tab**:
  - Performance metrics cards (return, Sharpe, drawdown, win rate)
  - Equity curve chart
  - Trade list table
  - Comparison tool (vs other strategies)

---

### 6. PORTFOLIO `/portfolio` ⏳ TO BUILD - PRIORITY 2
**Purpose**: Performance analytics and reporting
**Key Sections**:
- Tab navigation (Overview | Analytics | Reports | Risk)
- **Overview Tab**:
  - Account value chart (line chart, 30/90/365 day toggle)
  - Asset allocation pie chart
  - Performance metrics grid (6 key metrics)
  - Benchmark comparison
- **Analytics Tab**:
  - Trading statistics grid (win rate, avg win/loss, profit factor)
  - Performance breakdown charts (by symbol, by strategy, by time)
  - Best/worst trades table
- **Reports Tab**:
  - Date range selector
  - Generate report button
  - Download options (PDF, CSV, Excel)
  - Recent reports list
- **Risk Tab**:
  - Exposure breakdown (donut chart)
  - Margin health meter (gauge chart)
  - Value at Risk display
  - Liquidation warnings (if any)

---

### 7. MARKET `/market` ⏳ TO BUILD - PRIORITY 3
**Purpose**: Market data, charts, and analysis
**Key Sections**:
- Tab navigation (Watchlist | Charts | Market Overview | Data Explorer)
- **Watchlist Tab**:
  - Customizable symbol list (add/remove)
  - Live price table (symbol, price, 24h change %, volume)
  - Quick trade button per row
  - MEM insights callouts
- **Charts Tab**:
  - Full-screen TradingView chart
  - Symbol selector
  - Timeframe selector
  - Indicator toolbar
  - Drawing tools
  - Save layout button
- **Market Overview Tab**:
  - Top gainers table (10 rows)
  - Top losers table (10 rows)
  - Most active by volume (10 rows)
  - Market sentiment widgets (fear & greed index)
- **Data Explorer Tab**:
  - Query builder interface
  - Results table
  - Export data button

---

## SECONDARY PAGES (Utility)

### 8. SETTINGS `/settings` ⏳ TO BUILD
**Purpose**: Account and platform configuration
**Key Sections**:
- Vertical tab navigation (Account | Trading | Notifications | Appearance)
- **Account Tab**:
  - Profile info form (name, email)
  - API keys management (list, generate, revoke)
  - Two-factor auth setup
  - Change password
- **Trading Tab**:
  - Default order type dropdown
  - Confirmation dialogs toggles
  - Risk limits inputs (max position size, etc.)
  - Preferred exchanges checkboxes
- **Notifications Tab**:
  - Email notification toggles
  - Push notification toggles
  - Alert preferences sliders
- **Appearance Tab**:
  - Theme toggle (dark/light)
  - Layout preferences (sidebar vs top nav)
  - Chart color scheme selector
  - Language dropdown

---

### 9. HELP `/help` ⏳ TO BUILD - LOW PRIORITY
**Purpose**: Documentation and support
**Key Sections**:
- Search bar (prominent at top)
- Quick links cards (Getting Started, API Docs, Trading Guide, FAQ)
- Contact support form
- "Ask MEM" chat button
- Recent articles list
- Video tutorials grid

---

## AUTHENTICATION PAGES

### 10. LOGIN `/login` ✅ EXISTS
**Purpose**: User authentication
**Layout**:
- Centered card on dark background
- Logo at top
- Email input
- Password input
- Remember me checkbox
- Login button (full width)
- Forgot password link
- Sign up link
- MFA code input (conditional)

---

### 11. REGISTER `/register` ⏳ OPTIONAL
**Purpose**: New user signup
**Layout**:
- Similar to login page
- Additional fields: username, confirm password
- Terms & conditions checkbox
- Create account button
- Already have account? Login link

---

## ERROR PAGES

### 12. 404 NOT FOUND ✅ EXISTS
**Layout**:
- Centered content
- Large "404" text
- "Page not found" message
- Back to dashboard button
- Home link

---

### 13. 500 SERVER ERROR ⏳ OPTIONAL
**Layout**:
- Similar to 404
- "Something went wrong" message
- Refresh page button
- Contact support link

---

## PERSISTENT UI COMPONENTS (On All Pages)

### MEM CORNER WIDGET (Bottom-Right) ✅ EXISTS
**Collapsed State**:
- Small floating card (250px wide)
- MEM avatar + status dot
- Today's P&L
- Active strategies count
- Expand chat button

**Expanded State**:
- Larger panel (400px wide, 600px tall)
- Chat interface with message history
- Input box
- Quick suggestion chips
- Minimize button

### TOP NAVIGATION BAR ✅ EXISTS
**Layout**:
- Logo (left)
- Page links (center): Dashboard, Trading, Orders, Positions, Strategies, Portfolio, Market
- Right side: MEM icon, Settings icon, Profile dropdown

### SIDEBAR NAVIGATION (Alternative) ✅ EXISTS
**Layout**:
- Collapsible sidebar (250px wide when open)
- Logo at top
- Navigation links (icons + text)
- Collapse button at bottom
- Mobile: Hamburger menu

---

## DESIGN SPECIFICATIONS

### Typography
- **Headers**: Inter, Bold, 24-32px
- **Body**: Inter, Regular, 14-16px
- **Data/Numbers**: JetBrains Mono, 14px
- **Labels**: Inter, Medium, 12px

### Colors
- **Background**: #0B0E13 (deep charcoal)
- **Cards**: #141922 (elevated surfaces)
- **Borders**: #1F2937 (subtle dividers)
- **Text Primary**: #F9FAFB
- **Text Secondary**: #9CA3AF
- **Accent Blue**: #3B82F6 (MEM, links, buttons)
- **Success/Profit**: #10B981
- **Warning**: #F59E0B
- **Error/Loss**: #EF4444

### Spacing
- **Page Padding**: 24px
- **Card Padding**: 20px
- **Component Gap**: 16px
- **Table Row Height**: 48px
- **Button Height**: 40px

### Components
- **Buttons**: Rounded corners (6px), solid background, hover state
- **Inputs**: Dark background, light border, 40px height
- **Cards**: Dark background, subtle border, 8px border-radius
- **Tables**: Striped rows, hover highlight, sticky header
- **Charts**: Dark theme, grid lines subtle, tooltips on hover
- **Badges**: Small, rounded-full, colored background (status-specific)
- **Dropdowns**: Dark background, max-height with scroll
- **Modals**: Centered, overlay backdrop (rgba(0,0,0,0.7)), close button

---

## RESPONSIVE BREAKPOINTS

- **Desktop**: 1920px (primary target)
- **Laptop**: 1440px
- **Tablet**: 1024px (stack some panels)
- **Mobile**: 768px (hamburger menu, simplified tables)

---

## DATA VISUALIZATION COMPONENTS

- **Line Charts**: Performance, P&L over time
- **Bar Charts**: Volume, trade frequency
- **Pie Charts**: Asset allocation, exposure breakdown
- **Donut Charts**: Margin usage, portfolio composition
- **Gauge Charts**: Risk meters, health scores
- **Candlestick Charts**: Price charts (TradingView)
- **Heatmaps**: Correlation matrix, performance grid
- **Tables**: Orders, positions, trades (sortable, filterable)

---

## INTERACTION PATTERNS

- **Loading States**: Skeleton screens (shimmer effect)
- **Empty States**: Icon + message + CTA button
- **Error States**: Alert banner, inline validation
- **Success States**: Toast notifications (top-right)
- **Hover States**: Subtle highlight, cursor pointer
- **Active States**: Border highlight, background change
- **Disabled States**: Reduced opacity (0.5), no pointer
- **Drag & Drop**: Visual feedback, drop zones highlighted

---

**Design Priority**: Professional, data-dense, Bloomberg Terminal quality
**Mobile-First**: No (Desktop-first, but responsive)
**Dark Mode**: Primary (light mode optional later)
**Accessibility**: WCAG AA compliant, keyboard navigation

---

**Total Pages**: 13 (6 exist, 7 to build)
**Estimated Design Time**: 2-3 weeks for complete design system
**Figma Deliverables**: Component library, 13 page designs, responsive variants, prototype flows
