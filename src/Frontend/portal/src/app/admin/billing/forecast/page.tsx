'use client';

import React from 'react';
import { 
  TrendingUp, 
  ArrowUpRight,
  Database, 
  ShieldAlert,
  CalendarDays,
  Sparkles
} from 'lucide-react';

// Mock data for initial rendering
const mockForecast = {
  unbilledRevenue: '₹52,40,000',
  expectedCash30d: '₹1.08 Cr',
  atRiskAr: '₹15,20,000',
  confidenceScore: '88%',
  monthlyTrends: [
    { month: 'Apr', revenue: 1200000, cash: 1050000 },
    { month: 'May (Proj)', revenue: 1550000, cash: 1300000 },
    { month: 'Jun (Proj)', revenue: 1680000, cash: 1420000 },
  ]
};

export default function ForecastDashboard() {
  return (
    <div className="p-8 bg-slate-50 min-h-screen">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Revenue & Cash Forecast</h1>
          <p className="text-slate-500 text-sm">Predictive financial intelligence based on work-in-progress and client behavior.</p>
        </div>
        <div className="flex items-center text-xs font-semibold text-slate-400 bg-white px-3 py-1 rounded-full border border-slate-200 shadow-sm">
          <div className="w-2 h-2 bg-green-500 rounded-full mr-2"></div>
          Forecast Accurate to 88%
        </div>
      </div>

      {/* Main Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <StatCard 
          title="Unbilled Revenue" 
          value={mockForecast.unbilledRevenue} 
          subtitle="Work-in-progress" 
          icon={<Database className="w-5 h-5 text-blue-500" />} 
          color="border-blue-200"
        />
        <StatCard 
          title="Projected Cash (30d)" 
          value={mockForecast.expectedCash30d} 
          subtitle="Probability weighted" 
          icon={<ArrowUpRight className="w-5 h-5 text-green-500" />} 
          color="border-green-200"
        />
        <StatCard 
          title="Revenue at Risk" 
          value={mockForecast.atRiskAr} 
          subtitle="90+ Days / Low Prob" 
          icon={<ShieldAlert className="w-5 h-5 text-red-500" />} 
          color="border-red-200"
        />
        <StatCard 
          title="Avg. Collection Time" 
          value="42 Days" 
          subtitle="Client average" 
          icon={<CalendarDays className="w-5 h-5 text-purple-500" />} 
          color="border-purple-200"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Trend Chart Mockup */}
        <div className="lg:col-span-2 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <div className="flex justify-between items-center mb-6">
            <h3 className="font-bold text-slate-800">Forecast Trends</h3>
            <div className="flex space-x-4 text-xs font-medium">
              <div className="flex items-center"><div className="w-3 h-3 bg-blue-500 rounded-sm mr-1"></div> Revenue</div>
              <div className="flex items-center"><div className="w-3 h-3 bg-green-500 rounded-sm mr-1"></div> Cash</div>
            </div>
          </div>
          <div className="h-64 flex items-end space-x-8 px-4">
            {mockForecast.monthlyTrends.map((t, i) => (
              <div key={i} className="flex-1 flex flex-col items-center">
                <div className="w-full flex space-x-1 items-end justify-center mb-2">
                  <div className="w-8 bg-blue-500 rounded-t" style={{ height: `${(t.revenue / 2000000) * 100}%` }}></div>
                  <div className="w-8 bg-green-500 rounded-t" style={{ height: `${(t.cash / 2000000) * 100}%` }}></div>
                </div>
                <span className="text-[10px] font-bold text-slate-500 uppercase tracking-tighter">{t.month}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Insight Panel */}
        <div className="bg-slate-900 text-white p-6 rounded-2xl shadow-xl">
          <h3 className="font-bold mb-4 flex items-center text-yellow-400">
            <Sparkles className="w-5 h-5 mr-2" />
            AI CFO Insights
          </h3>
          <div className="space-y-6">
            <InsightItem 
              title="Underbilled Revenue" 
              desc="₹12L of work on Project Apollo is unbilled for >45 days." 
              action="Issue Invoice" 
            />
            <InsightItem 
              title="Collection Risk" 
              desc="Global Tech's payment behavior score dropped by 15%." 
              action="Follow Up" 
            />
            <InsightItem 
              title="Revenue Opportunity" 
              desc="Resource utilization is high, but revenue is flat. Suggest rate review." 
              action="Review Pricing" 
            />
          </div>
        </div>
      </div>
    </div>
  );
}

function StatCard({ title, value, subtitle, icon, color }) {
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

function InsightItem({ title, desc, action }) {
  return (
    <div className="p-4 bg-slate-800/50 rounded-xl border border-slate-700">
      <div className="text-xs font-bold text-yellow-400 uppercase mb-1">{title}</div>
      <div className="text-sm text-slate-300 mb-3">{desc}</div>
      <button className="text-[10px] font-bold bg-white text-slate-900 px-3 py-1 rounded hover:bg-yellow-400 transition">
        {action}
      </button>
    </div>
  );
}
