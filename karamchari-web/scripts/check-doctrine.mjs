#!/usr/bin/env node
// Doctrine compliance checks — design/DOCTRINE.md §9.
// Fails the build when a page violates Bindu, serif, motion, classification,
// or density-budget rules. Active entries in design/EXCEPTIONS.md may waive a
// specific rule for a specific surface until they expire.

import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = join(dirname(fileURLToPath(import.meta.url)), '..')
const pagesDir = join(root, 'src', 'pages')
const exceptionsPath = join(root, '..', 'design', 'EXCEPTIONS.md')
const routeMapPath = join(root, '..', 'design', 'archetypes', 'ROUTE-MAP.md')
const lawbookPath = join(root, '..', 'design', 'LAWBOOK.md')

// Mirrors ARCHETYPES in src/lib/doctrine.ts (design/archetypes/README.md). Frozen at twelve.
const ARCHETYPES = [
  'dashboard', 'registry', 'record-detail', 'approval', 'case', 'builder',
  'analytics', 'administration', 'timeline', 'intelligence', 'command-center', 'topology',
]

// Mirrors PERSONA_BUDGETS in src/lib/doctrine.ts.
const BUDGETS = {
  EMP: { signals: 3, risks: 1, narratives: 1, maps: 1 },
  MGR: { signals: 8, risks: 3, narratives: 1, maps: 1 },
  EXEC: { signals: 3, risks: 5, narratives: 1, maps: 1 },
  CMP: { signals: 1, risks: 1, narratives: 1, maps: 1 },
  SYS: { signals: 4, risks: 1, narratives: 1, maps: 1 },
}

