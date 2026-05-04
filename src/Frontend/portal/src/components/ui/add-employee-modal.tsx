"use client";

import { useState, useRef } from "react";
import { Button } from "./button";
import { useAnalyzeDocument, useOnboardEmployee } from "@/lib/hooks/use-employees";

export function AddEmployeeModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const analyzeDoc = useAnalyzeDocument();
  const onboardEmp = useOnboardEmployee();
  
  const [employeeNumber, setEmployeeNumber] = useState("");
  const [legalName, setLegalName] = useState("");
  const [workEmail, setWorkEmail] = useState("");
  const [hiredOn, setHiredOn] = useState(new Date().toISOString().split("T")[0] ?? "");

  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!isOpen) return null;

  const handleFileDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileUpload(e.dataTransfer.files[0]);
    }
  };

  const handleFileUpload = (file: File) => {
    analyzeDoc.mutate(file, {
      onSuccess: (data) => {
        setLegalName(data.legalName);
        // We could also set a DOB state if we wanted to display it
      },
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onboardEmp.mutate(
      { employeeNumber, legalName, workEmail, hiredOn },
      {
        onSuccess: () => {
          onClose();
        },
      }
    );
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="w-full max-w-md rounded-xl bg-surface p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-on-surface">Onboard Employee</h2>
          <button onClick={onClose} className="text-on-surface-variant hover:text-on-surface">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {/* Drag and Drop Zone */}
        <div
          className="mb-6 flex flex-col items-center justify-center rounded-lg border-2 border-dashed border-primary/50 bg-primary-container/10 p-6 text-center transition-colors hover:bg-primary-container/20"
          onDragOver={(e) => e.preventDefault()}
          onDrop={handleFileDrop}
          onClick={() => fileInputRef.current?.click()}
        >
          <span className="material-symbols-outlined mb-2 text-4xl text-primary">
            upload_file
          </span>
          <p className="text-sm font-medium text-on-surface">
            {analyzeDoc.isPending ? "Analyzing document..." : "Drag & drop ID Document here"}
          </p>
          <p className="text-xs text-on-surface-variant">
            or click to browse. Magic auto-fill powered by Azure AI.
          </p>
          <input
            type="file"
            ref={fileInputRef}
            className="hidden"
            accept="image/*,.pdf"
            onChange={(e) => {
              if (e.target.files?.[0]) handleFileUpload(e.target.files[0]);
            }}
          />
        </div>

        {analyzeDoc.isError && (
          <div className="mb-4 rounded bg-error-container/20 p-3 text-sm text-error">
            Failed to analyze document. Please enter details manually.
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-on-surface">Legal Name</label>
            <input
              type="text"
              required
              value={legalName}
              onChange={(e) => setLegalName(e.target.value)}
              className="w-full rounded border border-outline bg-surface-container-low px-3 py-2 text-on-surface focus:border-primary focus:outline-none"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-on-surface">Employee Number</label>
            <input
              type="text"
              required
              value={employeeNumber}
              onChange={(e) => setEmployeeNumber(e.target.value)}
              className="w-full rounded border border-outline bg-surface-container-low px-3 py-2 text-on-surface focus:border-primary focus:outline-none"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-on-surface">Work Email</label>
            <input
              type="email"
              value={workEmail}
              onChange={(e) => setWorkEmail(e.target.value)}
              className="w-full rounded border border-outline bg-surface-container-low px-3 py-2 text-on-surface focus:border-primary focus:outline-none"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-on-surface">Hired On</label>
            <input
              type="date"
              required
              value={hiredOn}
              onChange={(e) => setHiredOn(e.target.value)}
              className="w-full rounded border border-outline bg-surface-container-low px-3 py-2 text-on-surface focus:border-primary focus:outline-none"
            />
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={onClose} type="button">
              Cancel
            </Button>
            <Button variant="primary" type="submit" disabled={onboardEmp.isPending}>
              {onboardEmp.isPending ? "Onboarding..." : "Onboard"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
