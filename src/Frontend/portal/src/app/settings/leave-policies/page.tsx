'use client';

import React, { useState } from 'react';
import { useLeavePolicies, useAddLeavePolicy } from '@/lib/hooks/use-time';
import { LeaveCategory, AccrualFrequency } from '@/lib/types/time';

/**
 * Leave Policies Settings Page.
 * Allows HR Admins to build and manage leave types using the rules engine.
 */
export default function LeavePoliciesPage() {
  const { data: policies, isLoading } = useLeavePolicies();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  return (
    <div className="p-8 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex justify-between items-center mb-10">
        <div>
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight">Leave Policies</h1>
          <p className="text-slate-500 mt-1">Configure the rules engine for time-off accruals and approvals.</p>
        </div>
        <button
          onClick={() => setIsDrawerOpen(true)}
          className="px-5 py-2.5 bg-indigo-600 text-white rounded-xl font-semibold hover:bg-indigo-700 transition-all shadow-md flex items-center gap-2 active:scale-95"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Policy
        </button>
      </div>

      {/* Policy Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-48 bg-slate-100 rounded-3xl animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {policies?.map((policy) => (
            <div key={policy.id} className="bg-white border border-slate-200 rounded-3xl p-6 shadow-sm hover:shadow-md transition-shadow relative overflow-hidden group">
              <div className="flex justify-between items-start mb-4">
                <div className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${
                  policy.rules.category === LeaveCategory.Paid ? 'bg-emerald-100 text-emerald-700' :
                  policy.rules.category === LeaveCategory.Statutory ? 'bg-amber-100 text-amber-700' :
                  'bg-slate-100 text-slate-700'
                }`}>
                  {LeaveCategory[policy.rules.category]}
                </div>
                {policy.rules.requiresManagerApproval && (
                  <div title="Requires Manager Approval" className="text-slate-400">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                    </svg>
                  </div>
                )}
              </div>
              
              <h3 className="text-xl font-bold text-slate-900 mb-2">{policy.name}</h3>
              <p className="text-slate-500 text-sm line-clamp-2 mb-6">{policy.description}</p>
              
              <div className="flex items-end justify-between">
                <div>
                  <div className="text-3xl font-black text-indigo-600">{policy.rules.annualAllowance}</div>
                  <div className="text-xs font-bold text-slate-400 uppercase tracking-widest">Days / Year</div>
                </div>
                <div className="text-right">
                  <div className="text-sm font-semibold text-slate-900">
                    {AccrualFrequency[policy.rules.accrualFrequency]}
                  </div>
                  <div className="text-xs text-slate-400 uppercase tracking-widest">Accrual</div>
                </div>
              </div>
            </div>
          ))}
          {policies?.length === 0 && (
             <div className="col-span-full py-20 text-center bg-slate-50 rounded-3xl border-2 border-dashed border-slate-200">
                <p className="text-slate-400 font-medium italic">No leave policies configured yet.</p>
             </div>
          )}
        </div>
      )}

      {/* Slide-out Drawer */}
      {isDrawerOpen && (
        <PolicyBuilderDrawer onClose={() => setIsDrawerOpen(false)} />
      )}
    </div>
  );
}

/**
 * Slide-out drawer for building a new leave policy.
 */
