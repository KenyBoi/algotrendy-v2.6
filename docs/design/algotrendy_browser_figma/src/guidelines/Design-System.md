# AlgoTrendy Design System

**Institutional-grade interface with intelligent AI collaboration**

---

## 🎨 Design Philosophy

### The Balance

**Professional aesthetics** that would fit in any top hedge fund office, combined with **intelligent AI assistance** that feels human and collaborative.

**Visual Language:** Bloomberg Terminal precision + Modern fintech elegance  
**Personality:** Sophisticated platform + Friendly AI collaborator  
**Experience:** Institutional tools + Human conversation

---

## 🎯 Core Principles

1. **Premium First** - Every pixel matters. Institutional quality.
2. **Data Clarity** - Information density without clutter
3. **Subtle Intelligence** - AI presence is professional, not playful
4. **Performance Focus** - Fast, responsive, no unnecessary animations
5. **Dark Mode Native** - Designed for extended trading sessions

---

## 🎨 Color System

### Base Palette (Dark Theme)

```css
/* Backgrounds */
--bg-primary: #0B0E13        /* Deep charcoal - main background */
--bg-secondary: #141922      /* Elevated surfaces, cards */
--bg-tertiary: #1C2128       /* Input backgrounds, elevated cards */
--bg-hover: #252A33          /* Hover states */

/* Borders & Dividers */
--border-subtle: #1F2937     /* Subtle dividers */
--border-default: #374151    /* Default borders */
--border-emphasis: #4B5563   /* Emphasized borders */

/* Text */
--text-primary: #F9FAFB      /* Primary text, headers */
--text-secondary: #D1D5DB    /* Secondary text */
--text-tertiary: #9CA3AF     /* Tertiary text, labels */
--text-muted: #6B7280        /* Muted text, placeholders */

/* MEM Accent (Professional Blue) */
--mem-primary: #3B82F6       /* Professional blue */
--mem-light: #60A5FA         /* Lighter variant */
--mem-dark: #2563EB          /* Darker variant */
--mem-glow: rgba(59, 130, 246, 0.15)  /* Subtle glow */

/* Status Colors */
--success: #10B981           /* Green - profits, success */
--success-bg: rgba(16, 185, 129, 0.1)
--warning: #F59E0B           /* Amber - warnings */
--warning-bg: rgba(245, 158, 11, 0.1)
--error: #EF4444             /* Red - losses, errors */
--error-bg: rgba(239, 68, 68, 0.1)
--info: #3B82F6              /* Blue - information */
--info-bg: rgba(59, 130, 246, 0.1)

/* Data Visualization */
--chart-1: #3B82F6           /* Blue */
--chart-2: #8B5CF6           /* Purple */
--chart-3: #10B981           /* Green */
--chart-4: #F59E0B           /* Amber */
--chart-5: #EF4444           /* Red */
--chart-6: #06B6D4           /* Cyan */
```

### Light Theme (Optional)

```css
/* For daytime trading or presentations */
--bg-primary: #FFFFFF
--bg-secondary: #F9FAFB
--bg-tertiary: #F3F4F6
--text-primary: #111827
--text-secondary: #374151
--text-tertiary: #6B7280
```

---

## 📝 Typography

### Font Stack

```css
/* Interface */
--font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif

/* Data & Numbers */
--font-mono: 'JetBrains Mono', 'SF Mono', 'Fira Code', 'Courier New', monospace

/* Optional: Headers */
--font-display: 'Inter', sans-serif
```

### Type Scale

```css
/* Headers */
h1: 28px / 600  /* Page titles */
h2: 20px / 600  /* Section headers */
h3: 16px / 600  /* Subsection headers */
h4: 14px / 600  /* Card titles */

/* Body */
body: 14px / 400       /* Default text */
body-sm: 13px / 400    /* Small text */
body-xs: 12px / 400    /* Tiny text, labels */

/* Data */
data-lg: 16px / 500 mono   /* Large numbers */
data-md: 14px / 500 mono   /* Default numbers */
data-sm: 12px / 500 mono   /* Small numbers */

/* Code */
code: 13px / 400 mono      /* Code blocks */
```