// ---- Exceptions register ----------------------------------------------------
// Active exception line format (inside EXCEPTIONS.md "Active Exceptions" table):
// | EX-001 | <rule keyword> | <page file> | <owner> | YYYY-MM-DD | YYYY-MM-DD |
function loadExceptions() {
  if (!existsSync(exceptionsPath)) return []
  const out = []
  const today = new Date().toISOString().slice(0, 10)
  for (const line of readFileSync(exceptionsPath, 'utf8').split('\n')) {
    const m = line.match(/^\|\s*(EX-\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([\d-]+)\s*\|\s*([\d-]+)\s*\|/)
    if (!m) continue
    const [, id, rule, surface, owner, , expires] = m
    out.push({ id, rule, surface, owner, expires, expired: expires < today })
  }
  return out
}

const exceptions = loadExceptions()
const violations = []
const waived = []

// Archetype reuse metrics — capability should grow faster than unique UI code.
const archetypesUsed = new Set()
let pageCount = 0
let composedPages = 0

// Institutional surface coverage — portfolio view from the route map.
// Inventory totals come from the Distribution table; implemented screens are ● rows.
function loadRouteMap() {
  if (!existsSync(routeMapPath)) return null
  const text = readFileSync(routeMapPath, 'utf8')
  const inventory = {}
  for (const m of text.matchAll(/^\|\s*([\w-]+)\s*\|\s*(\d+)\s*\|/gm)) {
    if (ARCHETYPES.includes(m[1])) inventory[m[1]] = Number(m[2])
  }
  const implemented = {}
  let implementedTotal = 0
  for (const m of text.matchAll(/^\|[^|]*●[^|]*\|\s*([\w-]+)\s*\|/gm)) {
    if (!ARCHETYPES.includes(m[1])) continue
    implemented[m[1]] = (implemented[m[1]] ?? 0) + 1
    implementedTotal++
  }
  return { inventory, implemented, implementedTotal }
}

function report(file, ruleKeyword, message) {
  const ex = exceptions.find((e) => e.surface.includes(file) && e.rule.toLowerCase().includes(ruleKeyword))
  if (ex && !ex.expired) {
    waived.push(`${message}  [waived by ${ex.id}, expires ${ex.expires}]`)
  } else if (ex && ex.expired) {
    violations.push(`${message}  [exception ${ex.id} EXPIRED ${ex.expires} — re-approve or fix]`)
  } else {
    violations.push(message)
  }
}

for (const ex of exceptions.filter((e) => e.expired)) {
  violations.push(`EXCEPTIONS.md: ${ex.id} expired ${ex.expires} — remove or re-approve`)
}

// ---- Per-page checks --------------------------------------------------------
for (const file of readdirSync(pagesDir).filter((f) => f.endsWith('.tsx'))) {
  const path = join(pagesDir, file)
  const src = readFileSync(path, 'utf8')
  const lines = src.split('\n')
  pageCount++
  if (src.includes('@/components/institutional')) composedPages++

  // 1. Bindu rule: max one copper-filled action element per page.
  //    Status dots and borders are signals, not actions, and are exempt.
  //    Detection: bg-tamra-copper within a 6-line window of an opening <button>/<a>
  //    (attributes may span lines; `=>` arrows make simple tag-close detection unsafe).
  //    <BinduButton> from the institutional component library is a copper action
  //    by construction and counts directly.
  let copperActions = (src.match(/<BinduButton[\s/>]/g) ?? []).length
  let window = 0
  for (const line of lines) {
    if (/<(button|a)(\s|>|$)/.test(line)) window = 6
    if (window > 0 && /bg-tamra-copper(?![-/])/.test(line)) {
      copperActions++
      window = 0
    }
    if (/<\/(button|a)>/.test(line)) window = 0
    if (window > 0) window--
  }
  if (copperActions > 1) {
    report(file, 'bindu', `${file}: ${copperActions} copper actions — Bindu rule allows 0 or 1 (DOCTRINE §3)`)
  }

  // 2. Serif containment: Newsreader never inside table markup.
  lines.forEach((line, i) => {
    if (/<(th|td)[\s>]/.test(line) && /(font-serif|font-section-title|font-pull-quote)/.test(line)) {
      report(file, 'serif', `${file}:${i + 1}: serif typography inside table cell (DOCTRINE §6)`)
    }
  })

  // 3. Motion rule: no bounce, spin, ping, or long custom durations.
  lines.forEach((line, i) => {
    const m = line.match(/animate-(bounce|spin|ping)|duration-(500|700|1000)/)
    if (m) report(file, 'motion', `${file}:${i + 1}: forbidden motion \`${m[0]}\` (DOCTRINE §5b)`)
  })

  // 4. Ambient breathing: max one animate-pulse per page.
  const pulses = (src.match(/animate-pulse/g) ?? []).length
  if (pulses > 1) {
    report(file, 'breathing', `${file}: ${pulses} animate-pulse — max one ambient indicator (DOCTRINE §5b)`)
  }

  // 5. Classification gate: declaration required, within budget, bindu consistent.
  const block = src.match(/export const PAGE_CLASSIFICATION[^=]*=\s*\{([\s\S]*?)\}/)
  if (!block) {
    report(file, 'classification', `${file}: missing PAGE_CLASSIFICATION export (DOCTRINE §4)`)
    continue
  }
  const body = block[1]
  const persona = body.match(/persona:\s*'(\w+)'/)?.[1]
  const archetype = body.match(/archetype:\s*'([\w-]+)'/)?.[1]
  const num = (k) => Number(body.match(new RegExp(`${k}:\\s*(\\d+)`))?.[1] ?? NaN)
  const declared = {
    signals: num('signals'),
    risks: num('risks'),
    narratives: num('narratives'),
    maps: num('maps'),
  }
  const bindu = body.match(/bindu:\s*(true|false)/)?.[1]

  if (!persona || !BUDGETS[persona] || Object.values(declared).some(Number.isNaN) || !bindu) {
    report(file, 'classification', `${file}: malformed PAGE_CLASSIFICATION (DOCTRINE §4)`)
    continue
  }
  if (!archetype) {
    report(file, 'archetype', `${file}: PAGE_CLASSIFICATION missing archetype (DOCTRINE §4b)`)
  } else if (ARCHETYPES.includes(archetype)) {
    archetypesUsed.add(archetype)
  } else {
    report(
      file,
      'archetype',
      `${file}: archetype '${archetype}' is not one of the twelve (design/archetypes/README.md; DOCTRINE §4b)`
    )
  }
  const budget = BUDGETS[persona]
  for (const k of Object.keys(budget)) {
    if (declared[k] > budget[k]) {
      report(file, 'density', `${file}: ${k} ${declared[k]} exceeds ${persona} budget ${budget[k]} (DOCTRINE §4)`)
    }
  }
  const expected = bindu === 'true' ? 1 : 0
  if (copperActions !== expected) {
    report(
      file,
      'bindu',
      `${file}: declared bindu=${bindu} but detected ${copperActions} copper action(s) (DOCTRINE §4)`
    )
  }
}

// ---- Lawbook citation check ---------------------------------------------------
// Every law marked web-enforced in design/LAWBOOK.md must be cited somewhere in
// src/. A registered law nobody cites is dead text.
let lawStats = null
if (existsSync(lawbookPath)) {
  const laws = []
  for (const line of readFileSync(lawbookPath, 'utf8').split('\n')) {
    const m = line.match(/^\|\s*([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+)\s*\|[^|]+\|[^|]+\|\s*([^|]*web[^|]*)\|/)
    if (m) laws.push(m[1])
  }
  const srcFiles = readdirSync(join(root, 'src'), { recursive: true })
    .filter((f) => /\.(tsx?|mjs)$/.test(String(f)))
    .map((f) => readFileSync(join(root, 'src', String(f)), 'utf8'))
  const allSrc = srcFiles.join('\n')
  const dead = laws.filter((id) => !allSrc.includes(id))
  for (const id of dead) {
    violations.push(`LAWBOOK.md: law ${id} is web-enforced but cited nowhere in src/ — dead law (design/LAWBOOK.md)`)
  }
  lawStats = { total: laws.length, cited: laws.length - dead.length }
}

// ---- Route map drift --------------------------------------------------------
// "No screen ships without a row here" (ROUTE-MAP.md). Every page must be marked
// implemented (●) in the route map, and vice versa.
const routeMap = loadRouteMap()
if (routeMap && routeMap.implementedTotal !== pageCount) {
  violations.push(
    `ROUTE-MAP.md drift: ${routeMap.implementedTotal} screens marked implemented (●) but ${pageCount} pages exist (design/archetypes/ROUTE-MAP.md)`
  )
}

// ---- Verdict ----------------------------------------------------------------
if (waived.length) {
  console.warn('Waived by active exceptions:\n' + waived.map((w) => '  ' + w).join('\n'))
}
if (violations.length) {
  console.error('DOCTRINE VIOLATIONS:\n' + violations.map((v) => '  ' + v).join('\n'))
  process.exit(1)
}
console.log(
  'Doctrine compliance: all pages pass (Bindu, serif, motion, breathing, classification, budgets, archetypes).'
)
const pct = pageCount ? Math.round((composedPages / pageCount) * 100) : 0
console.log(
  `Archetype reuse: ${pageCount} pages on ${archetypesUsed.size}/${ARCHETYPES.length} archetypes · ` +
    `${composedPages}/${pageCount} pages composed from institutional components (${pct}%).`
)
if (routeMap) {
  const inventoryTotal = Object.values(routeMap.inventory).reduce((a, b) => a + b, 0)
  const covPct = inventoryTotal ? ((routeMap.implementedTotal / inventoryTotal) * 100).toFixed(1) : '0'
  const breakdown = ARCHETYPES.filter((a) => routeMap.implemented[a])
    .map((a) => `${a} ${routeMap.implemented[a]}/${routeMap.inventory[a] ?? '?'}`)
    .join(' · ')
  console.log(`Surface coverage: ${routeMap.implementedTotal}/${inventoryTotal} inventory screens (${covPct}%).`)
  console.log(`  ${breakdown}`)
}
if (lawStats) {
  console.log(`Lawbook: ${lawStats.total} laws registered · ${lawStats.cited} cited on shipped surfaces.`)
}