function PolicyBuilderDrawer({ onClose }: { onClose: () => void }) {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    rules: {
      category: LeaveCategory.Paid,
      annualAllowance: 15,
      accrualFrequency: AccrualFrequency.Upfront,
      requiresManagerApproval: true,
      allowHalfDays: true,
      maximumCarryForward: 5,
    }
  });

  const addPolicy = useAddLeavePolicy();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await addPolicy.mutateAsync(formData);
      onClose();
    } catch (err) {
      console.error(err);
      alert('Failed to save policy');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity" onClick={onClose} />
      
      {/* Drawer Content */}
      <div className="relative w-full max-w-xl bg-white shadow-2xl h-full flex flex-col transform transition-transform animate-in slide-in-from-right duration-300">
        <div className="px-8 py-6 border-b border-slate-100 flex justify-between items-center bg-slate-50/50">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Policy Builder</h2>
            <p className="text-sm text-slate-500">Define the rules for a new leave type.</p>
          </div>
          <button onClick={onClose} className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-full transition-all">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-8 space-y-10">
          {/* Section: General */}
          <section className="space-y-6">
            <h3 className="text-xs font-bold text-indigo-600 uppercase tracking-widest flex items-center gap-2">
              <span className="w-6 h-px bg-indigo-600" /> General Information
            </h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Policy Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Wellness & Mental Health"
                  className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
                  value={formData.name}
                  onChange={e => setFormData({...formData, name: e.target.value})}
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Description</label>
                <textarea
                  rows={3}
                  placeholder="Describe when and how this leave can be used..."
                  className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all resize-none"
                  value={formData.description}
                  onChange={e => setFormData({...formData, description: e.target.value})}
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Category</label>
                  <select 
                    className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all appearance-none bg-white"
                    value={formData.rules.category}
                    onChange={e => setFormData({...formData, rules: {...formData.rules, category: Number(e.target.value)}})}
                  >
                    <option value={LeaveCategory.Paid}>Paid Leave</option>
                    <option value={LeaveCategory.Unpaid}>Unpaid Leave</option>
                    <option value={LeaveCategory.Statutory}>Statutory (Legal)</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Accrual Frequency</label>
                  <select 
                    className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all appearance-none bg-white"
                    value={formData.rules.accrualFrequency}
                    onChange={e => setFormData({...formData, rules: {...formData.rules, accrualFrequency: Number(e.target.value)}})}
                  >
                    <option value={AccrualFrequency.Upfront}>Upfront (Annual)</option>
                    <option value={AccrualFrequency.Monthly}>Monthly Accrual</option>
                    <option value={AccrualFrequency.HoursWorked}>Based on Hours</option>
                  </select>
                </div>
              </div>
            </div>
          </section>

          {/* Section: Allowances */}
          <section className="space-y-6">
            <h3 className="text-xs font-bold text-indigo-600 uppercase tracking-widest flex items-center gap-2">
              <span className="w-6 h-px bg-indigo-600" /> Accrual Rules
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Annual Allowance (Days)</label>
                <input
                  type="number"
                  className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
                  value={formData.rules.annualAllowance}
                  onChange={e => setFormData({...formData, rules: {...formData.rules, annualAllowance: Number(e.target.value)}})}
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Max Carry Forward (Days)</label>
                <input
                  type="number"
                  className="w-full px-4 py-3 border border-slate-200 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
                  value={formData.rules.maximumCarryForward}
                  onChange={e => setFormData({...formData, rules: {...formData.rules, maximumCarryForward: Number(e.target.value)}})}
                />
              </div>
            </div>
          </section>

          {/* Section: Workflow */}
          <section className="space-y-6">
            <h3 className="text-xs font-bold text-indigo-600 uppercase tracking-widest flex items-center gap-2">
              <span className="w-6 h-px bg-indigo-600" /> Workflow & Constraints
            </h3>
            <div className="space-y-4">
              <label className="flex items-center justify-between p-4 bg-slate-50 rounded-2xl cursor-pointer hover:bg-slate-100 transition-colors">
                <div>
                  <div className="font-semibold text-slate-900">Requires Approval</div>
                  <div className="text-xs text-slate-500">Manager must review every request.</div>
                </div>
                <input 
                  type="checkbox" 
                  className="w-6 h-6 rounded-lg text-indigo-600 border-slate-300 focus:ring-indigo-500 transition-all"
                  checked={formData.rules.requiresManagerApproval}
                  onChange={e => setFormData({...formData, rules: {...formData.rules, requiresManagerApproval: e.target.checked}})}
                />
              </label>
              <label className="flex items-center justify-between p-4 bg-slate-50 rounded-2xl cursor-pointer hover:bg-slate-100 transition-colors">
                <div>
                  <div className="font-semibold text-slate-900">Allow Half-Days</div>
                  <div className="text-xs text-slate-500">Employees can request 0.5 day increments.</div>
                </div>
                <input 
                  type="checkbox" 
                  className="w-6 h-6 rounded-lg text-indigo-600 border-slate-300 focus:ring-indigo-500 transition-all"
                  checked={formData.rules.allowHalfDays}
                  onChange={e => setFormData({...formData, rules: {...formData.rules, allowHalfDays: e.target.checked}})}
                />
              </label>
            </div>
          </section>
        </form>

        {/* Footer */}
        <div className="p-8 border-t border-slate-100 bg-slate-50/50 flex gap-4">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 px-6 py-3 border border-slate-200 text-slate-600 font-bold rounded-xl hover:bg-white transition-all active:scale-95"
          >
            Discard
          </button>
          <button
            onClick={handleSubmit}
            disabled={addPolicy.isPending}
            className="flex-1 px-6 py-3 bg-indigo-600 text-white font-bold rounded-xl hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-200 active:scale-95 disabled:opacity-50"
          >
            {addPolicy.isPending ? 'Saving...' : 'Save Policy'}
          </button>
        </div>
      </div>
    </div>
  );
}