### Text Hierarchy

```tsx
// Page Title
className="text-[28px] font-semibold text-gray-50"

// Section Header
className="text-xl font-semibold text-gray-50"

// Card Title
className="text-sm font-semibold text-gray-50"

// Body Text
className="text-sm text-gray-300"

// Secondary Text
className="text-sm text-gray-400"

// Muted Text
className="text-xs text-gray-500"

// Numbers/Data
className="font-mono text-sm font-medium text-gray-50"
```

---

## 🎭 Component Aesthetics

### Cards

```tsx
// Standard Card
className="bg-slate-900 border border-slate-800 rounded-lg"

// Elevated Card
className="bg-slate-900 border border-slate-700 rounded-lg shadow-xl"

// Interactive Card
className="bg-slate-900 border border-slate-800 hover:border-slate-700 
           transition-colors rounded-lg cursor-pointer"

// MEM Card (subtle accent)
className="bg-slate-900 border border-blue-500/20 rounded-lg"
```

### Buttons

```tsx
// Primary Action
className="bg-blue-600 hover:bg-blue-700 text-white"

// Secondary Action
className="bg-slate-800 hover:bg-slate-700 border border-slate-700 text-gray-200"

// Destructive
className="bg-red-600 hover:bg-red-700 text-white"

// Ghost
className="hover:bg-slate-800 text-gray-300"

// MEM Action
className="bg-blue-600 hover:bg-blue-700 text-white 
           shadow-lg shadow-blue-500/20"
```

### Inputs

```tsx
// Text Input
className="bg-slate-800 border border-slate-700 
           focus:border-blue-500 focus:ring-1 focus:ring-blue-500
           text-gray-100 placeholder:text-gray-500"

// Select
className="bg-slate-800 border border-slate-700 text-gray-100"

// Number Input (monospace)
className="bg-slate-800 border border-slate-700 font-mono text-gray-100"
```

### Tables

```tsx
// Table Container
className="border border-slate-800 rounded-lg overflow-hidden"

// Table Header
className="bg-slate-800/50 border-b border-slate-800"

// Table Row
className="border-b border-slate-800 hover:bg-slate-800/30 
           transition-colors"

// Table Cell
className="text-sm text-gray-300"

// Numeric Cell
className="font-mono text-sm text-gray-100 text-right"
```

---

## ✨ Interactive States

### Subtle, Professional Animations

```css
/* Transitions - Fast & Crisp */
transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

/* Hover - Subtle */
hover:bg-slate-800
hover:border-slate-700

/* Focus - Clear but not distracting */
focus:ring-2 focus:ring-blue-500/50 focus:ring-offset-0
focus:border-blue-500

/* Active - Immediate feedback */
active:scale-[0.98]

/* Disabled - Clear indication */
disabled:opacity-50 disabled:cursor-not-allowed
```

### MEM-Specific Indicators

```tsx
// MEM Status Pulse (very subtle)
<motion.div
  animate={{ opacity: [1, 0.7, 1] }}
  transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
/>

// MEM Thinking (minimal)
<motion.div
  animate={{ scale: [1, 1.02, 1] }}
  transition={{ duration: 1.5, repeat: Infinity, ease: "easeInOut" }}
/>

// No bouncing, no playful animations
```

---

## 📊 Data Visualization

### Charts

```tsx
// Chart Colors (professional palette)
colors: ['#3B82F6', '#8B5CF6', '#10B981', '#F59E0B', '#EF4444']

// Grid Lines
stroke: '#374151'
strokeWidth: 1
strokeDasharray: '3 3'

// Tooltip
background: '#1C2128'
border: '1px solid #374151'
shadow: '0 4px 6px -1px rgba(0, 0, 0, 0.3)'
```

### Numbers

```tsx
// Large Numbers
className="font-mono text-2xl font-semibold text-gray-50"

// Percentage (Positive)
className="font-mono text-sm text-green-400"

// Percentage (Negative)  
className="font-mono text-sm text-red-400"

// Currency
className="font-mono text-base text-gray-100"
```

### Indicators

