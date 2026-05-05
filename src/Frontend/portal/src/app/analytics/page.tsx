'use client';

import { useState, useEffect, useMemo } from 'react';

// ── Types ──────────────────────────────────────────────────────────────────

type ProjectMetrics = {
  projectId: string;
  revenue: number;
  cost: number;
  profit: number;
  margin: number;
  billableHours: number;
  utilization: number;
};

type Anomaly = {
  projectId: string;
  type: string;
  message: string;
  severity: 'Info' | 'Warning' | 'Critical';
};

type SimulationResult = {
  originalRevenue: number;
  originalCost: number;
  originalProfit: number;
  originalMargin: number;
  simulatedRevenue: number;
  simulatedCost: number;
  simulatedProfit: number;
  simulatedMargin: number;
  profitDelta: number;
  marginDelta: number;
};

// ── Helpers ────────────────────────────────────────────────────────────────

const fmt = (n: number) =>
  new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(n);

const pct = (n: number) => `${n.toFixed(1)}%`;

// ── Component ──────────────────────────────────────────────────────────────

export default function AnalyticsDashboard() {
  const [projects, setProjects] = useState<ProjectMetrics[]>([]);
  const [anomalies, setAnomalies] = useState<Anomaly[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedProject, setSelectedProject] = useState<string | null>(null);

  // Simulation state
  const [rateChange, setRateChange] = useState(0);
  const [costChange, setCostChange] = useState(0);
  const [simResult, setSimResult] = useState<SimulationResult | null>(null);

  // Fetch data
  useEffect(() => {
    Promise.all([
      fetch('/api/analytics/projects').then(r => r.ok ? r.json() : []),
      fetch('/api/analytics/anomalies').then(r => r.ok ? r.json() : []),
    ])
      .then(([projectData, anomalyData]) => {
        setProjects(projectData);
        setAnomalies(anomalyData);
      })
      .catch(() => {
        // Fallback demo data
        setProjects([
          { projectId: 'demo-alpha', revenue: 2400000, cost: 1680000, profit: 720000, margin: 30, billableHours: 480, utilization: 82 },
          { projectId: 'demo-beta', revenue: 960000, cost: 864000, profit: 96000, margin: 10, billableHours: 192, utilization: 65 },
          { projectId: 'demo-gamma', revenue: 1800000, cost: 1260000, profit: 540000, margin: 30, billableHours: 360, utilization: 78 },
        ]);
        setAnomalies([
          { projectId: 'demo-beta', type: 'NegativeMargin', message: 'Project Beta margin is dangerously low at 10%.', severity: 'Warning' },
        ]);
      })
      .finally(() => setLoading(false));
  }, []);

  // Totals
  const totals = useMemo(() => {
    const revenue = projects.reduce((s, p) => s + p.revenue, 0);
    const cost = projects.reduce((s, p) => s + p.cost, 0);
    const profit = revenue - cost;
    const margin = revenue > 0 ? profit / revenue * 100 : 0;
    return { revenue, cost, profit, margin };
  }, [projects]);

  // Run simulation
  const runSimulation = async () => {
    if (!selectedProject) return;
    const project = projects.find(p => p.projectId === selectedProject);
    if (!project) return;

    try {
      const res = await fetch('/api/analytics/simulate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          currentRevenue: project.revenue,
          currentCost: project.cost,
          rateChangePercent: rateChange,
          costChangePercent: costChange,
        }),
      });
      if (res.ok) {
        const data = await res.json();
        setSimResult(data);
      }
    } catch {
      // Fallback: compute locally
      const newRev = project.revenue * (1 + rateChange / 100);
      const newCost = project.cost * (1 + costChange / 100);
      const newProfit = newRev - newCost;
      setSimResult({
        originalRevenue: project.revenue,
        originalCost: project.cost,
        originalProfit: project.profit,
        originalMargin: project.margin,
        simulatedRevenue: newRev,
        simulatedCost: newCost,
        simulatedProfit: newProfit,
        simulatedMargin: newRev > 0 ? newProfit / newRev * 100 : 0,
        profitDelta: newProfit - project.profit,
        marginDelta: (newRev > 0 ? newProfit / newRev * 100 : 0) - project.margin,
      });
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh', background: '#0f1117', color: '#6366f1', fontSize: '1.2rem' }}>
        Loading analytics...
      </div>
    );
  }

  return (
    <div className="analytics-dashboard">
      <style>{`
        .analytics-dashboard {
          font-family: 'Inter', 'Segoe UI', sans-serif;
          background: #0f1117;
          min-height: 100vh;
          padding: 2rem;
          color: #e2e8f0;
        }
        .dash-header h1 {
          font-size: 1.7rem;
          font-weight: 700;
          color: #fff;
          margin: 0 0 0.25rem;
        }
        .dash-header p {
          font-size: 0.85rem;
          color: #6b7280;
          margin: 0;
        }

        /* Anomaly banner */
        .anomaly-banner {
          margin-top: 1.25rem;
          background: linear-gradient(135deg, #1c0a0a, #2a0a0a);
          border: 1px solid #ef4444;
          border-radius: 10px;
          padding: 0.85rem 1.25rem;
        }
        .anomaly-banner h3 {
          color: #fca5a5;
          font-size: 0.8rem;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          margin: 0 0 0.5rem;
        }
        .anomaly-item {
          font-size: 0.82rem;
          color: #fca5a5;
          padding: 0.2rem 0;
        }
        .anomaly-item .severity {
          display: inline-block;
          padding: 1px 6px;
          border-radius: 4px;
          font-size: 0.7rem;
          font-weight: 600;
          margin-right: 0.5rem;
        }
        .severity-Critical { background: #ef4444; color: #fff; }
        .severity-Warning { background: #f59e0b; color: #000; }

        /* KPI Cards */
        .kpi-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
          gap: 1rem;
          margin-top: 1.5rem;
        }
        .kpi-card {
          background: #1a1d27;
          border: 1px solid #2d3149;
          border-radius: 12px;
          padding: 1.25rem;
          position: relative;
          overflow: hidden;
        }
        .kpi-card::before {
          content: '';
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          height: 3px;
        }
        .kpi-card.revenue::before { background: linear-gradient(90deg, #6366f1, #8b5cf6); }
        .kpi-card.cost::before { background: linear-gradient(90deg, #f59e0b, #ef4444); }
        .kpi-card.profit::before { background: linear-gradient(90deg, #10b981, #4ade80); }
        .kpi-card.margin::before { background: linear-gradient(90deg, #06b6d4, #3b82f6); }
        .kpi-label {
          font-size: 0.72rem;
          text-transform: uppercase;
          letter-spacing: 0.06em;
          color: #6b7280;
          margin-bottom: 0.4rem;
        }
        .kpi-value {
          font-size: 1.8rem;
          font-weight: 700;
          color: #fff;
        }

        /* Table */
        .table-card {
          background: #1a1d27;
          border: 1px solid #2d3149;
          border-radius: 12px;
          padding: 1.25rem;
          margin-top: 1.5rem;
          overflow-x: auto;
        }
        .table-card h2 {
          font-size: 1rem;
          font-weight: 600;
          color: #fff;
          margin: 0 0 1rem;
        }
        table { width: 100%; border-collapse: collapse; }
        th {
          text-align: left;
          padding: 0.6rem 0.75rem;
          font-size: 0.72rem;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #6b7280;
          border-bottom: 1px solid #2d3149;
        }
        td {
          padding: 0.65rem 0.75rem;
          border-bottom: 1px solid #1e2235;
          font-size: 0.88rem;
        }
        tr:hover td { background: #1e2235; }
        td.num { text-align: right; font-variant-numeric: tabular-nums; }
        .margin-badge {
          display: inline-block;
          padding: 2px 8px;
          border-radius: 999px;
          font-size: 0.75rem;
          font-weight: 600;
        }
        .margin-good { background: #064e3b; color: #4ade80; }
        .margin-ok { background: #422006; color: #fbbf24; }
        .margin-bad { background: #450a0a; color: #fca5a5; }

        /* Utilization bar */
        .util-bar {
          display: flex;
          align-items: center;
          gap: 0.4rem;
        }
        .util-track {
          width: 60px;
          height: 6px;
          background: #2d3149;
          border-radius: 3px;
          overflow: hidden;
        }
        .util-fill {
          height: 100%;
          border-radius: 3px;
          transition: width 0.3s;
        }
        .util-high { background: #4ade80; }
        .util-mid { background: #fbbf24; }
        .util-low { background: #ef4444; }

        /* Simulation panel */
        .sim-panel {
          background: #1a1d27;
          border: 1px solid #2d3149;
          border-radius: 12px;
          padding: 1.25rem;
          margin-top: 1.5rem;
        }
        .sim-panel h2 {
          font-size: 1rem;
          font-weight: 600;
          color: #fff;
          margin: 0 0 1rem;
        }
        .sim-controls {
          display: flex;
          flex-wrap: wrap;
          gap: 1.5rem;
          align-items: flex-end;
        }
        .sim-group label {
          display: block;
          font-size: 0.75rem;
          color: #6b7280;
          margin-bottom: 0.3rem;
        }
        .sim-group input[type='range'] {
          width: 200px;
          accent-color: #6366f1;
        }
        .sim-group select {
          background: #0f1117;
          border: 1px solid #2d3149;
          border-radius: 6px;
          color: #e2e8f0;
          padding: 0.4rem 0.6rem;
          font-size: 0.85rem;
        }
        .sim-run-btn {
          background: linear-gradient(135deg, #6366f1, #4f46e5);
          color: #fff;
          border: none;
          border-radius: 8px;
          padding: 0.5rem 1.2rem;
          font-size: 0.85rem;
          font-weight: 600;
          cursor: pointer;
          transition: opacity 0.15s;
        }
        .sim-run-btn:hover { opacity: 0.9; }
        .sim-result {
          margin-top: 1rem;
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
          gap: 0.75rem;
        }
        .sim-result-card {
          background: #0f1117;
          border: 1px solid #2d3149;
          border-radius: 8px;
          padding: 0.75rem;
        }
        .sim-result-label {
          font-size: 0.7rem;
          color: #6b7280;
          text-transform: uppercase;
        }
        .sim-result-value {
          font-size: 1.2rem;
          font-weight: 700;
          color: #fff;
          margin-top: 0.2rem;
        }
        .delta-positive { color: #4ade80; }
        .delta-negative { color: #ef4444; }
      `}</style>

      {/* Header */}
      <div className="dash-header">
        <h1>Profitability Intelligence</h1>
        <p>Real-time financial overview · {new Date().getFullYear()}</p>
      </div>

      {/* Anomaly banner */}
      {anomalies.length > 0 && (
        <div className="anomaly-banner">
          <h3>⚠ Anomalies Detected ({anomalies.length})</h3>
          {anomalies.map((a, i) => (
            <div key={i} className="anomaly-item">
              <span className={`severity severity-${a.severity}`}>{a.severity}</span>
              {a.message}
            </div>
          ))}
        </div>
      )}

      {/* KPI Cards */}
      <div className="kpi-grid">
        <div className="kpi-card revenue">
          <div className="kpi-label">Total Revenue</div>
          <div className="kpi-value">{fmt(totals.revenue)}</div>
        </div>
        <div className="kpi-card cost">
          <div className="kpi-label">Total Cost</div>
          <div className="kpi-value">{fmt(totals.cost)}</div>
        </div>
        <div className="kpi-card profit">
          <div className="kpi-label">Total Profit</div>
          <div className="kpi-value" style={{ color: totals.profit >= 0 ? '#4ade80' : '#ef4444' }}>
            {fmt(totals.profit)}
          </div>
        </div>
        <div className="kpi-card margin">
          <div className="kpi-label">Avg. Margin</div>
          <div className="kpi-value">{pct(totals.margin)}</div>
        </div>
      </div>

      {/* Project Profitability Table */}
      <div className="table-card">
        <h2>Project Profitability</h2>
        <table>
          <thead>
            <tr>
              <th>Project</th>
              <th style={{ textAlign: 'right' }}>Revenue</th>
              <th style={{ textAlign: 'right' }}>Cost</th>
              <th style={{ textAlign: 'right' }}>Profit</th>
              <th>Margin</th>
              <th style={{ textAlign: 'right' }}>Billable Hrs</th>
              <th>Utilization</th>
            </tr>
          </thead>
          <tbody>
            {projects.map(p => {
              const marginClass = p.margin >= 25 ? 'margin-good' : p.margin >= 10 ? 'margin-ok' : 'margin-bad';
              const utilClass = p.utilization >= 75 ? 'util-high' : p.utilization >= 60 ? 'util-mid' : 'util-low';
              return (
                <tr
                  key={p.projectId}
                  style={{ cursor: 'pointer' }}
                  onClick={() => setSelectedProject(p.projectId)}
                >
                  <td style={{ fontWeight: 500 }}>{p.projectId.substring(0, 12)}…</td>
                  <td className="num">{fmt(p.revenue)}</td>
                  <td className="num">{fmt(p.cost)}</td>
                  <td className="num" style={{ color: p.profit >= 0 ? '#4ade80' : '#ef4444' }}>
                    {fmt(p.profit)}
                  </td>
                  <td>
                    <span className={`margin-badge ${marginClass}`}>{pct(p.margin)}</span>
                  </td>
                  <td className="num">{p.billableHours.toFixed(0)}h</td>
                  <td>
                    <div className="util-bar">
                      <div className="util-track">
                        <div
                          className={`util-fill ${utilClass}`}
                          style={{ width: `${Math.min(100, p.utilization)}%` }}
                        />
                      </div>
                      <span style={{ fontSize: '0.78rem', color: '#9ca3af' }}>{pct(p.utilization)}</span>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Simulation Panel */}
      <div className="sim-panel">
        <h2>What-If Simulation</h2>
        <div className="sim-controls">
          <div className="sim-group">
            <label>Project</label>
            <select
              value={selectedProject ?? ''}
              onChange={e => {
                setSelectedProject(e.target.value);
                setSimResult(null);
              }}
            >
              <option value="">— Select —</option>
              {projects.map(p => (
                <option key={p.projectId} value={p.projectId}>
                  {p.projectId.substring(0, 12)}…
                </option>
              ))}
            </select>
          </div>

          <div className="sim-group">
            <label>Rate Change: {rateChange > 0 ? '+' : ''}{rateChange}%</label>
            <input
              type="range"
              min={-50}
              max={50}
              value={rateChange}
              onChange={e => setRateChange(Number(e.target.value))}
            />
          </div>

          <div className="sim-group">
            <label>Cost Change: {costChange > 0 ? '+' : ''}{costChange}%</label>
            <input
              type="range"
              min={-30}
              max={30}
              value={costChange}
              onChange={e => setCostChange(Number(e.target.value))}
            />
          </div>

          <button className="sim-run-btn" onClick={runSimulation} disabled={!selectedProject}>
            Run Simulation
          </button>
        </div>

        {simResult && (
          <div className="sim-result">
            <div className="sim-result-card">
              <div className="sim-result-label">Simulated Revenue</div>
              <div className="sim-result-value">{fmt(simResult.simulatedRevenue)}</div>
            </div>
            <div className="sim-result-card">
              <div className="sim-result-label">Simulated Cost</div>
              <div className="sim-result-value">{fmt(simResult.simulatedCost)}</div>
            </div>
            <div className="sim-result-card">
              <div className="sim-result-label">Simulated Profit</div>
              <div className="sim-result-value" style={{ color: simResult.simulatedProfit >= 0 ? '#4ade80' : '#ef4444' }}>
                {fmt(simResult.simulatedProfit)}
              </div>
            </div>
            <div className="sim-result-card">
              <div className="sim-result-label">Margin</div>
              <div className="sim-result-value">{pct(simResult.simulatedMargin)}</div>
            </div>
            <div className="sim-result-card">
              <div className="sim-result-label">Profit Δ</div>
              <div className={`sim-result-value ${simResult.profitDelta >= 0 ? 'delta-positive' : 'delta-negative'}`}>
                {simResult.profitDelta >= 0 ? '+' : ''}{fmt(simResult.profitDelta)}
              </div>
            </div>
            <div className="sim-result-card">
              <div className="sim-result-label">Margin Δ</div>
              <div className={`sim-result-value ${simResult.marginDelta >= 0 ? 'delta-positive' : 'delta-negative'}`}>
                {simResult.marginDelta >= 0 ? '+' : ''}{pct(simResult.marginDelta)}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
