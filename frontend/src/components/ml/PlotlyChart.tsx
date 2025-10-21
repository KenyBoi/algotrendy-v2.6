import { useEffect, useRef } from 'react';

interface PlotlyChartProps {
  data: any; // Plotly figure JSON from API
  title?: string;
  height?: number;
}

export default function PlotlyChart({ data, title, height = 500 }: PlotlyChartProps) {
  const chartRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!chartRef.current || !data) return;

    // Dynamically import Plotly to reduce initial bundle size
    import('plotly.js-dist-min').then((Plotly) => {
      if (chartRef.current) {
        const figure = typeof data === 'string' ? JSON.parse(data) : data;

        Plotly.newPlot(
          chartRef.current,
          figure.data,
          {
            ...figure.layout,
            height,
            responsive: true,
            displayModeBar: true,
            displaylogo: false,
            modeBarButtonsToRemove: ['sendDataToCloud', 'toImage'],
          },
          { responsive: true }
        );
      }
    });

    return () => {
      if (chartRef.current) {
        import('plotly.js-dist-min').then((Plotly) => {
          if (chartRef.current) {
            Plotly.purge(chartRef.current);
          }
        });
      }
    };
  }, [data, height]);

  if (!data) {
    return (
      <div style={{
        height: `${height}px`,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'var(--card-bg)',
        borderRadius: '8px',
        color: 'var(--text-secondary)'
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '2rem', marginBottom: '1rem' }}>📊</div>
          <div>Loading visualization...</div>
        </div>
      </div>
    );
  }

  return (
    <div>
      {title && <h3 style={{ marginBottom: '1rem' }}>{title}</h3>}
      <div ref={chartRef} style={{ width: '100%' }} />
    </div>
  );
}
