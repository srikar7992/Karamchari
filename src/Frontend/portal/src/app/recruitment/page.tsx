"use client";

import { useRouter } from "next/navigation";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { StatusDot } from "@/components/ui/status-dot";
import { usePipeline } from "@/lib/hooks/use-recruitment";
import type { PipelineCandidateDto, PipelineStageDto } from "@/lib/types/recruitment";
import type { ApiError } from "@/lib/api/errors";

const STAGE_LABELS: Record<string, string> = {
  New: "Applied",
  Screening: "Screening",
  Interviewing: "Interview",
  Offered: "Offer",
  Hired: "Hired",
};

export default function RecruitmentPipelinePage() {
  const router = useRouter();
  const pipelineQuery = usePipeline();

  return (
    <AppShell>
      <Topbar
        title="Candidate Pipeline"
        subtitle="Live view across every active application. Stage counts come from the recruitment BFF."
      >
        <Button variant="secondary" size="sm">
          <span className="material-symbols-outlined text-[16px]">filter_list</span>
          Filter
        </Button>
      </Topbar>

      <section className="px-margin py-8">
        {pipelineQuery.isPending ? <PipelineSkeleton /> : null}

        {pipelineQuery.isError ? (
          <PipelineError error={pipelineQuery.error as ApiError} />
        ) : null}

        {pipelineQuery.isSuccess ? (
          <PipelineBoard
            stages={pipelineQuery.data.stages}
            onOpenCandidate={(id) => router.push(`/recruitment/${id}`)}
          />
        ) : null}
      </section>
    </AppShell>
  );
}

function PipelineBoard({
  stages,
  onOpenCandidate,
}: {
  stages: PipelineStageDto[];
  onOpenCandidate: (candidateId: string) => void;
}) {
  if (stages.length === 0) {
    return (
      <div className="rounded border bg-surface-container-low p-12 text-center">
        <p className="text-body text-on-surface-variant">
          Pipeline is empty. Create candidates and applications to populate stages.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
      {stages.map((stage) => (
        <PipelineColumn key={stage.stage} stage={stage} onOpenCandidate={onOpenCandidate} />
      ))}
    </div>
  );
}

function PipelineColumn({
  stage,
  onOpenCandidate,
}: {
  stage: PipelineStageDto;
  onOpenCandidate: (candidateId: string) => void;
}) {
  const label = STAGE_LABELS[stage.stage] ?? stage.stage;
  return (
    <div className="flex flex-col rounded border bg-surface-container-low">
      <header className="flex items-center justify-between border-b px-3 py-2">
        <span className="text-caps uppercase text-on-surface">{label}</span>
        <span className="font-mono text-mono text-on-surface-variant">{stage.cards.length}</span>
      </header>

      <div className="flex flex-col gap-2 p-3">
        {stage.cards.length === 0 ? (
          <p className="py-6 text-center text-sm text-on-surface-variant">—</p>
        ) : (
          stage.cards.map((card) => (
            <PipelineCard
              key={card.applicationId}
              card={card}
              onOpen={() => onOpenCandidate(card.candidateId)}
            />
          ))
        )}
      </div>
    </div>
  );
}

function PipelineCard({ card, onOpen }: { card: PipelineCandidateDto; onOpen: () => void }) {
  return (
    <button
      type="button"
      onClick={onOpen}
      className="flex flex-col items-start gap-1 rounded border bg-surface px-3 py-2 text-left transition-colors hover:bg-surface-container"
    >
      <div className="flex w-full items-center justify-between">
        <span className="text-body font-medium text-on-surface">{card.candidateName}</span>
      </div>
      {card.email ? <span className="text-sm text-on-surface-variant">{card.email}</span> : null}
      <div className="mt-1 flex w-full items-center justify-between">
        <StatusDot status="active" label={STAGE_LABELS[card.stage] ?? card.stage} />
        <span className="font-mono text-mono text-on-surface-variant">
          {new Date(card.appliedAt).toLocaleDateString()}
        </span>
      </div>
    </button>
  );
}

function PipelineSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="h-64 animate-pulse rounded border bg-surface-container-high/30" />
      ))}
    </div>
  );
}

function PipelineError({ error }: { error: ApiError | undefined }) {
  return (
    <div className="flex flex-col items-start gap-2 rounded border border-error/30 bg-error-container/10 p-4">
      <p className="text-body font-medium text-error">
        {error?.displayMessage ?? "Failed to load recruitment pipeline."}
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