import { Copy, Download, Check } from 'lucide-react';
import { useState } from 'react';

interface Props {
  code: string;
  language: 'python' | 'csharp';
}

export default function StrategyCodeViewer({ code, language }: Props) {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    const extension = language === 'python' ? 'py' : 'cs';
    const blob = new Blob([code], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `strategy.${extension}`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="code-viewer">
      <div className="code-header">
        <div>
          <h2 style={{ margin: 0 }}>Generated Code</h2>
          <p style={{ margin: '0.5rem 0 0 0', color: 'var(--text-secondary)' }}>
            Language: {language === 'python' ? 'Python' : 'C#'}
          </p>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            onClick={handleCopy}
            className="btn-secondary"
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
          >
            {copied ? <Check size={18} /> : <Copy size={18} />}
            {copied ? 'Copied!' : 'Copy'}
          </button>
          <button
            onClick={handleDownload}
            className="btn-secondary"
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
          >
            <Download size={18} />
            Download
          </button>
        </div>
      </div>

      {code ? (
        <pre className="code-block">
          <code>{code}</code>
        </pre>
      ) : (
        <div className="code-placeholder">
          <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '3rem' }}>
            No code generated yet. Click "Generate Code" in the header to create code from your strategy configuration.
          </p>
        </div>
      )}

      <style>{`
        .code-viewer {
          max-width: 1200px;
        }

        .code-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          margin-bottom: 1.5rem;
        }

        .code-block {
          background: var(--background);
          border: 1px solid var(--border);
          border-radius: 8px;
          padding: 1.5rem;
          overflow-x: auto;
          font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
          font-size: 0.9rem;
          line-height: 1.6;
          color: var(--text);
        }

        .code-block code {
          display: block;
          white-space: pre;
        }

        .code-placeholder {
          background: var(--background);
          border: 2px dashed var(--border);
          border-radius: 8px;
          min-height: 300px;
          display: flex;
          align-items: center;
          justify-content: center;
        }
      `}</style>
    </div>
  );
}
