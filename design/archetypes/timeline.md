# Archetype: Timeline

ID: `timeline`
Question: *What happened, in what order, by whose hand?*

The memory surface — Sutra made visible. A timeline asserts that nothing in the
institution happens without record. Its register is forensic: precise, attributed,
unemotional.

## Zones

1. **Scope header** — what history this is (entity, actor, or system-wide), range
   selector in mono.
2. **Query strip** — entity id / actor / event-type filters. Mono inputs.
3. **The trail** — CaseTimeline entries: ISO timestamp in mono, event tag chip,
   factual sentence with entity references as inline mono codes, severity expressed
   by hairline tone (forest/caution/rakta border), never by alarm color fills.
4. **Load strip** — "Load older entries", ink. History is paged calmly, not
   infinite-scrolled.

## Classification profile

Signals 0–1 (entry count), Risks 0–1 (flagged anomalies in range), Narratives 0,
Maps 0. The trail itself is the content.

## Bindu

Zero, almost always. History is read, not acted upon. Exception: evidence-export
("Seal Audit Extract") on compliance-facing trails.

## Component manifest

`CaseTimeline`, `LedgerStrip`, `InstitutionalTable` (dense ranges).

## Used by

Audit Trail Explorer, Approval Timeline, Employee History, Recruitment History,
Notification Center, Recognition feed.

## Anti-patterns

- Relative-only timestamps ("2h ago" alone). Forensic surfaces show ISO time;
  relative age may accompany it.
- Editorializing entries. "Multiple failed authentication attempts (5)" — count,
  source, target. No adjectives.
- Mutable history. Timeline entries never have edit affordances.
