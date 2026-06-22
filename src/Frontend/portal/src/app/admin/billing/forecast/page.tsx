'use client';

import React, { type ReactNode } from 'react';
import { TrendingUp, ArrowUpRight, ShieldAlert } from 'lucide-react';
import { useForecastSummary } from '@/lib/hooks/use-billing';

function formatInr(amount: number): string {
  if (amount >= 10_000_000) return `₹${(amount / 10_000_000).toFixed(2)} Cr`;
  if (amount >= 100_000)    return `₹${(amount / 100_000).toFixed(1)} L`;
  return `₹${amount.toLocaleString('en-IN')}`;
}

function kpiVal(isLoading: boolean, isError: boolean, value: string): string {
  if (isLoading) return '…';
  if (isError)   return '—';
  return value;
}

export default function ForecastDashboard() {
  const forecast = useForecastSummary();
  const f = forecast.data;

  const maxTrend = f?.trends.length
    ? Math.max(...f.trends.flatMap(t => [t.revenue, t.cash]))
    : 1;

  return (
    <div className="p-8 bg-slate-50 min-h-screen">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-slate-900">Revenue &amp; Cash Forecast</h1>
        <p className="text-slate-500 text-sm">
          Predictive financial intelligence based on work-in-progress and client behaviour.
        </p>
      </div>

      {/* 3 KPI cards — Confidence Score and Avg. Collection Time removed (no API source) */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <StatCard
          title="Unbilled Revenue"
          value={kpiVal(forecast.isLoading, forecast.isError, f ? formatInr(f.totalUnbilledRevenue) : '—')}
          subtitle="Work-in-progress"
          icon={<TrendingUp className="w-5 h-5 text-blue-500" />}
          color="border-blue-200"
        />
        <StatCard
          title="Projected Cash (30d)"
          value={kpiVal(forecast.isLoading, forecast.isError, f ? formatInr(f.expectedCashNext30Days) : '—')}
          subtitle="Probability weighted"
          icon={<ArrowUpRight className="w-5 h-5 text-green-500" />}
          color="border-green-200"
        />
        <StatCard
          title="High Risk AR"
          value={kpiVal(forecast.isLoading, forecast.isError, f ? formatInr(f.highRiskAR) : '—')}
          subtitle="90+ Days / Low probability"
          icon={<ShieldAlert className="w-5 h-5 text-red-500" />}
          color="border-red-200"
        />
      </div>

      {/* Trend Chart */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div className="flex justify-between items-center mb-6">
          <h3 className="font-bold text-slate-800">Forecast Trends</h3>
          <div className="flex space-x-4 text-xs font-medium">
            <div className="flex items-center">
              <div className="w-3 h-3 bg-blue-500 rounded-sm mr-1" />
              Revenue
            </div>
            <div className="flex items-center">
              <div className="w-3 h-3 bg-green-500 rounded-sm mr-1" />
              Cash
            </div>
          </div>
        </div>

        {forecast.isLoading ? (
          <div className="h-64 flex items-center justify-center">
            <div className="text-slate-400 text-sm">Loading trends…</div>
          </div>
        ) : forecast.isError ? (
          <div className="h-64 flex flex-col items-center justify-center gap-3">
            <p className="text-red-600 font-medium">Unable to load forecast data.</p>
            <p className="text-slate-500 text-sm">Please retry or check API availability.</p>
            <button
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
              onClick={() => forecast.refetch()}
            >
              Retry
            </button>
          </div>
        ) : (f?.trends.length ?? 0) === 0 ? (
          <div className="h-64 flex items-center justify-center text-slate-400 text-sm">
            No trend data available.
          </div>
        ) : (
          <div className="h-64 flex items-end space-x-8 px-4">
            {f!.trends.map((t, i) => (
              <div key={i} className="flex-1 flex flex-col items-center">
                <div className="w-full flex space-x-1 items-end justify-center mb-2">
                  <div
                    className="w-8 bg-blue-500 rounded-t"
                    style={{ height: `${(t.revenue / maxTrend) * 100}%` }}
                  />
                  <div
                    className="w-8 bg-green-500 rounded-t"
                    style={{ height: `${(t.cash / maxTrend) * 100}%` }}
                  />
                </div>
                <div className="text-center">
                  <span className="text-[10px] font-bold text-slate-500 uppercase tracking-tighter">
                    {t.month}
                  </span>
                  <div className="text-[9px] text-slate-400">{formatInr(t.revenue)}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

type StatCardProps = {
  title: string;
  value: string;
  subtitle: string;
  icon: ReactNode;
  color: string;
};

function StatCard({ title, value, subtitle, icon, color }: StatCardProps) {
  return (
    <div className={`bg-white p-5 rounded-2xl border ${color} shadow-sm transition hover:shadow-md cursor-default`}>
      <div className="flex items-center justify-between mb-3">
        <div className="p-2 bg-slate-50 rounded-xl">{icon}</div>
      </div>
      <div className="text-xl font-black text-slate-900">{value}</div>
      <div className="text-xs font-bold text-slate-400 uppercase tracking-wider">{title}</div>
      <div className="mt-2 text-[10px] text-slate-400 italic">{subtitle}</div>
    </div>
  );
}
