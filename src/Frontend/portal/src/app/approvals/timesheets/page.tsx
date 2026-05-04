'use client';

import React from 'react';
import { format, parseISO, addDays } from 'date-fns';
import { 
  Check, 
  X, 
  Clock, 
  User, 
  ChevronDown,
  ChevronUp,
  Inbox,
  AlertCircle
} from 'lucide-react';
import { usePendingTimesheets, useApproveTimesheet, useRejectTimesheet } from '@/lib/hooks/use-timesheets';
import { Timesheet } from '@/lib/types/time';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * High-efficiency Triage Inbox for Manager Timesheet Approvals.
 */
export default function TimesheetApprovalsPage() {
  const { data: timesheets, isLoading, error } = usePendingTimesheets();
  const approveMutation = useApproveTimesheet();
  const rejectMutation = useRejectTimesheet();

  if (isLoading) return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <div className="text-slate-500 animate-pulse font-medium">Scanning for pending reviews...</div>
    </div>
  );

  if (error) return (
    <div className="min-h-screen bg-slate-950 p-8 flex items-center justify-center">
        <div className="bg-red-500/10 border border-red-500/20 p-6 rounded-2xl text-center max-w-sm">
            <AlertCircle className="w-10 h-10 text-red-500 mx-auto mb-4" />
            <h2 className="text-white font-bold mb-1">Inbox Sync Failed</h2>
            <p className="text-slate-400 text-sm">We encountered an error while fetching the triage feed.</p>
        </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-4 md:p-8">
        <div className="max-w-4xl mx-auto">
            <header className="mb-12">
                <div className="flex items-center gap-3 mb-2">
                    <div className="bg-blue-600/20 p-2 rounded-lg">
                        <Inbox className="w-6 h-6 text-blue-500" />
                    </div>
                    <h1 className="text-3xl font-black text-white tracking-tight">Timesheet Triage</h1>
                </div>
                <p className="text-slate-400 font-medium">Review and finalize billable hours for your department.</p>
            </header>

            {timesheets?.length === 0 ? (
                <div className="bg-slate-900/40 border border-slate-800/60 rounded-[2rem] p-16 text-center">
                    <div className="w-20 h-20 bg-slate-800/50 rounded-full flex items-center justify-center mx-auto mb-6">
                        <Check className="w-10 h-10 text-slate-600" />
                    </div>
                    <h2 className="text-2xl font-bold text-slate-200 mb-2">Inbox Zero</h2>
                    <p className="text-slate-500 max-w-xs mx-auto text-sm leading-relaxed">
                        All timesheets have been processed. Great job keeping the workflow moving!
                    </p>
                </div>
            ) : (
                <div className="space-y-4">
                    {timesheets?.map((timesheet) => (
                        <TimesheetReviewCard 
                            key={timesheet.id}
                            timesheet={timesheet}
                            onApprove={() => approveMutation.mutate(timesheet.id)}
                            onReject={(reason) => rejectMutation.mutate({ id: timesheet.id, reason })}
                            isProcessing={approveMutation.isPending || rejectMutation.isPending}
                        />
                    ))}
                </div>
            )}
        </div>
    </div>
  );
}

function TimesheetReviewCard({ 
    timesheet, 
    onApprove, 
    onReject,
    isProcessing 
}: { 
    timesheet: Timesheet; 
    onApprove: () => void;
    onReject: (reason: string) => void;
    isProcessing: boolean;
}) {
    const [isExpanded, setIsExpanded] = React.useState(false);
    const weekStart = parseISO(timesheet.weekStartDate);
    const weekEnd = addDays(weekStart, 6);

    return (
        <div className="group bg-slate-900/40 border border-slate-800/60 rounded-3xl overflow-hidden transition-all duration-300 hover:border-slate-700 hover:bg-slate-900/60">
            {/* Main Content */}
            <div className="p-6 flex flex-col md:flex-row md:items-center justify-between gap-6">
                <div className="flex items-center gap-5">
                    <div className="w-14 h-14 rounded-2xl bg-slate-800 flex items-center justify-center border border-slate-700/50">
                        <User className="w-7 h-7 text-slate-400" />
                    </div>
                    <div>
                        <h3 className="text-lg font-bold text-white flex items-center gap-2">
                            Staff Member 
                            <span className="text-[10px] text-slate-600 font-mono tracking-tighter">#{timesheet.employeeId.slice(0, 8)}</span>
                        </h3>
                        <p className="text-sm text-slate-400 font-medium">
                            {format(weekStart, 'MMM d')} - {format(weekEnd, 'MMM d, yyyy')}
                        </p>
                    </div>
                </div>

                <div className="flex items-center gap-8">
                    <div className="text-right">
                        <span className="block text-[10px] uppercase font-black tracking-widest text-slate-600 mb-0.5">Total Billable</span>
                        <div className="flex items-baseline justify-end gap-1">
                            <span className="text-3xl font-black text-white">{timesheet.totalHours.toFixed(2)}</span>
                            <span className="text-xs font-bold text-slate-500 uppercase">HRS</span>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <button 
                            onClick={() => onReject('Hours mismatch')}
                            disabled={isProcessing}
                            className="w-12 h-12 rounded-2xl flex items-center justify-center text-red-400 hover:bg-red-500/10 transition-all border border-transparent hover:border-red-500/20 active:scale-95 disabled:opacity-30"
                            title="Reject"
                        >
                            <X className="w-6 h-6" />
                        </button>
                        <button 
                            onClick={onApprove}
                            disabled={isProcessing}
                            className="px-6 h-12 rounded-2xl bg-emerald-600 hover:bg-emerald-500 text-white font-black flex items-center gap-2 transition-all shadow-lg shadow-emerald-600/10 active:scale-95 disabled:opacity-30"
                        >
                            <Check className="w-5 h-5" />
                            Approve
                        </button>
                    </div>
                </div>
            </div>

            {/* Expandable Breakdown */}
            <div className="border-t border-slate-800/40">
                <button 
                    onClick={() => setIsExpanded(!isExpanded)}
                    className="w-full py-3 px-6 flex items-center justify-center gap-2 text-[10px] uppercase font-black tracking-[0.2em] text-slate-600 hover:text-slate-400 transition-colors"
                >
                    {isExpanded ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
                    {isExpanded ? 'Hide Daily Matrix' : 'View Daily Matrix'}
                </button>

                {isExpanded && (
                    <div className="px-6 pb-8 animate-in slide-in-from-top-2 duration-300">
                        <div className="grid grid-cols-7 gap-2">
                            {timesheet.entries.map((entry) => (
                                <div key={entry.date} className="bg-slate-950/40 rounded-xl p-3 border border-slate-800/40">
                                    <span className="block text-[9px] uppercase font-black text-slate-600 mb-1">
                                        {format(parseISO(entry.date), 'EEE')}
                                    </span>
                                    <span className={cn(
                                        "text-lg font-black",
                                        entry.hours > 0 ? "text-blue-400" : "text-slate-800"
                                    )}>
                                        {entry.hours || '0'}
                                    </span>
                                    {entry.description && (
                                        <div className="mt-2 text-[10px] text-slate-500 line-clamp-1 italic">
                                            "{entry.description}"
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
