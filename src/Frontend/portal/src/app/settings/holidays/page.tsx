'use client';

import React, { useState } from 'react';
import { useHolidays, useAddHoliday } from '@/lib/hooks/use-time';
import { format, parseISO } from 'date-fns';

/**
 * Holiday Settings Page.
 * Allows HR Admins to view and configure the organization's holiday calendar.
 */
export default function HolidaySettingsPage() {
  const { data: holidays, isLoading } = useHolidays();
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight">Holiday Calendar</h1>
          <p className="text-slate-500 mt-1">Manage public and organizational holidays for the current year.</p>
        </div>
        <button
          onClick={() => setIsModalOpen(true)}
          className="px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700 transition-colors shadow-sm flex items-center gap-2"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add Holiday
        </button>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200">
                <th className="px-6 py-4 text-sm font-semibold text-slate-600">Holiday Name</th>
                <th className="px-6 py-4 text-sm font-semibold text-slate-600">Date</th>
                <th className="px-6 py-4 text-sm font-semibold text-slate-600 text-right">Day</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {holidays && holidays.length > 0 ? (
                holidays.map((holiday) => (
                  <tr key={holiday.id} className="hover:bg-slate-50 transition-colors group">
                    <td className="px-6 py-4">
                      <div className="font-medium text-slate-900">{holiday.name}</div>
                    </td>
                    <td className="px-6 py-4 text-slate-600">
                      {format(parseISO(holiday.date), 'MMMM dd, yyyy')}
                    </td>
                    <td className="px-6 py-4 text-slate-500 text-right">
                      {format(parseISO(holiday.date), 'EEEE')}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={3} className="px-6 py-12 text-center text-slate-500 italic">
                    No holidays configured for the current calendar.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {isModalOpen && (
        <AddHolidayModal onClose={() => setIsModalOpen(false)} />
      )}
    </div>
  );
}

/**
 * Modal component for adding a new holiday.
 */
function AddHolidayModal({ onClose }: { onClose: () => void }) {
  const [name, setName] = useState('');
  const [date, setDate] = useState('');
  const addHoliday = useAddHoliday();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !date) return;

    try {
      await addHoliday.mutateAsync({ name, date });
      onClose();
    } catch (err) {
      console.error(err);
      alert('Failed to add holiday. Please try again.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity" 
        onClick={onClose}
      />
      
      {/* Modal Content */}
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden transform transition-all border border-slate-200">
        <div className="px-6 py-4 border-b border-slate-100 flex justify-between items-center">
          <h3 className="text-lg font-semibold text-slate-900">Add New Holiday</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 transition-colors">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Holiday Name</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Independence Day"
              className="w-full px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Date</label>
            <input
              type="date"
              required
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all"
            />
          </div>

          <div className="pt-4 flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg font-medium hover:bg-slate-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={addHoliday.isPending}
              className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700 transition-colors shadow-sm disabled:opacity-50"
            >
              {addHoliday.isPending ? 'Adding...' : 'Add Holiday'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
