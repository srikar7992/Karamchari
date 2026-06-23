"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { StatusDot } from "@/components/ui/status-dot";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useCandidate, useCandidateTimeline } from "@/lib/hooks/use-recruitment";
import type { ApiError } from "@/lib/api/errors";

const STAGE_LABELS: Record<string, string> = {
  New: "Applied",
  Screening: "Screening",
  Interviewing: "Interview",
  Offered: "Offer",
  Hired: "Hired",
};

export default function CandidateDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id ?? "";
  const candidateQuery = useCandidate(id);
  const timelineQuery = useCandidateTimeline(id);

  return (
    <AppShell>
      <Topbar title="Candidate Detail" subtitle="Profile, applications, interviews, offers and audit timeline.">
        <Button variant="secondary" size="sm" onClick={() => router.push("/recruitment")}>
          <span className="material-symbols-outlined text-[16px]">arrow_back</span>
          Back to Pipeline
        </Button>
      </Topbar>

      <section className="px-margin py-8">
        {candidateQuery.isPending ? <DetailSkeleton /> : null}
        {candidateQuery.isError ? (
          <ErrorBlock error={candidateQuery.error as ApiError} />
        ) : null}
        {candidateQuery.isSuccess && candidateQuery.data ? (
          <CandidateProfileView candidate={candidateQuery.data} timeline={timelineQuery.data ?? []} timelinePending={timelineQuery.isPending} />
        ) : null}
      </section>
    </AppShell>
  );
}

function CandidateProfileView({
  candidate,
  timeline,
  timelinePending,
}: {
  candidate: NonNullable<ReturnType<typeof useCandidate>["data"]>;
  timeline: import("@/lib/types/recruitment").TimelineEntryDto[];
  timelinePending: boolean;
}) {
  const router = useRouter();
  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>
            {candidate.firstName} {candidate.lastName}
          </CardTitle>
          <p className="text-body text-on-surface-variant">{candidate.email}</p>
          {candidate.phoneNumber ? (
            <p className="text-sm text-on-surface-variant">{candidate.phoneNumber}</p>
          ) : null}
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-4 md:grid-cols-4">
            <Metadata label="Candidate ID" value={candidate.id} mono />
            <Metadata label="Profile version" value={String(candidate.profileVersion)} />
            <Metadata label="Created (UTC)" value={new Date(candidate.createdOnUtc).toLocaleString()} />
            <Metadata label="Applications" value={String(candidate.applications.length)} />
          </dl>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Applications</CardTitle>
          </CardHeader>
          <CardContent>
            {candidate.applications.length === 0 ? (
              <p className="text-body text-on-surface-variant">No applications yet.</p>
            ) : (
              <div className="rounded border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Requisition</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Applied</TableHead>
                      <TableHead>Hired By</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {candidate.applications.map((app) => (
                      <TableRow key={app.id}>
                        <TableCell className="font-mono text-mono text-on-surface-variant">{app.requisitionId}</TableCell>
                        <TableCell>
                          <StatusDot
                            status={app.status === "Hired" ? "active" : app.status === "Rejected" ? "error" : "warning"}
                            label={STAGE_LABELS[app.status] ?? app.status}
                          />
                        </TableCell>
                        <TableCell className="text-on-surface-variant">
                          {new Date(app.appliedAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell className="text-on-surface-variant">{app.hiredBy ?? "—"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Interviews</CardTitle>
          </CardHeader>
          <CardContent>
            {candidate.interviews.length === 0 ? (
              <p className="text-body text-on-surface-variant">No interviews scheduled.</p>
            ) : (
              <div className="rounded border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Scheduled</TableHead>
                      <TableHead>Duration</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Feedback</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {candidate.interviews.map((iv) => (
                      <TableRow key={iv.id}>
                        <TableCell className="text-on-surface-variant">
                          {new Date(iv.scheduledAt).toLocaleString()}
                        </TableCell>
                        <TableCell className="text-on-surface-variant">{iv.durationMinutes}m</TableCell>
                        <TableCell>
                          <StatusDot
                            status={iv.status === "Completed" ? "active" : iv.status === "Cancelled" ? "error" : "warning"}
                            label={iv.status}
                          />
                        </TableCell>
                        <TableCell className="text-on-surface-variant">{iv.feedback.length}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Offers</CardTitle>
          </CardHeader>
          <CardContent>
            {candidate.offers.length === 0 ? (
              <p className="text-body text-on-surface-variant">No offers yet.</p>
            ) : (
              <div className="rounded border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Status</TableHead>
                      <TableHead>Base Salary</TableHead>
                      <TableHead>Currency</TableHead>
                      <TableHead>Manage</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {candidate.offers.map((offer) => (
                      <TableRow key={offer.id}>
                        <TableCell>
                          <StatusDot
                            status={offer.status === "Accepted" ? "active" : offer.status === "Declined" ? "error" : "warning"}
                            label={offer.status}
                          />
                        </TableCell>
                        <TableCell className="text-on-surface-variant">
                          {offer.baseSalary.toLocaleString()}
                        </TableCell>
                        <TableCell className="text-on-surface-variant">{offer.currency}</TableCell>
                        <TableCell>
                          <Link
                            href={`/recruitment/offers/${offer.id}`}
                            className="text-on-surface underline-offset-2 hover:underline"
                          >
                            Open
                          </Link>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Audit Timeline</CardTitle>
          </CardHeader>
          <CardContent>
            {timelinePending ? (
              <div className="h-32 animate-pulse rounded bg-surface-container-high/30" />
            ) : timeline.length === 0 ? (
              <p className="text-body text-on-surface-variant">No audit entries yet.</p>
            ) : (
              <ol className="flex flex-col gap-3">
                {timeline.map((entry) => (
                  <li key={entry.id} className="flex flex-col gap-1 rounded border p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-caps uppercase text-on-surface">{entry.action}</span>
                      <span className="font-mono text-mono text-on-surface-variant">
                        {new Date(entry.timestamp).toLocaleString()}
                      </span>
                    </div>
                    <span className="text-sm text-on-surface-variant">
                      {entry.entityType} · {entry.entityId} {entry.oldState ? `· ${entry.oldState} → ${entry.newState}` : `· ${entry.newState}`}
                    </span>
                    <span className="text-sm text-on-surface-variant">by {entry.userId}</span>
                  </li>
                ))}
              </ol>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="flex gap-2">
        {candidate.offers.length > 0 ? (
          <Button
            variant="primary"
            size="md"
            onClick={() => router.push(`/recruitment/offers/${candidate.offers[0]?.id ?? ""}`)}
          >
            Open Offer Management
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function Metadata({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <dt className="text-caps uppercase text-on-surface-variant">{label}</dt>
      <dd className={mono ? "mt-1 font-mono text-mono text-on-surface" : "mt-1 text-body text-on-surface"}>
        {value}
      </dd>
    </div>
  );
}

function DetailSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <div className="h-32 animate-pulse rounded border bg-surface-container-high/30" />
      <div className="h-64 animate-pulse rounded border bg-surface-container-high/30" />
    </div>
  );
}

function ErrorBlock({ error }: { error: ApiError | undefined }) {
  return (
    <div className="flex flex-col items-start gap-2 rounded border border-error/30 bg-error-container/10 p-4">
      <p className="text-body font-medium text-error">
        {error?.displayMessage ?? "Failed to load candidate."}
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