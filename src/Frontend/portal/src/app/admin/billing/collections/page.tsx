'use client';

import React, { useState } from 'react';
import { Banknote, Clock, TriangleAlert, RefreshCcw, Mail, MessageSquare } from 'lucide-react';
import { useARSummary, useCollectionCases, useSendReminder, useMarkDisputed } from '@/lib/hooks/use-billing';
import type { CollectionCase } from '@/lib/types/billing';

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

const FILTERS = [
  { label: 'All',       value: undefined },
  { label: 'Active',    value: 'Active' },
  { label: 'Escalated', value: 'Escalated' },
  { label: 'Disputed',  value: 'Disputed' },
  { label: 'Closed',    value: 'Closed' },
] as const;

export default function CollectionsDashboard() {
  const [filter, setFilter] = useState<string | undefined>(undefined);

  const arSummary    = useARSummary();
  const cases        = useCollectionCases(filter);
  const sendReminder = useSendReminder();
  const markDisputed = useMarkDisputed();

  const ar = arSummary.data;

  return (
    <div className="p-8 bg-gray-50 min-h-screen">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Collections Control Tower</h1>
        <p className="text-gray-500">Real-time cash flow and accounts receivable management.</p>
      </div>

      {/* KPI Bar — 4 cards; Collection Efficiency removed (no API source) */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <KpiCard
          title="Total Outstanding"
          value={kpiVal(arSummary.isLoading, arSummary.isError, ar ? formatInr(ar.totalOutstanding) : '—')}
          icon={<Banknote className="w-6 h-6 text-blue-600" />}
        />
        <KpiCard
          title="30+ Days Overdue"
          value={kpiVal(arSummary.isLoading, arSummary.isError, ar ? formatInr(ar.overdue30) : '—')}
          icon={<Clock className="w-6 h-6 text-yellow-600" />}
          color="bg-yellow-50"
        />
        <KpiCard
          title="90+ Days Critical"
          value={kpiVal(
            arSummary.isLoading,
            arSummary.isError,
            ar ? formatInr((ar.overdue90 ?? 0) + (ar.overduePlus ?? 0)) : '—',
          )}
          icon={<TriangleAlert className="w-6 h-6 text-red-600" />}
          color="bg-red-50"
        />
        <KpiCard
          title="DSO (Days Sales Outstanding)"
          value={kpiVal(
            arSummary.isLoading,
            arSummary.isError,
            ar ? `${ar.daysSalesOutstanding.toFixed(1)} Days` : '—',
          )}
          icon={<RefreshCcw className="w-6 h-6 text-purple-600" />}
        />
      </div>

      {/* Cases table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div className="p-4 border-b border-gray-200 bg-gray-50">
          <div className="flex space-x-4">
            {FILTERS.map(({ label, value }) => (
              <button
                key={label}
                onClick={() => setFilter(value)}
                className={`px-3 py-1 text-sm font-medium rounded-md transition ${
                  filter === value
                    ? 'bg-white text-blue-600 shadow-sm'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>

        {cases.isLoading ? (
          <div className="p-8 space-y-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded bg-gray-100" />
            ))}
          </div>
        ) : cases.isError ? (
          <div className="p-8 text-center">
            <p className="text-red-600 font-medium">Unable to load collection cases.</p>
            <p className="text-gray-500 text-sm mt-1">Please retry or check API availability.</p>
            <button
              className="mt-3 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
              onClick={() => cases.refetch()}
            >
              Retry
            </button>
          </div>
        ) : (cases.data?.length ?? 0) === 0 ? (
          <div className="p-8 text-center text-gray-500">
            No collection cases{filter ? ` with status "${filter}"` : ''}.
          </div>
        ) : (
          <table className="w-full text-left">
            <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-bold tracking-wider">
              <tr>
                <th className="px-6 py-4">Invoice ID</th>
                <th className="px-6 py-4">Outstanding</th>
                <th className="px-6 py-4">Days</th>
                <th className="px-6 py-4">Bucket</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4">Last Action</th>
                <th className="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {cases.data?.map((c) => (
                <CaseRow
                  key={c.id}
                  c={c}
                  onRemind={() => sendReminder.mutate(c.id)}
                  onDispute={() => markDisputed.mutate(c.id)}
                  remindPending={sendReminder.isPending && sendReminder.variables === c.id}
                  disputePending={markDisputed.isPending && markDisputed.variables === c.id}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function CaseRow({
  c,
  onRemind,
  onDispute,
  remindPending,
  disputePending,
}: {
  c: CollectionCase;
  onRemind: () => void;
  onDispute: () => void;
  remindPending: boolean;
  disputePending: boolean;
}) {
  return (
    <tr className="hover:bg-gray-50 transition">
      <td className="px-6 py-4">
        <div className="font-mono text-sm font-bold text-gray-900">
          {c.invoiceId.slice(0, 8).toUpperCase()}
        </div>
        <div className="text-xs text-gray-400">Invoice ID</div>
      </td>
      <td className="px-6 py-4">
        <div className="font-medium text-gray-900">{formatInr(c.outstandingAmount)}</div>
      </td>
      <td className="px-6 py-4">
        <div
          className={`text-sm font-bold ${
            c.daysOutstanding > 90
              ? 'text-red-600'
              : c.daysOutstanding > 30
              ? 'text-yellow-600'
              : 'text-gray-900'
          }`}
        >
          {c.daysOutstanding} Days
        </div>
      </td>
      <td className="px-6 py-4">
        <span className={`px-2 py-1 rounded text-[10px] font-bold uppercase ${getBucketColor(c.currentStage)}`}>
          {c.currentStage}
        </span>
      </td>
      <td className="px-6 py-4">
        <div className="flex items-center">
          <div className={`w-2 h-2 rounded-full mr-2 ${getStatusDot(c.status)}`} />
          <span className="text-sm">{c.status}</span>
        </div>
      </td>
      <td className="px-6 py-4 text-sm text-gray-500">
        {c.lastActionAt ? new Date(c.lastActionAt).toLocaleDateString('en-IN') : '—'}
      </td>
      <td className="px-6 py-4 text-right">
        <div className="flex justify-end space-x-2">
          <button
            className="p-2 hover:bg-blue-50 text-blue-600 rounded-lg transition disabled:opacity-40"
            title="Send Reminder"
            onClick={onRemind}
            disabled={remindPending || c.status === 'Closed'}
          >
            <Mail className="w-5 h-5" />
          </button>
          <button
            className="p-2 hover:bg-yellow-50 text-yellow-600 rounded-lg transition disabled:opacity-40"
            title="Mark Disputed"
            onClick={onDispute}
            disabled={disputePending || c.status === 'Disputed' || c.status === 'Closed'}
          >
            <MessageSquare className="w-5 h-5" />
          </button>
        </div>
      </td>
    </tr>
  );
}

interface KpiCardProps {
  title: string;
  value: string;
  icon: React.ReactNode;
  color?: string;
}

function KpiCard({ title, value, icon, color = 'bg-white' }: KpiCardProps) {
  return (
    <div className={`${color} p-6 rounded-xl border border-gray-200 shadow-sm`}>
      <div className="flex items-center justify-between mb-4">
        <div className="p-2 rounded-lg bg-white shadow-sm border border-gray-100">{icon}</div>
      </div>
      <div className="text-2xl font-black text-gray-900">{value}</div>
      <div className="text-xs font-medium text-gray-500 uppercase tracking-wider">{title}</div>
    </div>
  );
}

function getBucketColor(stage: string) {
  switch (stage) {
    case '90d': return 'bg-red-100 text-red-700';
    case '60d': return 'bg-orange-100 text-orange-700';
    case '30d': return 'bg-yellow-100 text-yellow-700';
    case '7d':  return 'bg-blue-100 text-blue-700';
    default:    return 'bg-gray-100 text-gray-700';
  }
}

function getStatusDot(status: string) {
  switch (status) {
    case 'Active':    return 'bg-green-500';
    case 'Escalated': return 'bg-orange-500';
    case 'Disputed':  return 'bg-red-500';
    case 'Closed':    return 'bg-gray-400';
    default:          return 'bg-gray-300';
  }
}
