"use client";

import { useParams, useRouter } from "next/navigation";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusDot } from "@/components/ui/status-dot";
import {
  useAcceptOffer,
  useApproveOffer,
  useHireCandidate,
  useIssueOffer,
  useOffer,
} from "@/lib/hooks/use-recruitment";
import type { OfferStatus } from "@/lib/types/recruitment";
import type { ApiError } from "@/lib/api/errors";

const LIFECYCLE: { status: OfferStatus; label: string }[] = [
  { status: "Draft", label: "Draft" },
  { status: "Approved", label: "Approved" },
  { status: "Issued", label: "Extended" },
  { status: "Accepted", label: "Accepted" },
];

export default function OfferManagementPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id ?? "";
  const offerQuery = useOffer(id);

  return (
    <AppShell>
      <Topbar title="Offer Management" subtitle="Offer lifecycle is governed by the recruitment BFF.">
        <Button variant="secondary" size="sm" onClick={() => router.back()}>
          <span className="material-symbols-outlined text-[16px]">arrow_back</span>
          Back
        </Button>
      </Topbar>

      <section className="px-margin py-8">
        {offerQuery.isPending ? <OfferSkeleton /> : null}
        {offerQuery.isError ? <OfferError error={offerQuery.error as ApiError} /> : null}
        {offerQuery.isSuccess && offerQuery.data ? (
          <OfferConsole offer={offerQuery.data} />
        ) : null}
      </section>
    </AppShell>
  );
}

function OfferConsole({ offer }: { offer: NonNullable<ReturnType<typeof useOffer>["data"]> }) {
  const approve = useApproveOffer(offer.id);
  const issue = useIssueOffer(offer.id);
  const accept = useAcceptOffer(offer.id);
  const hire = useHireCandidate(offer.applicationId);

  const isTerminal = offer.status === "Declined" || offer.status === "Expired" || offer.status === "Rescinded";
  const currentStep = LIFECYCLE.findIndex((s) => s.status === offer.status);

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Offer {offer.id}</CardTitle>
          <p className="text-body text-on-surface-variant">
            Application <span className="font-mono text-mono">{offer.applicationId}</span>
          </p>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-4 md:grid-cols-4">
            <Metadata label="Status" value={offer.status} />
            <Metadata label="Base Salary" value={`${offer.baseSalary.toLocaleString()} ${offer.currency}`} />
            <Metadata label="Issued At" value={offer.issuedAt ? new Date(offer.issuedAt).toLocaleString() : "—"} />
            <Metadata label="Expires At" value={offer.expiresAt ? new Date(offer.expiresAt).toLocaleString() : "—"} />
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Lifecycle</CardTitle>
        </CardHeader>
        <CardContent>
          <ol className="flex flex-wrap items-center gap-3">
            {LIFECYCLE.map((step, idx) => {
              const reached = currentStep >= idx || (offer.status === "Accepted" && step.status === "Accepted");
              return (
                <li key={step.status} className="flex items-center gap-2">
                  <StatusDot
                    status={
                      idx === currentStep
                        ? "warning"
                        : reached
                          ? "active"
                          : "inactive"
                    }
                    label={step.label}
                  />
                  {idx < LIFECYCLE.length - 1 ? (
                    <span className="font-mono text-mono text-on-surface-variant">→</span>
                  ) : null}
                </li>
              );
            })}
          </ol>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Actions</CardTitle>
          <p className="text-body text-on-surface-variant">
            Only the legal forward action for the current state is enabled.
          </p>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {offer.status === "Draft" ? (
              <Button
                variant="primary"
                size="md"
                disabled={approve.isPending}
                onClick={() => approve.mutate()}
              >
                {approve.isPending ? "Approving…" : "Approve Offer"}
              </Button>
            ) : null}

            {offer.status === "Approved" ? (
              <Button
                variant="primary"
                size="md"
                disabled={issue.isPending}
                onClick={() =>
                  issue.mutate({ expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString() })
                }
              >
                {issue.isPending ? "Extending…" : "Extend Offer"}
              </Button>
            ) : null}

            {offer.status === "Issued" ? (
              <Button variant="primary" size="md" disabled={accept.isPending} onClick={() => accept.mutate()}>
                {accept.isPending ? "Recording…" : "Record Acceptance"}
              </Button>
            ) : null}

            {offer.status === "Accepted" ? (
              <Button variant="primary" size="md" disabled={hire.isPending} onClick={() => hire.mutate()}>
                {hire.isPending ? "Onboarding…" : "Create Employee Identity"}
              </Button>
            ) : null}

            {isTerminal ? (
              <p className="text-body text-on-surface-variant">
                Offer is in a terminal state ({offer.status}). No further action is permitted on this offer.
              </p>
            ) : null}
          </div>

          {approve.isError ? <MutationError error={approve.error as ApiError} /> : null}
          {issue.isError ? <MutationError error={issue.error as ApiError} /> : null}
          {accept.isError ? <MutationError error={accept.error as ApiError} /> : null}
          {hire.isError ? <MutationError error={hire.error as ApiError} /> : null}
        </CardContent>
      </Card>
    </div>
  );
}

function Metadata({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-caps uppercase text-on-surface-variant">{label}</dt>
      <dd className="mt-1 text-body text-on-surface">{value}</dd>
    </div>
  );
}

function OfferSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <div className="h-32 animate-pulse rounded border bg-surface-container-high/30" />
      <div className="h-32 animate-pulse rounded border bg-surface-container-high/30" />
    </div>
  );
}

function OfferError({ error }: { error: ApiError | undefined }) {
  return (
    <div className="flex flex-col items-start gap-2 rounded border border-error/30 bg-error-container/10 p-4">
      <p className="text-body font-medium text-error">
        {error?.displayMessage ?? "Failed to load offer."}
      </p>
      {error?.status ? (
        <p className="font-mono text-mono text-on-surface-variant">
          HTTP {error.status}
          {error.reason ? ` · ${error.reason}` : ""}
        </p>
      ) : null}
    </div>
  );
}

function MutationError({ error }: { error: ApiError | undefined }) {
  if (!error) return null;
  return (
    <p className="mt-3 font-mono text-mono text-error">
      {error.displayMessage}
      {error.status ? ` (HTTP ${error.status})` : ""}
    </p>
  );
}