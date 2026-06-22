'use client';

import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '@/features/auth/AuthContext';

// ── Types ──────────────────────────────────────────────────────────────────

type Project = {
  id: string;
  name: string;
  billingType: 'TimeAndMaterial' | 'FixedBid' | 'NonBillable';
};

type TimesheetRow = {
  id: string;
  projectId: string;
  isBillable: boolean;
  hours: number[]; // 7 days
};

type DayMeta = {
  label: string;
  date: string; // ISO
  isWeekend: boolean;
};

// ── Helpers ────────────────────────────────────────────────────────────────

function getWeekDays(weekStart: Date): DayMeta[] {
  const days: DayMeta[] = [];
  const labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  for (let i = 0; i < 7; i++) {
    const d = new Date(weekStart);
    d.setDate(weekStart.getDate() + i);
    days.push({
      label: labels[i] ?? '',
      date: d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      isWeekend: i >= 5,
    });
  }
  return days;
}

function getMondayOfCurrentWeek(): Date {
  const today = new Date();
  const day = today.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(today);
  monday.setDate(today.getDate() + diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
}

function createEmptyRow(): TimesheetRow {
  return {
    id: crypto.randomUUID(),
    projectId: '',
    isBillable: true,
    hours: [0, 0, 0, 0, 0, 0, 0],
  };
}

function totalForDay(rows: TimesheetRow[], day: number): number {
  return rows.reduce((sum, r) => sum + (r.hours[day] || 0), 0);
}

// ── Component ──────────────────────────────────────────────────────────────

export default function TimesheetPage() {
  const [rows, setRows] = useState<TimesheetRow[]>([createEmptyRow()]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [weekStart] = useState<Date>(getMondayOfCurrentWeek());
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [errors, setErrors] = useState<string[]>([]);
  const { user, isLoading: authLoading } = useAuth();

  const days = getWeekDays(weekStart);

  // Fetch employee's assigned projects from the PSA API
  useEffect(() => {
    if (authLoading || !user?.id) return;
    fetch(`/api/psa/employees/${user.id}/projects`)
      .then(r => r.ok ? r.json() : [])
      .then((data: Project[]) => setProjects(data))
      .catch(() => {
        // Fallback to mock projects in development
        setProjects([
          { id: 'mock-alpha', name: 'Alpha Portal Migration', billingType: 'TimeAndMaterial' },
          { id: 'mock-internal', name: 'Internal Training', billingType: 'NonBillable' },
        ]);
      });
  }, [authLoading, user?.id]);

  const addRow = () => setRows(prev => [...prev, createEmptyRow()]);

  const removeRow = (id: string) =>
    setRows(prev => prev.filter(r => r.id !== id));

  const updateRow = useCallback((id: string, patch: Partial<TimesheetRow>) => {
    setRows(prev =>
      prev.map(r => {
        if (r.id !== id) return r;
        const updated = { ...r, ...patch };
        // If project is NonBillable, force IsBillable = false
        if (patch.projectId) {
          const proj = projects.find(p => p.id === patch.projectId);
          if (proj?.billingType === 'NonBillable') {
            updated.isBillable = false;
          }
        }
        return updated;
      })
    );
  }, [projects]);

  const updateHours = useCallback((id: string, dayIdx: number, val: string) => {
    const num = Math.max(0, Math.min(24, parseFloat(val) || 0));
    setRows(prev =>
      prev.map(r => {
        if (r.id !== id) return r;
        const hours = [...r.hours];
        hours[dayIdx] = num;
        return { ...r, hours };
      })
    );
  }, []);

  const validate = (): boolean => {
    const errs: string[] = [];

    // Day total must not exceed 24h
    for (let d = 0; d < 7; d++) {
      const total = totalForDay(rows, d);
      if (total > 24) {
        const day = days[d];
        if (day) {
          errs.push(`${day.label} ${day.date} exceeds 24 hours (${total}h logged).`);
        }
      }
    }

    // Every row must have a project selected
    rows.forEach((r, i) => {
      if (!r.projectId) errs.push(`Row ${i + 1}: Please select a project.`);
    });

    // IsBillable requires a project that is TimeAndMaterial or FixedBid
    rows.forEach((r, i) => {
      if (r.isBillable) {
        const proj = projects.find(p => p.id === r.projectId);
        if (proj?.billingType === 'NonBillable') {
          errs.push(`Row ${i + 1}: "${proj.name}" is Non-Billable — hours cannot be marked billable.`);
        }
      }
    });

    setErrors(errs);
    return errs.length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    setIsSubmitting(true);
    setSubmitStatus('idle');

    try {
      const payload = {
        weekStartDate: weekStart.toISOString().split('T')[0],
        entries: rows.flatMap(row =>
          row.hours.map((h, di) => ({
            date: (() => {
              const d = new Date(weekStart);
              d.setDate(weekStart.getDate() + di);
              return d.toISOString().split('T')[0];
            })(),
            hours: h,
            projectId: row.projectId || null,
            isBillable: row.isBillable,
          })).filter(e => e.hours > 0)
        ),
      };

      const res = await fetch('/api/time/timesheets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      setSubmitStatus(res.ok ? 'success' : 'error');
    } catch {
      setSubmitStatus('error');
    } finally {
      setIsSubmitting(false);
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────

  if (authLoading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh', background: '#0f1117', color: '#6b7280', fontFamily: 'Inter, sans-serif' }}>
        Loading…
      </div>
    );
  }

  if (!user) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh', background: '#0f1117', color: '#ef4444', fontFamily: 'Inter, sans-serif' }}>
        You must be signed in to access timesheets.
      </div>
    );
  }

  const weekLabel = `Week of ${weekStart.toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' })}`;

  return (
    <div className="timesheet-page">
      <style>{`
        .timesheet-page {
          font-family: 'Inter', 'Segoe UI', sans-serif;
          background: #0f1117;
          min-height: 100vh;
          padding: 2rem;
          color: #e2e8f0;
        }
        .ts-header {
          margin-bottom: 1.5rem;
        }
        .ts-header h1 {
          font-size: 1.6rem;
          font-weight: 700;
          color: #fff;
          margin: 0 0 0.25rem;
        }
        .ts-header .week-label {
          font-size: 0.85rem;
          color: #6b7280;
        }
        .ts-card {
          background: #1a1d27;
          border: 1px solid #2d3149;
          border-radius: 12px;
          padding: 1.5rem;
          overflow-x: auto;
        }
        table {
          width: 100%;
          border-collapse: collapse;
          min-width: 860px;
        }
        th {
          text-align: center;
          padding: 0.6rem 0.5rem;
          font-size: 0.75rem;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #9ca3af;
          border-bottom: 1px solid #2d3149;
        }
        th.col-project { text-align: left; }
        th.weekend { color: #6b7280; }
        td {
          padding: 0.5rem 0.4rem;
          border-bottom: 1px solid #1e2235;
          vertical-align: middle;
        }
        td.day-total {
          text-align: center;
          font-size: 0.7rem;
          font-weight: 600;
          padding: 0.4rem;
          border-top: 1px solid #2d3149;
        }
        td.day-total.over { color: #ef4444; }
        td.day-total.warn { color: #f59e0b; }
        td.day-total.ok { color: #4ade80; }
        select, input[type='number'] {
          background: #0f1117;
          border: 1px solid #2d3149;
          border-radius: 6px;
          color: #e2e8f0;
          font-size: 0.85rem;
          padding: 0.35rem 0.5rem;
          outline: none;
          transition: border-color 0.15s;
        }
        select:focus, input[type='number']:focus {
          border-color: #6366f1;
        }
        select { width: 100%; }
        input[type='number'] {
          width: 56px;
          text-align: center;
        }
        input[type='number'].over { border-color: #ef4444; background: #1f0a0a; }
        .billable-badge {
          display: inline-flex;
          align-items: center;
          gap: 0.4rem;
          font-size: 0.75rem;
          cursor: pointer;
          user-select: none;
        }
        .billable-badge input { width: auto; }
        .remove-btn {
          background: transparent;
          border: none;
          color: #ef4444;
          cursor: pointer;
          padding: 0.25rem;
          border-radius: 4px;
          font-size: 1rem;
          transition: background 0.15s;
        }
        .remove-btn:hover { background: #1f0a0a; }
        .add-row-btn {
          margin-top: 1rem;
          background: transparent;
          border: 1px dashed #2d3149;
          border-radius: 8px;
          color: #6366f1;
          font-size: 0.85rem;
          padding: 0.5rem 1rem;
          cursor: pointer;
          transition: all 0.15s;
          width: 100%;
        }
        .add-row-btn:hover {
          border-color: #6366f1;
          background: #1a1d27;
        }
        .actions {
          display: flex;
          gap: 1rem;
          margin-top: 1.25rem;
          align-items: center;
        }
        .submit-btn {
          background: linear-gradient(135deg, #6366f1, #4f46e5);
          color: #fff;
          border: none;
          border-radius: 8px;
          padding: 0.65rem 1.5rem;
          font-size: 0.9rem;
          font-weight: 600;
          cursor: pointer;
          transition: opacity 0.15s;
        }
        .submit-btn:disabled { opacity: 0.5; cursor: not-allowed; }
        .submit-btn:hover:not(:disabled) { opacity: 0.9; }
        .error-list {
          background: #1c0a0a;
          border: 1px solid #ef4444;
          border-radius: 8px;
          padding: 0.75rem 1rem;
          margin-top: 1rem;
        }
        .error-list li {
          font-size: 0.82rem;
          color: #fca5a5;
          margin: 0.2rem 0;
        }
        .status-ok { color: #4ade80; font-size: 0.85rem; }
        .status-err { color: #ef4444; font-size: 0.85rem; }
        .weekend-col { opacity: 0.6; }
        .totals-row td { background: #15172080; }
      `}</style>

      {/* Header */}
      <div className="ts-header">
        <h1>Weekly Timesheet</h1>
        <div className="week-label">{weekLabel}</div>
      </div>

      {/* Grid card */}
      <div className="ts-card">
        <table>
          <thead>
            <tr>
              <th className="col-project" style={{ width: '26%' }}>Project</th>
              <th style={{ width: '7%' }}>Billable</th>
              {days.map((d, i) => (
                <th key={i} className={d.isWeekend ? 'weekend' : ''}>
                  {d.label}
                  <br />
                  <span style={{ fontWeight: 400, fontSize: '0.7rem' }}>{d.date}</span>
                </th>
              ))}
              <th style={{ width: '3%' }}></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                {/* Project selector */}
                <td>
                  <select
                    value={row.projectId}
                    onChange={e => updateRow(row.id, { projectId: e.target.value })}
                  >
                    <option value="">— Select project —</option>
                    {projects.map(p => (
                      <option key={p.id} value={p.id}>
                        {p.name}{p.billingType === 'NonBillable' ? ' (NB)' : ''}
                      </option>
                    ))}
                  </select>
                </td>

                {/* Billable toggle */}
                <td style={{ textAlign: 'center' }}>
                  <label className="billable-badge">
                    <input
                      type="checkbox"
                      checked={row.isBillable}
                      disabled={
                        !row.projectId ||
                        projects.find(p => p.id === row.projectId)?.billingType === 'NonBillable'
                      }
                      onChange={e => updateRow(row.id, { isBillable: e.target.checked })}
                    />
                    <span style={{ color: row.isBillable ? '#4ade80' : '#6b7280' }}>
                      {row.isBillable ? '₹' : '—'}
                    </span>
                  </label>
                </td>

                {/* Hour inputs */}
                {row.hours.map((h, di) => {
                  const dayTotal = totalForDay(rows, di);
                  return (
                    <td key={di} className={days[di]?.isWeekend ? 'weekend-col' : ''} style={{ textAlign: 'center' }}>
                      <input
                        type="number"
                        value={h === 0 ? '' : h}
                        placeholder="0"
                        min={0}
                        max={24}
                        step={0.5}
                        className={dayTotal > 24 ? 'over' : ''}
                        onChange={e => updateHours(row.id, di, e.target.value)}
                      />
                    </td>
                  );
                })}

                {/* Remove row */}
                <td>
                  <button
                    className="remove-btn"
                    onClick={() => removeRow(row.id)}
                    disabled={rows.length === 1}
                    title="Remove row"
                  >
                    ✕
                  </button>
                </td>
              </tr>
            ))}

            {/* Day totals row */}
            <tr className="totals-row">
              <td colSpan={2} style={{ fontSize: '0.72rem', color: '#6b7280', paddingLeft: '0.5rem' }}>
                Daily totals
              </td>
              {days.map((_, di) => {
                const total = totalForDay(rows, di);
                const cls = total > 24 ? 'over' : total > 8 ? 'warn' : 'ok';
                return (
                  <td key={di} className={`day-total ${total === 0 ? '' : cls}`}>
                    {total > 0 ? `${total}h` : '—'}
                  </td>
                );
              })}
              <td />
            </tr>
          </tbody>
        </table>

        {/* Add row */}
        <button className="add-row-btn" onClick={addRow}>
          + Add Project Row
        </button>

        {/* Validation errors */}
        {errors.length > 0 && (
          <ul className="error-list">
            {errors.map((e, i) => <li key={i}>{e}</li>)}
          </ul>
        )}

        {/* Actions */}
        <div className="actions">
          <button
            className="submit-btn"
            onClick={handleSubmit}
            disabled={isSubmitting || authLoading || !user}
          >
            {isSubmitting ? 'Submitting…' : 'Submit for Approval'}
          </button>

          {submitStatus === 'success' && (
            <span className="status-ok">✓ Timesheet submitted successfully.</span>
          )}
          {submitStatus === 'error' && (
            <span className="status-err">✗ Submission failed. Please try again.</span>
          )}
        </div>
      </div>
    </div>
  );
}
