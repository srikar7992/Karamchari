'use client';

import React, { useState } from 'react';
import { 
  Banknote, 
  Clock, 
  TriangleAlert, 
  ShieldCheck,
  RefreshCcw,
  Mail,
  MessageSquare,
  Search
} from 'lucide-react';

// Mock data for initial rendering
const mockKpis = {
  totalOutstanding: '₹1.24 Cr',
  overdue30: '₹45.2 L',
  overdue90: '₹12.8 L',
  dso: '42 Days',
  collectionEfficiency: '92.4%'
};

const mockCases = [
  { id: '1', invoiceNo: 'INV-2026-001', client: 'Acme Corp', amount: '₹12,40,000', outstanding: '₹5,00,000', days: 34, stage: '30d', status: 'Active', lastAction: '2026-04-28' },
  { id: '2', invoiceNo: 'INV-2026-042', client: 'Global Tech', amount: '₹8,50,000', outstanding: '₹8,50,000', days: 92, stage: '90d', status: 'Escalated', lastAction: '2026-04-15' },
  { id: '3', invoiceNo: 'INV-2026-088', client: 'Zion Solutions', amount: '₹4,20,000', outstanding: '₹2,10,000', days: 12, stage: '7d', status: 'Active', lastAction: '2026-05-02' },
  { id: '4', invoiceNo: 'INV-2026-105', client: 'NexGen AI', amount: '₹15,00,000', outstanding: '₹15,00,000', days: 5, stage: 'Initial', status: 'Active', lastAction: '-' },
];

export default function CollectionsDashboard() {
  const [filter, setFilter] = useState('All');

  return (
    <div className="p-8 bg-gray-50 min-h-screen">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Collections Control Tower</h1>
          <p className="text-gray-500">Real-time cash flow and accounts receivable management.</p>
        </div>
        <div className="flex space-x-3">
          <button className="flex items-center px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50 transition">
            <RefreshCcw className="w-4 h-4 mr-2" />
            Sync Bank
          </button>
          <button className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-bold hover:bg-blue-700 shadow-sm transition">
            <Banknote className="w-4 h-4 mr-2" />
            Record Bulk Payment
          </button>
        </div>
      </div>

      {/* KPI Bar */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4 mb-8">
        <KpiCard title="Total Outstanding" value={mockKpis.totalOutstanding} icon={<Banknote className="w-6 h-6 text-blue-600" />} />
        <KpiCard title="30+ Days Overdue" value={mockKpis.overdue30} icon={<Clock className="w-6 h-6 text-yellow-600" />} color="bg-yellow-50" />
        <KpiCard title="90+ Days Critical" value={mockKpis.overdue90} icon={<TriangleAlert className="w-6 h-6 text-red-600" />} color="bg-red-50" />
        <KpiCard title="DSO (Avg)" value={mockKpis.dso} icon={<RefreshCcw className="w-6 h-6 text-purple-600" />} />
        <KpiCard title="Efficiency" value={mockKpis.collectionEfficiency} icon={<ShieldCheck className="w-6 h-6 text-green-600" />} />
      </div>

      {/* Grid */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div className="p-4 border-b border-gray-200 bg-gray-50 flex justify-between items-center">
          <div className="flex space-x-4">
            {['All', 'Active', 'Escalated', 'Disputed', 'Closed'].map((tab) => (
              <button 
                key={tab} 
                onClick={() => setFilter(tab)}
                className={`px-3 py-1 text-sm font-medium rounded-md transition ${filter === tab ? 'bg-white text-blue-600 shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
              >
                {tab}
              </button>
            ))}
          </div>
          <div className="relative">
            <input 
              type="text" 
              placeholder="Search client or invoice..." 
              className="pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none w-64"
            />
            <div className="absolute left-3 top-2.5">
              <Search className="w-4 h-4 text-gray-400" />
            </div>
          </div>
        </div>

        <table className="w-full text-left">
          <thead className="bg-gray-50 text-gray-500 text-xs uppercase font-bold tracking-wider">
            <tr>
              <th className="px-6 py-4">Invoice / Client</th>
              <th className="px-6 py-4">Outstanding</th>
              <th className="px-6 py-4">Days</th>
              <th className="px-6 py-4">Bucket</th>
              <th className="px-6 py-4">Status</th>
              <th className="px-6 py-4">Last Action</th>
              <th className="px-6 py-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {mockCases.map((c) => (
              <tr key={c.id} className="hover:bg-gray-50 transition cursor-pointer">
                <td className="px-6 py-4">
                  <div className="font-bold text-gray-900">{c.invoiceNo}</div>
                  <div className="text-xs text-gray-500">{c.client}</div>
                </td>
                <td className="px-6 py-4">
                  <div className="font-medium text-gray-900">{c.outstanding}</div>
                  <div className="text-xs text-gray-400">Total: {c.amount}</div>
                </td>
                <td className="px-6 py-4">
                  <div className={`text-sm font-bold ${c.days > 90 ? 'text-red-600' : c.days > 30 ? 'text-yellow-600' : 'text-gray-900'}`}>
                    {c.days} Days
                  </div>
                </td>
                <td className="px-6 py-4">
                  <span className={`px-2 py-1 rounded text-[10px] font-bold uppercase ${getBucketColor(c.stage)}`}>
                    {c.stage}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center">
                    <div className={`w-2 h-2 rounded-full mr-2 ${getStatusColor(c.status)}`}></div>
                    <span className="text-sm">{c.status}</span>
                  </div>
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">{c.lastAction}</td>
                <td className="px-6 py-4 text-right">
                  <div className="flex justify-end space-x-2">
                    <button className="p-2 hover:bg-blue-50 text-blue-600 rounded-lg transition" title="Send Reminder">
                      <Mail className="w-5 h-5" />
                    </button>
                    <button className="p-2 hover:bg-yellow-50 text-yellow-600 rounded-lg transition" title="Mark Disputed">
                      <MessageSquare className="w-5 h-5" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
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
        <div className="p-2 rounded-lg bg-white shadow-sm border border-gray-100">
          {icon}
        </div>
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
    case '7d': return 'bg-blue-100 text-blue-700';
    default: return 'bg-gray-100 text-gray-700';
  }
}

function getStatusColor(status: string) {
  switch (status) {
    case 'Active': return 'bg-green-500';
    case 'Escalated': return 'bg-orange-500';
    case 'Disputed': return 'bg-red-500';
    case 'Closed': return 'bg-gray-400';
    default: return 'bg-gray-300';
  }
}