```tsx
// Status Badge - Success
className="bg-green-500/10 text-green-400 border border-green-500/20"

// Status Badge - Warning
className="bg-amber-500/10 text-amber-400 border border-amber-500/20"

// Status Badge - Error
className="bg-red-500/10 text-red-400 border border-red-500/20"

// Status Badge - Info
className="bg-blue-500/10 text-blue-400 border border-blue-500/20"
```

---

## 🤖 MEM's Visual Presence

### The Principle

**MEM's personality comes through in words, not visuals.**

The interface is professional. MEM's friendliness is in:
- ✅ Conversational language
- ✅ Helpful suggestions  
- ✅ Clear explanations
- ❌ NOT in bouncy animations
- ❌ NOT in casual colors
- ❌ NOT in playful UI elements

### MEM Status Indicator

```tsx
// Professional, minimal presence
<div className="flex items-center gap-2">
  <div className="w-2 h-2 rounded-full bg-blue-500 
                  shadow-sm shadow-blue-500/50" />
  <span className="text-xs text-gray-400 font-medium">
    MEM ACTIVE
  </span>
</div>
```

### MEM Corner Widget

```tsx
// Elegant, sophisticated
className="fixed bottom-6 right-6 
           bg-slate-900 border border-slate-700
           rounded-xl shadow-2xl
           backdrop-blur-sm"
```

### MEM Chat Interface

```tsx
// Clean, professional chat
// User messages
className="bg-blue-600 text-white rounded-lg"

// MEM messages
className="bg-slate-800 text-gray-100 border border-slate-700 rounded-lg"

// NOT colorful gradients, NOT casual styling
```

---

## 🎨 Layout Patterns

### Page Layout

```tsx
<div className="min-h-screen bg-slate-950">
  {/* Sidebar - Fixed */}
  <aside className="w-64 border-r border-slate-800 bg-slate-900" />
  
  {/* Main Content */}
  <main className="ml-64 p-8">
    {/* Page Header */}
    <header className="mb-6">
      <h1 className="text-[28px] font-semibold text-gray-50">Title</h1>
      <p className="text-sm text-gray-400">Description</p>
    </header>
    
    {/* Content Grid */}
    <div className="grid grid-cols-12 gap-6">
      {/* Cards */}
    </div>
  </main>
</div>
```

### Card Layout

```tsx
<Card className="bg-slate-900 border-slate-800">
  {/* Header */}
  <div className="p-4 border-b border-slate-800">
    <h3 className="text-sm font-semibold text-gray-50">Title</h3>
  </div>
  
  {/* Content */}
  <div className="p-4">
    {/* Content */}
  </div>
  
  {/* Footer (optional) */}
  <div className="p-4 border-t border-slate-800 bg-slate-800/30">
    {/* Actions */}
  </div>
</Card>
```

### Data Table

```tsx
<div className="border border-slate-800 rounded-lg overflow-hidden">
  <table className="w-full">
    <thead className="bg-slate-800/50">
      <tr>
        <th className="px-4 py-3 text-left text-xs font-medium 
                       text-gray-400 uppercase tracking-wider">
          Column
        </th>
      </tr>
    </thead>
    <tbody className="divide-y divide-slate-800">
      <tr className="hover:bg-slate-800/30">
        <td className="px-4 py-3 text-sm text-gray-300">Data</td>
      </tr>
    </tbody>
  </table>
</div>
```

---

## 📏 Spacing & Sizing

### Spacing Scale

```css
4px   - xs   - Tight spacing
8px   - sm   - Compact spacing
12px  - md   - Default spacing
16px  - lg   - Comfortable spacing
24px  - xl   - Section spacing
32px  - 2xl  - Page spacing
48px  - 3xl  - Major sections
```

### Container Widths

```css
--container-sm: 640px    /* Small content */
--container-md: 768px    /* Medium content */
--container-lg: 1024px   /* Large content */
--container-xl: 1280px   /* Extra large content */
--container-2xl: 1536px  /* Full width dashboards */
```

### Border Radius

