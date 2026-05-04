'use client';

import React from 'react';
import { useForm, useFieldArray, useWatch } from 'react-hook-form';
import { format, addDays, parseISO } from 'date-fns';
import { 
  ChevronLeft, 
  ChevronRight, 
  Save, 
  Send, 
  Clock, 
  AlertCircle,
  CheckCircle2
} from 'lucide-react';
import { useCurrentTimesheet, useSubmitTimesheet } from '@/lib/hooks/use-timesheets';
import { TimesheetStatus } from '@/lib/types/time';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

type TimesheetFormValues = {
  entries: {
    date: string;
    hours: number;
    description: string;
  }[];
};

/**
 * Interactive Weekly Timesheet Page.
 * Provides a matrix-style interface for employees to log billable hours.
 */
export default function TimesheetPage() {
  const { data: timesheet, isLoading, error } = useCurrentTimesheet();
  const submitMutation = useSubmitTimesheet();

  const { register, control, handleSubmit, reset, formState: { isDirty } } = useForm<TimesheetFormValues>({
    defaultValues: {
      entries: [],
    },
  });

  const { fields } = useFieldArray({
    control,
    name: 'entries',
  });

  // Watch hours for live-summing "Total Hours" sticky footer
  const watchedEntries = useWatch({
    control,
    name: 'entries',
  });

  const totalHours = React.useMemo(() => {
    return (watchedEntries || []).reduce((sum, entry) => sum + (Number(entry.hours) || 0), 0);
  }, [watchedEntries]);

  // Sync form with data when it arrives from the BFF
  React.useEffect(() => {
    if (timesheet) {
      reset({
        entries: timesheet.entries.map(e => ({
          date: e.date,
          hours: e.hours,
          description: e.description || '',
        })),
      });
    }
  }, [timesheet, reset]);

  const onSubmit = (values: TimesheetFormValues) => {
    if (!timesheet) return;
    submitMutation.mutate({
      weekStartDate: timesheet.weekStartDate,
      entries: values.entries,
    });
  };

  if (isLoading) return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <div className="flex items-center gap-3 text-slate-400 animate-pulse">
            <Clock className="w-6 h-6" />
            <span className="text-xl font-medium">Initializing Workspace...</span>
        </div>
    </div>
  );
  
  if (error) return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-8">
        <div className="bg-red-500/10 border border-red-500/20 p-6 rounded-2xl max-w-md text-center">
            <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
            <h2 className="text-xl font-bold text-white mb-2">Sync Error</h2>
            <p className="text-slate-400">We couldn't retrieve your current timesheet. Please verify your connection and try again.</p>
        </div>
    </div>
  );

  const isLocked = timesheet?.status === TimesheetStatus.Submitted || timesheet?.status === TimesheetStatus.Approved;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-4 md:p-8">
        {/* Header Section */}
        <div className="max-w-7xl mx-auto mb-8 flex flex-col lg:flex-row lg:items-center justify-between gap-6">
            <div>
                <div className="flex items-center gap-3 mb-2">
                    <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center shadow-lg shadow-blue-500/20">
                        <Clock className="w-6 h-6 text-white" />
                    </div>
                    <h1 className="text-3xl font-black tracking-tight text-white">
                        Timesheets
                    </h1>
                </div>
                <p className="text-slate-400 font-medium flex items-center gap-2">
                    Period: 
                    <span className="text-slate-200">
                        {timesheet && `${format(parseISO(timesheet.weekStartDate), 'MMMM d')} - ${format(addDays(parseISO(timesheet.weekStartDate), 6), 'MMMM d, yyyy')}`}
                    </span>
                </p>
            </div>

            <div className="flex items-center gap-4">
                <div className="flex bg-slate-900 border border-slate-800 rounded-xl p-1">
                    <button className="p-2 hover:bg-slate-800 rounded-lg transition-all text-slate-400 hover:text-white">
                        <ChevronLeft className="w-5 h-5" />
                    </button>
                    <button className="px-4 py-2 text-sm font-bold text-slate-300 hover:text-white transition-colors">
                        Current Week
                    </button>
                    <button className="p-2 hover:bg-slate-800 rounded-lg transition-all text-slate-400 hover:text-white">
                        <ChevronRight className="w-5 h-5" />
                    </button>
                </div>
                
                <div className={cn(
                    "px-4 py-2 rounded-xl text-xs font-black uppercase tracking-[0.1em] border shadow-sm",
                    timesheet?.status === TimesheetStatus.Draft && "bg-blue-500/10 text-blue-400 border-blue-500/20",
                    timesheet?.status === TimesheetStatus.Submitted && "bg-amber-500/10 text-amber-400 border-amber-500/20",
                    timesheet?.status === TimesheetStatus.Approved && "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
                    timesheet?.status === TimesheetStatus.Rejected && "bg-red-500/10 text-red-400 border-red-500/20",
                )}>
                    {TimesheetStatus[timesheet?.status ?? 0]}
                </div>
            </div>
        </div>

        {/* Matrix Grid */}
        <form onSubmit={handleSubmit(onSubmit)} className="max-w-7xl mx-auto pb-32">
            <div className="grid grid-cols-1 md:grid-cols-4 lg:grid-cols-7 gap-5">
                {fields.map((field, index) => {
                    const date = parseISO(field.date);
                    const isWeekend = date.getDay() === 0 || date.getDay() === 6;
                    
                    return (
                        <div key={field.id} className={cn(
                            "group relative bg-slate-900/40 border border-slate-800/60 rounded-2xl p-5 transition-all duration-300 hover:border-blue-500/30 hover:bg-slate-900/60",
                            isWeekend && "bg-slate-950/40 opacity-70 grayscale-[0.5]"
                        )}>
                            <div className="mb-6 flex items-center justify-between">
                                <div>
                                    <span className="block text-[11px] uppercase font-black tracking-widest text-slate-500 mb-1">
                                        {format(date, 'EEEE')}
                                    </span>
                                    <span className="text-xl font-bold text-slate-200">
                                        {format(date, 'MMM d')}
                                    </span>
                                </div>
                                {isWeekend && <span className="text-[10px] bg-slate-800 text-slate-500 px-2 py-0.5 rounded font-bold uppercase">Weekend</span>}
                            </div>

                            <div className="space-y-4">
                                <div>
                                    <label className="text-[10px] text-slate-500 uppercase font-black mb-1.5 block">Hours Worked</label>
                                    <div className="relative group/input">
                                        <input
                                            type="number"
                                            step="0.5"
                                            disabled={isLocked}
                                            {...register(`entries.${index}.hours` as const, { valueAsNumber: true })}
                                            className="w-full bg-slate-950/50 border border-slate-800 rounded-xl px-4 py-3 text-2xl font-black text-white focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-all disabled:opacity-50 appearance-none"
                                            placeholder="0.0"
                                        />
                                        <div className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-600 font-bold text-xs pointer-events-none group-focus-within/input:text-blue-500/50 transition-colors">HRS</div>
                                    </div>
                                </div>
                                <div>
                                    <label className="text-[10px] text-slate-500 uppercase font-black mb-1.5 block">Work Description</label>
                                    <textarea
                                        rows={3}
                                        disabled={isLocked}
                                        {...register(`entries.${index}.description` as const)}
                                        className="w-full bg-slate-950/30 border border-slate-800 rounded-xl px-3 py-2 text-sm text-slate-400 placeholder:text-slate-700 focus:outline-none focus:ring-1 focus:ring-slate-700 transition-all resize-none disabled:opacity-50"
                                        placeholder="Specific tasks or project codes..."
                                    />
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Bottom Floating Dashboard */}
            <div className="fixed bottom-8 left-1/2 -translate-x-1/2 w-[calc(100%-2rem)] max-w-5xl z-50">
                <div className="bg-slate-900/80 backdrop-blur-2xl border border-slate-800 rounded-3xl p-6 shadow-[0_32px_64px_-12px_rgba(0,0,0,0.6)] flex flex-col md:flex-row items-center justify-between gap-8">
                    <div className="flex items-center gap-10">
                        <div className="flex flex-col">
                            <span className="text-[10px] text-slate-500 uppercase font-black tracking-[0.2em] mb-1">Weekly billable total</span>
                            <div className="flex items-baseline gap-2">
                                <span className={cn(
                                    "text-5xl font-black transition-all",
                                    totalHours > 0 ? "text-white" : "text-slate-700"
                                )}>
                                    {totalHours.toFixed(2)}
                                </span>
                                <span className="text-sm font-bold text-slate-500 uppercase">Hours</span>
                            </div>
                        </div>
                        
                        <div className="h-10 w-px bg-slate-800 hidden md:block" />

                        <div className="hidden sm:flex flex-col gap-2">
                            <div className="flex items-center gap-2 text-xs">
                                <div className={cn("w-2 h-2 rounded-full", totalHours >= 40 ? "bg-emerald-500" : "bg-blue-500")} />
                                <span className="text-slate-400 font-medium">{totalHours >= 40 ? 'Capacity Reached' : `${(40 - totalHours).toFixed(1)} hrs left for standard week`}</span>
                            </div>
                            <div className="w-48 h-2 bg-slate-800 rounded-full overflow-hidden">
                                <div 
                                    className={cn("h-full transition-all duration-500", totalHours >= 40 ? "bg-emerald-500" : "bg-blue-600")}
                                    style={{ width: `${Math.min((totalHours / 40) * 100, 100)}%` }}
                                />
                            </div>
                        </div>
                    </div>

                    <div className="flex items-center gap-3 w-full md:w-auto">
                        <button
                            type="button"
                            disabled={isLocked || !isDirty}
                            className="flex-1 md:flex-none flex items-center justify-center gap-2 px-6 py-4 rounded-2xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold transition-all disabled:opacity-20 active:scale-95 border border-slate-700/50"
                        >
                            <Save className="w-5 h-5" />
                            Save Draft
                        </button>
                        <button
                            type="submit"
                            disabled={isLocked || totalHours === 0}
                            className="flex-1 md:flex-none flex items-center justify-center gap-2 px-10 py-4 rounded-2xl bg-blue-600 hover:bg-blue-500 text-white font-black shadow-xl shadow-blue-600/20 transition-all disabled:opacity-20 active:scale-95 group"
                        >
                            {submitMutation.isPending ? (
                                <div className="w-6 h-6 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                            ) : (
                                <>
                                    <Send className="w-5 h-5 group-hover:translate-x-1 group-hover:-translate-y-1 transition-transform" />
                                    Submit Week
                                </>
                            )}
                        </button>
                    </div>
                </div>
                
                {submitMutation.isSuccess && (
                    <div className="absolute -top-12 left-1/2 -translate-x-1/2 flex items-center gap-2 text-emerald-400 font-bold bg-emerald-400/10 px-4 py-2 rounded-full border border-emerald-400/20 animate-in fade-in slide-in-from-bottom-2">
                        <CheckCircle2 className="w-4 h-4" />
                        Timesheet locked and sent for approval
                    </div>
                )}
            </div>
        </form>
    </div>
  );
}