```css
--radius-sm: 4px    /* Badges, small elements */
--radius-md: 6px    /* Buttons, inputs */
--radius-lg: 8px    /* Cards */
--radius-xl: 12px   /* Large cards, modals */
```

---

## 🎯 Component-Specific Guidelines

### MEM Corner

- **Size:** 80px × 80px (collapsed), 400px × 500px (expanded)
- **Position:** Fixed bottom-right with 24px margin
- **Background:** `bg-slate-900 border border-slate-700`
- **Shadow:** `shadow-2xl` with subtle glow
- **Animation:** Minimal - only status pulse
- **Typography:** Inter for text, monospace for numbers

### MEM Chat

- **Width:** 400px (desktop), full screen (mobile)
- **Height:** 600px max
- **Header:** `bg-slate-800 border-b border-slate-700`
- **Messages:** Clean bubbles, no gradients
- **Input:** Professional, minimal styling
- **Suggestions:** Subtle button pills

### Strategy Builder

- **Layout:** Grid-based, 12 columns
- **Cards:** Elevated cards with subtle borders
- **Drag handles:** Minimal, functional
- **Charts:** Professional Recharts styling
- **Controls:** Clean, labeled inputs

### Data Tables

- **Header:** Sticky, subtle background
- **Rows:** Hover state, alternating subtle bg (optional)
- **Numbers:** Right-aligned, monospace
- **Actions:** Icon buttons, minimal
- **Sorting:** Clear indicators

---

## 🚀 Performance Guidelines

1. **Minimize animations** - Only where they add value
2. **Lazy load components** - Split code intelligently
3. **Optimize re-renders** - Use React.memo wisely
4. **Debounce inputs** - Especially for API calls
5. **Virtual scrolling** - For large data tables
6. **Image optimization** - Use appropriate formats

---

## ♿ Accessibility

1. **Color contrast:** WCAG AA minimum (4.5:1)
2. **Focus indicators:** Always visible
3. **Keyboard navigation:** Full support
4. **Screen readers:** Proper ARIA labels
5. **Error messages:** Clear and descriptive
6. **Loading states:** Indicate progress

---

## 🎨 Example Component Styling

### Professional Card

```tsx
<Card className="bg-slate-900 border border-slate-800 hover:border-slate-700 
                 transition-colors rounded-lg overflow-hidden">
  <div className="p-4 border-b border-slate-800">
    <h3 className="text-sm font-semibold text-gray-50">
      Portfolio Performance
    </h3>
    <p className="text-xs text-gray-500 mt-0.5">
      Last 30 days
    </p>
  </div>
  <div className="p-6">
    <div className="flex items-baseline gap-2">
      <span className="text-2xl font-mono font-semibold text-gray-50">
        $124,567.89
      </span>
      <span className="text-sm font-mono text-green-400">
        +12.34%
      </span>
    </div>
  </div>
</Card>
```

### Professional Button

```tsx
<Button className="bg-blue-600 hover:bg-blue-700 text-white 
                   transition-colors px-4 py-2 rounded-md
                   text-sm font-medium shadow-sm">
  Execute Trade
</Button>
```

### Professional Input

```tsx
<div className="space-y-1.5">
  <label className="text-xs font-medium text-gray-400 uppercase tracking-wide">
    Symbol
  </label>
  <input
    className="w-full bg-slate-800 border border-slate-700 
               rounded-md px-3 py-2 text-sm text-gray-100
               focus:border-blue-500 focus:ring-1 focus:ring-blue-500
               placeholder:text-gray-500 transition-colors"
    placeholder="BTCUSDT"
  />
</div>
```

---

## 🎯 Summary

**Visual Language:** Dark, sophisticated, data-focused, institutional-grade

**MEM's Presence:** Professional blue accents, minimal animations, friendly *words* not playful *visuals*

**Typography:** Clean sans-serif for UI, monospace for data

**Interactions:** Subtle, fast, purposeful

**Feel:** A tool that belongs on a hedge fund trading desk, but with an intelligent AI collaborator that actually helps

---

**Last Updated:** 2025-10-20  
**Status:** Design System Defined  
**Target:** Institutional-grade professional interface
