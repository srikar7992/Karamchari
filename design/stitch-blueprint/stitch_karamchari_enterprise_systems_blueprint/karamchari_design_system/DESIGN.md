---
name: Karamchari Design System
colors:
  surface: '#fcf8f8'
  surface-dim: '#ddd9d9'
  surface-bright: '#fcf8f8'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f7f3f2'
  surface-container: '#f1eded'
  surface-container-high: '#ebe7e7'
  surface-container-highest: '#e5e2e1'
  on-surface: '#1c1b1c'
  on-surface-variant: '#46464b'
  inverse-surface: '#313030'
  inverse-on-surface: '#f4f0ef'
  outline: '#77777b'
  outline-variant: '#c7c6cb'
  surface-tint: '#5e5e62'
  primary: '#000000'
  on-primary: '#ffffff'
  primary-container: '#1b1b1f'
  on-primary-container: '#848387'
  inverse-primary: '#c7c6ca'
  secondary: '#615e57'
  on-secondary: '#ffffff'
  secondary-container: '#e7e2d8'
  on-secondary-container: '#67645c'
  tertiary: '#000000'
  on-tertiary: '#ffffff'
  tertiary-container: '#1f1b16'
  on-tertiary-container: '#8a827c'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e3e2e6'
  primary-fixed-dim: '#c7c6ca'
  on-primary-fixed: '#1b1b1f'
  on-primary-fixed-variant: '#46464a'
  secondary-fixed: '#e7e2d8'
  secondary-fixed-dim: '#cac6bd'
  on-secondary-fixed: '#1d1c16'
  on-secondary-fixed-variant: '#494740'
  tertiary-fixed: '#ebe1d9'
  tertiary-fixed-dim: '#cec5be'
  on-tertiary-fixed: '#1f1b16'
  on-tertiary-fixed-variant: '#4c4640'
  background: '#fcf8f8'
  on-background: '#1c1b1c'
  surface-variant: '#e5e2e1'
  yantra-indigo: '#1B2A4E'
  sthira-forest: '#2D5D4B'
  tamra-copper: '#B16A3C'
  tamra-copper-bright: '#C68A60'
  hiranya-gold: '#C9A86A'
  graphite: '#2A2D33'
  cloud: '#D9D2C2'
  rakta-critical: '#7A2B2B'
  pita-caution: '#9C842A'
typography:
  display-xl:
    fontFamily: Newsreader
    fontSize: 124px
    fontWeight: '300'
    lineHeight: '0.94'
    letterSpacing: -0.035em
  section-title:
    fontFamily: Newsreader
    fontSize: 56px
    fontWeight: '300'
    lineHeight: '1.02'
    letterSpacing: -0.02em
  pull-quote:
    fontFamily: Newsreader
    fontSize: 32px
    fontWeight: '300'
    lineHeight: '1.2'
    letterSpacing: -0.015em
  body-standard:
    fontFamily: Geist
    fontSize: 15px
    fontWeight: '400'
    lineHeight: '1.55'
  tabular-data:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.4'
  mono-label:
    fontFamily: JetBrains Mono
    fontSize: 10.5px
    fontWeight: '500'
    lineHeight: '1.2'
    letterSpacing: 0.12em
  display-xl-mobile:
    fontFamily: Newsreader
    fontSize: 72px
    fontWeight: '300'
    lineHeight: '1.0'
spacing:
  unit: 8px
  page-margin: 64px
  section-padding: 96px
  gutter: 48px
  grid-12: 12px
---

<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Karamchari — Brand System & Identity Strategy</title>
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Newsreader:ital,opsz,wght@0,6..72,300;0,6..72,400;0,6..72,500;0,6..72,600;1,6..72,400&family=Geist:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap" />
<style>
  :root {
    --ivory: #F2EDE3;
    --ivory-2: #ECE6D9;
    --ivory-3: #E3DCCB;
    --ink: #0E0F12;
    --ink-2: #1A1C21;
    --graphite: #2A2D33;
    --indigo: #1B2A4E;
    --indigo-2: #2A3D6B;
    --copper: #B16A3C;
    --copper-2: #C68A60;
    --forest: #2D5D4B;
    --cloud: #C8C0AE;
    --hair: rgba(14,15,18,0.12);
    --hair-strong: rgba(14,15,18,0.32);
  }

  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; }
  body {
    background: var(--ivory);
    color: var(--ink);
    font-family: 'Geist', system-ui, sans-serif;
    font-weight: 400;
    font-size: 15px;
    line-height: 1.55;
    -webkit-font-smoothing: antialiased;
    text-rendering: optimizeLegibility;
  }

  .mono { font-family: 'JetBrains Mono', ui-monospace, monospace; font-size: 11px; letter-spacing: 0.04em; text-transform: uppercase; color: var(--graphite); }
  .serif { font-family: 'Newsreader', Georgia, serif; font-weight: 400; }
  .label { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--graphite); }
  .copper { color: var(--copper); }
  .indigo { color: var(--indigo); }
  .muted { color: #5C5F66; }

  /* Page rhythm */
  .page {
    max-width: 1280px;
    margin: 0 auto;
    padding: 0 64px;
    border-left: 1px solid var(--hair);
    border-right: 1px solid var(--hair);
  }
  .section {
    padding: 96px 0 96px 0;
    border-top: 1px solid var(--hair);
    position: relative;
  }
  .section:first-of-type { border-top: none; }
  .section-head {
    display: grid;
    grid-template-columns: 120px 1fr;
    gap: 48px;
    align-items: start;
    margin-bottom: 56px;
  }
  .section-no { font-family: 'JetBrains Mono', monospace; font-size: 11px; letter-spacing: 0.12em; color: var(--copper); text-transform: uppercase; padding-top: 8px; }
  .section-title {
    font-family: 'Newsreader', serif;
    font-weight: 300;
    font-size: 56px;
    line-height: 1.02;
    letter-spacing: -0.02em;
    margin: 0 0 16px 0;
    text-wrap: balance;
  }
  .section-kicker {
    font-family: 'Newsreader', serif;
    font-style: italic;
    font-weight: 400;
    font-size: 19px;
    line-height: 1.55;
    color: var(--graphite);
    max-width: 720px;
    text-wrap: pretty;
  }

  /* Cover */
  .cover {
    min-height: 92vh;
    display: grid;
    grid-template-rows: auto 1fr auto;
    padding: 48px 0 56px 0;
  }
  .cover-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-bottom: 32px;
    border-bottom: 1px solid var(--hair);
  }
  .cover-mid {
    display: grid;
    grid-template-columns: 1.2fr 1fr;
    gap: 64px;
    align-items: end;
    padding: 80px 0 64px 0;
  }
  .cover-title {
    font-family: 'Newsreader', serif;
    font-weight: 300;
    font-size: 124px;
    line-height: 0.94;
    letter-spacing: -0.035em;
    margin: 0;
  }
  .cover-title em { font-style: italic; font-weight: 400; color: var(--indigo); }
  .cover-sub {
    font-family: 'Newsreader', serif;
    font-style: italic;
    font-size: 22px;
    line-height: 1.5;
    color: var(--graphite);
    text-wrap: pretty;
  }
  .cover-bot {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 48px;
    padding-top: 32px;
    border-top: 1px solid var(--hair);
  }
  .cover-meta-k { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--graphite); text-transform: uppercase; margin-bottom: 6px; }
  .cover-meta-v { font-family: 'Newsreader', serif; font-size: 17px; color: var(--ink); }

  /* Two-column body */
  .two-col { display: grid; grid-template-columns: 120px 1fr 1fr; gap: 48px; }
  .body-col { font-size: 15px; line-height: 1.65; color: var(--ink-2); max-width: 540px; }
  .body-col p { margin: 0 0 14px 0; text-wrap: pretty; }
  .body-col p:last-child { margin-bottom: 0; }
  .pull-q { font-family: 'Newsreader', serif; font-style: italic; font-weight: 300; font-size: 32px; line-height: 1.2; color: var(--indigo); letter-spacing: -0.015em; text-wrap: balance; }

  /* Tables / spec rows */
  .spec-table { width: 100%; border-collapse: collapse; }
  .spec-table th, .spec-table td { text-align: left; padding: 14px 16px 14px 0; border-bottom: 1px solid var(--hair); font-size: 14px; vertical-align: top; }
  .spec-table th { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--graphite); text-transform: uppercase; font-weight: 500; width: 28%; }
  .spec-table td.serif { font-family: 'Newsreader', serif; font-size: 17px; line-height: 1.4; color: var(--ink); }

  /* Logo plate */
  .plates { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0; border-top: 1px solid var(--hair); border-left: 1px solid var(--hair); }
  .plate {
    border-right: 1px solid var(--hair);
    border-bottom: 1px solid var(--hair);
    padding: 36px 36px 32px 36px;
    background: var(--ivory);
    display: grid;
    grid-template-rows: auto 1fr auto;
    min-height: 520px;
    position: relative;
  }
  .plate.dark { background: var(--ink); color: var(--ivory); }
  .plate.dark .plate-name, .plate.dark .plate-no { color: var(--cloud); }
  .plate.dark .plate-meaning { color: rgba(242,237,227,0.7); }
  .plate.dark .plate-spec-k { color: rgba(242,237,227,0.5); }
  .plate.dark .plate-spec-v { color: var(--ivory); }
  .plate-head { display: flex; justify-content: space-between; align-items: baseline; }
  .plate-no { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--copper); text-transform: uppercase; }
  .plate-name { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.18em; text-transform: uppercase; color: var(--graphite); }
  .plate-body { display: flex; align-items: center; justify-content: center; padding: 32px 0 24px 0; position: relative; }
  .plate-foot { border-top: 1px solid var(--hair); padding-top: 18px; display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px; }
  .plate.dark .plate-foot { border-top-color: rgba(242,237,227,0.12); }
  .plate-spec-k { font-family: 'JetBrains Mono', monospace; font-size: 9.5px; letter-spacing: 0.12em; color: var(--graphite); text-transform: uppercase; margin-bottom: 4px; }
  .plate-spec-v { font-family: 'Newsreader', serif; font-size: 15px; color: var(--ink); }
  .plate-meaning { font-family: 'Newsreader', serif; font-style: italic; font-size: 15px; line-height: 1.5; color: var(--graphite); margin-top: 18px; text-wrap: pretty; }

  /* concept description block */
  .concept {
    display: grid;
    grid-template-columns: 280px 1fr;
    gap: 48px;
    padding: 48px 0;
    border-bottom: 1px solid var(--hair);
    align-items: start;
  }
  .concept:last-child { border-bottom: none; }
  .concept-id { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--copper); text-transform: uppercase; }
  .concept-name { font-family: 'Newsreader', serif; font-weight: 400; font-size: 30px; line-height: 1.1; letter-spacing: -0.015em; margin: 4px 0 4px 0; }
  .concept-trans { font-family: 'Newsreader', serif; font-style: italic; font-size: 16px; color: var(--graphite); }
  .concept-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 24px 40px;
  }
  .concept-grid h4 { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--copper); text-transform: uppercase; margin: 0 0 6px 0; font-weight: 500; }
  .concept-grid p { margin: 0; font-size: 14.5px; line-height: 1.6; color: var(--ink-2); text-wrap: pretty; }

  /* Construction plate */
  .construction-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; margin-top: 24px; }
  .ccard { border: 1px solid var(--hair); padding: 18px; background: var(--ivory); }
  .ccard-svg { aspect-ratio: 1 / 1; display: grid; place-items: center; }
  .ccard-cap { display: flex; justify-content: space-between; margin-top: 14px; }

  /* Type specimen */
  .type-spec { display: grid; grid-template-columns: 1fr; gap: 0; border-top: 1px solid var(--hair); }
  .type-row { display: grid; grid-template-columns: 120px 80px 1fr 200px; gap: 32px; align-items: baseline; padding: 28px 0; border-bottom: 1px solid var(--hair); }
  .type-name { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; letter-spacing: 0.12em; color: var(--copper); text-transform: uppercase; }
  .type-size { font-family: 'JetBrains Mono', monospace; font-size: 10.5px; color: var(--graphite); }
  .type-sample-serif { font-family: 'Newsreader', serif; line-height: 1.1; letter-spacing: -0.02em; }
  .type-sample-sans { font-family: 'Geist', sans-serif; line-height: 1.2; }
  .type-sample-mono { font-family: 'JetBrains Mono', monospace; line-height: 1.3; }
  .type-meta { font-size: 13px; color: var(--graphite); }

  /* Color */
  .swatches { display: grid; grid-template-columns: repeat(6, 1fr); gap: 0; border-top: 1px solid var(--hair); border-left: 1px solid var(--hair); }
  .sw { border-right: 1px solid var(--hair); border-bottom: 1px solid var(--hair); }
  .sw-chip { aspect-ratio: 1 / 1.1; position: relative; }
  .sw-meta { padding: 14px 16px 18px 16px; background: var(--ivory); }
  .sw-name { font-family: 'Newsreader', serif; font-size: 17px; color: var(--ink); margin-bottom: 2px; }
  .sw-hex { font-family: 'JetBrains Mono', monospace; font-size: 11px; color: var(--graphite); letter-spacing: 0.04em; }
  .sw-role { font-family: 'JetBrains Mono', monospace; font-size: 9.5px; letter-spacing: 0.14em; text-transform: uppercase; color: var(--copper); position: absolute; top: 14px; left: 14px; }
  .sw-chip.on-dark .sw-role { color: var(--copper-2); }

  /* Sub-brand cards */
  .subbrands { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0; border-top: 1px solid var(--hair); border-left: 1px solid var(--hair); }
  .sb { border-right: 1px solid var(--hair); border-bottom: 1px solid var(--hair); padding: 32px; }
  .sb-mark { height: 72px; display: flex; align-items: center; }
  .sb-name { font-family: 'Newsreader', serif; font-size: 28px; line-height: 1; margin: 22px 0 4px 0; letter-spacing: -0.015em; }
  .sb-sansk { font-family: 'Newsreader', serif; font-style: italic; font-size: 14px; color: var(--graphite); margin-bottom: 18px; }
  .sb-desc { font-size: 14px; line-height: 1.6; color: var(--ink-2); }

  /* UI mockups */
  .ui-grid { display: grid; grid-template-columns: 1.4fr 1fr; gap: 24px; }
  .ui-card { border: 1px solid var(--hair); background: var(--ivory); padding: 24px; }
  .ui-card.dark { background: var(--ink); color: var(--ivory); border-color: rgba(242,237,227,0.08); }
  .ui-card .ui-cap { display: flex; justify-content: space-between; font-family: 'JetBrains Mono', monospace; font-size: 10px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--graphite); margin-bottom: 16px; }
  .ui-card.dark .ui-cap { color: rgba(242,237,227,0.5); }

  /* Anti-patterns */
  .anti { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0; border-top: 1px solid var(--hair); border-left: 1px solid var(--hair); }
  .anti-cell { border-right: 1px solid var(--hair); border-bottom: 1px solid var(--hair); padding: 28px 32px; display: grid; grid-template-columns: 48px 1fr; gap: 20px; align-items: start; }
  .anti-mark { font-family: 'Newsreader', serif; font-size: 32px; line-height: 1; color: var(--copper); }
  .anti h4 { font-family: 'Newsreader', serif; font-weight: 400; font-size: 19px; margin: 0 0 6px 0; line-height: 1.25; }
  .anti p { margin: 0; font-size: 13.5px; line-height: 1.55; color: var(--ink-2); }

  /* Competitive grid */
  .compete { display: grid; grid-template-columns: 140px 1fr 1fr; gap: 32px; padding: 22px 0; border-bottom: 1px solid var(--hair); align-items: start; }
  .compete:first-child { border-top: 1px solid var(--hair); }
  .compete-name { font-family: 'Newsreader', serif; font-size: 22px; letter-spacing: -0.01em; }
  .compete h5 { font-family: 'JetBrains Mono', monospace; font-size: 10px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--copper); margin: 0 0 6px 0; }
  .compete p { margin: 0; font-size: 13.5px; line-height: 1.55; color: var(--ink-2); }

  /* Motion strip */
  .motion-strip { display: grid; grid-template-columns: repeat(5, 1fr); gap: 0; border: 1px solid var(--hair); }
  .motion-frame { aspect-ratio: 1 / 1; border-right: 1px solid var(--hair); display: grid; place-items: center; position: relative; }
  .motion-frame:last-child { border-right: none; }
  .motion-frame .mono { position: absolute; top: 12px; left: 14px; }

  /* Footer */
  .footer { padding: 64px 0 48px 0; border-top: 1px solid var(--hair); display: flex; justify-content: space-between; align-items: baseline; }
  .footer-l { font-family: 'Newsreader', serif; font-style: italic; font-size: 15px; color: var(--graphite); }

  /* Section: dark interlude */
  .dark-band {
    background: var(--ink);
    color: var(--ivory);
    margin: 0;
    padding: 96px 0;
    border: none;
  }
  .dark-band .section-title { color: var(--ivory); }
  .dark-band .section-kicker { color: rgba(242,237,227,0.7); }
  .dark-band .section-no { color: var(--copper-2); }
  .dark-band .body-col { color: rgba(242,237,227,0.78); }
  .dark-band .pull-q { color: var(--copper-2); }

  /* Hairline accents in plates */
  .geo-line { stroke: var(--hair); stroke-width: 1; fill: none; vector-effect: non-scaling-stroke; }
  .geo-dot { fill: var(--hair-strong); }
  .ink-fill { fill: var(--ink); }
  .ink-stroke { stroke: var(--ink); fill: none; vector-effect: non-scaling-stroke; }
  .ivory-fill { fill: var(--ivory); }
  .ivory-stroke { stroke: var(--ivory); fill: none; vector-effect: non-scaling-stroke; }
  .copper-fill { fill: var(--copper); }
  .copper-stroke { stroke: var(--copper); fill: none; vector-effect: non-scaling-stroke; }
  .indigo-fill { fill: var(--indigo); }
  .indigo-stroke { stroke: var(--indigo); fill: none; vector-effect: non-scaling-stroke; }

  /* corner ticks for plate body */
  .tick { stroke: var(--hair-strong); stroke-width: 1; }
  .plate.dark .tick { stroke: rgba(242,237,227,0.32); }

  /* responsive guardrails */
  @media (max-width: 980px) {
    .page { padding: 0 24px; }
    .cover-title { font-size: 72px; }
    .section-title { font-size: 36px; }
    .two-col, .cover-mid, .cover-bot, .plates, .subbrands, .anti, .ui-grid, .construction-row, .swatches, .compete, .type-row, .concept { grid-template-columns: 1fr; }
    .section-head { grid-template-columns: 1fr; gap: 12px; }
  }
</style>
</head>
<body>

<!-- ============================================================ -->
<!-- COVER                                                         -->
<!-- ============================================================ -->
<div class="page">
  <section class="cover" data-screen-label="01 Cover">
    <div class="cover-top">
      <div style="display:flex;align-items:center;gap:16px;">
        <!-- Primary mark: Sutra (continuous thread, sacred geometry abstraction) -->
        <svg width="34" height="34" viewBox="0 0 100 100" aria-hidden="true">
          <g class="ink-stroke" stroke-width="6.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 78 L50 22 L80 78 Z" />
            <circle cx="50" cy="58" r="3.5" class="copper-fill" stroke="none" />
          </g>
        </svg>
        <span class="mono">Karamchari / Brand System / v 1.0</span>
      </div>
      <span class="mono">Confidential · Internal Strategy · MMXXVI</span>
    </div>

    <div class="cover-mid">
      <div>
        <div class="mono copper" style="margin-bottom:28px;">Workforce Operating System · Identity &amp; Strategy</div>
        <h1 class="cover-title">Karam<em>chari</em>.</h1>
        <p style="font-family:'Newsreader',serif;font-size:24px;line-height:1.45;letter-spacing:-0.005em;margin:24px 0 0 0;max-width:560px;color:var(--graphite);text-wrap:balance;">
          Ancient organizational wisdom, rebuilt as
          <span class="indigo" style="font-style:italic;">operational intelligence</span>
          for the modern enterprise.
        </p>
      </div>
      <div>
        <p class="cover-sub">
          A brand system designed to scale from favicon to enterprise signage —
          rooted in the geometric grammar of yantras, the proportional rhythm of temple
          architecture, and the modular logic of distributed work.
        </p>
      </div>
    </div>

    <div class="cover-bot">
      <div><div class="cover-meta-k">Document</div><div class="cover-meta-v">Identity Manual</div></div>
      <div><div class="cover-meta-k">Scope</div><div class="cover-meta-v">Logo · Type · Color · System · Motion</div></div>
      <div><div class="cover-meta-k">Audience</div><div class="cover-meta-v">Founders · Board · Design</div></div>
      <div><div class="cover-meta-k">Status</div><div class="cover-meta-v copper">Strategic proposal</div></div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- 01  PHILOSOPHY                                                -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="02 Philosophy">
    <div class="section-head">
      <div class="section-no">01 — Philosophy</div>
      <div>
        <h2 class="section-title">A name that means <em style="font-style:italic;font-weight:300;">the one who does the work</em>.</h2>
        <p class="section-kicker">
          Karamchari (कर्मचारी) is built on a Sanskritic root older than most languages still in use —
          <em>karma</em>, action, and <em>chari</em>, one who carries out. The brand reclaims that
          word from its bureaucratic Hindi shadow and restores its original dignity: the
          intelligent operator of work.
        </p>
      </div>
    </div>

    <div class="two-col">
      <div></div>
      <div class="body-col">
        <p><strong>Core philosophy.</strong> Every enterprise is a system of <em>karma</em> — a network of actions, intentions, and consequences. Karamchari is the operating layer that gives that system structure, memory and intelligence. Not software for HR. The OS for organised human action.</p>
        <p><strong>Archetype.</strong> The Sage-Architect. A figure who builds with mathematical certainty and humane intent. Quiet authority; long thinking; the opposite of growth-hacker theatre.</p>
        <p><strong>Long-term narrative.</strong> Workforces shifted from paper, to spreadsheets, to ticketing systems, to disconnected SaaS. Karamchari is the consolidation event — a single substrate for people, processes and decisions, governed by rules an enterprise can actually trust.</p>
      </div>
      <div class="body-col">
        <p><strong>Emotional positioning.</strong> The relief of finally being understood by your tools. The respect of being treated as a thinking operator, not a row in a database.</p>
        <p><strong>Psychological perception.</strong> Premium without being precious. Intelligent without being intimidating. Indian-rooted without being decorative. Built to last decades, not quarters.</p>
        <p><strong>Brand personality.</strong> Composed. Geometrically minded. Quietly confident. Speaks in declarative sentences. Never apologises for being thorough.</p>
        <p><strong>Why it resonates.</strong> Because every founder, COO and people-leader has felt the cognitive tax of running a workforce across fifteen disconnected tools. Karamchari names that condition and ends it.</p>
      </div>
    </div>

    <div style="margin-top:80px;padding:48px 0;border-top:1px solid var(--hair);border-bottom:1px solid var(--hair);display:grid;grid-template-columns:120px 1fr;gap:48px;">
      <div class="mono copper">Manifesto pull</div>
      <div class="pull-q">
        “Work is the oldest technology. Karamchari is its newest interface — a system that remembers what every action means, and what every operator deserves.”
      </div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- 02  LOGO DIRECTIONS                                           -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="03 Logo Directions">
    <div class="section-head">
      <div class="section-no">02 — Logo Directions</div>
      <div>
        <h2 class="section-title">Six concepts. One grammar.</h2>
        <p class="section-kicker">
          Each direction is derived from a single sacred-geometry primitive — the
          <em>bindu</em>, the triangle, the lattice, the column, the curve, the seal — and
          drawn to enterprise tolerances. No motifs. No ornament. Only proportion.
        </p>
      </div>
    </div>

    <div class="plates">

      <!-- 01 SUTRA -->
      <div class="plate">
        <div class="plate-head"><span class="plate-no">01 / Sutra</span><span class="plate-name">सूत्र · The Thread</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Sutra mark">
            <!-- corner ticks -->
            <g class="tick">
              <line x1="10" y1="10" x2="24" y2="10" /><line x1="10" y1="10" x2="10" y2="24" />
              <line x1="350" y1="10" x2="336" y2="10" /><line x1="350" y1="10" x2="350" y2="24" />
              <line x1="10" y1="230" x2="24" y2="230" /><line x1="10" y1="230" x2="10" y2="216" />
              <line x1="350" y1="230" x2="336" y2="230" /><line x1="350" y1="230" x2="350" y2="216" />
            </g>
            <!-- construction circles (faint) -->
            <g class="geo-line">
              <circle cx="180" cy="120" r="80" />
              <circle cx="180" cy="120" r="40" />
            </g>
            <!-- the mark: three lines tied at a bindu, forming an open knot -->
            <g class="ink-stroke" stroke-width="9" stroke-linecap="round" stroke-linejoin="round">
              <path d="M120 170 L180 60 L240 170" />
              <path d="M140 130 L220 130" />
            </g>
            <circle cx="180" cy="130" r="7" class="copper-fill" />
            <!-- wordmark -->
            <text x="180" y="220" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" class="ink-fill">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">Triangle + thread</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">16 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">Wordmark lockup</div></div>
        </div>
        <p class="plate-meaning">A triangle, cut horizontally by a thread. The mountain of work, bound by the rule. The bindu marks where intent meets action.</p>
      </div>

      <!-- 02 BINDU -->
      <div class="plate dark">
        <div class="plate-head"><span class="plate-no">02 / Bindu</span><span class="plate-name">बिन्दु · The Point</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Bindu mark">
            <g style="stroke:rgba(242,237,227,0.2);fill:none;stroke-width:1;">
              <circle cx="180" cy="120" r="80" />
              <circle cx="180" cy="120" r="55" />
              <circle cx="180" cy="120" r="30" />
            </g>
            <!-- orbital dot ring -->
            <g class="ivory-fill">
              <circle cx="180" cy="40" r="3.5" />
              <circle cx="237" cy="63" r="3.5" />
              <circle cx="260" cy="120" r="3.5" />
              <circle cx="237" cy="177" r="3.5" />
              <circle cx="180" cy="200" r="3.5" />
              <circle cx="123" cy="177" r="3.5" />
              <circle cx="100" cy="120" r="3.5" />
              <circle cx="123" cy="63" r="3.5" />
            </g>
            <!-- inner ring (smaller) -->
            <g class="ivory-fill" opacity="0.55">
              <circle cx="180" cy="65" r="2" />
              <circle cx="225" cy="120" r="2" />
              <circle cx="180" cy="175" r="2" />
              <circle cx="135" cy="120" r="2" />
            </g>
            <!-- central bindu -->
            <circle cx="180" cy="120" r="11" class="copper-fill" />
            <text x="180" y="220" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" fill="#F2EDE3">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">Orbital lattice</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">20 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">App icon / favicon</div></div>
        </div>
        <p class="plate-meaning">A central operator, surrounded by the orbit of workers, processes and rules it conducts. The copper centre is the only ornament the system permits.</p>
      </div>

      <!-- 03 YANTRA-K -->
      <div class="plate">
        <div class="plate-head"><span class="plate-no">03 / Yantra-K</span><span class="plate-name">यन्त्र · The Seal</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Yantra mark">
            <!-- outer square (faint) -->
            <g class="geo-line">
              <rect x="100" y="40" width="160" height="160" />
              <line x1="100" y1="120" x2="260" y2="120" />
              <line x1="180" y1="40" x2="180" y2="200" />
            </g>
            <!-- two interlocked triangles, simplified -->
            <g class="ink-stroke" stroke-width="7" stroke-linejoin="round">
              <path d="M180 60 L246 174 L114 174 Z" />
            </g>
            <g class="indigo-stroke" stroke-width="7" stroke-linejoin="round">
              <path d="M180 180 L114 66 L246 66 Z" />
            </g>
            <!-- bindu -->
            <circle cx="180" cy="120" r="9" class="copper-fill" />
            <text x="180" y="220" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" class="ink-fill">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">Dual triangle</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">24 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">Seal / certification</div></div>
        </div>
        <p class="plate-meaning">Two triangles — agency descending, intelligence ascending — locked in the square of the enterprise. Reduced from yantra logic to four lines and a point.</p>
      </div>

      <!-- 04 LATTICE -->
      <div class="plate">
        <div class="plate-head"><span class="plate-no">04 / Lattice</span><span class="plate-name">जाल · The Network</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Lattice mark">
            <!-- 4x4 dot grid -->
            <g class="geo-dot">
              <!-- col 1 -->
              <circle cx="120" cy="60" r="3" />
              <circle cx="120" cy="100" r="3" />
              <circle cx="120" cy="140" r="3" />
              <circle cx="120" cy="180" r="3" />
              <!-- col 2 -->
              <circle cx="160" cy="60" r="3" />
              <circle cx="160" cy="100" r="3" />
              <circle cx="160" cy="140" r="3" />
              <circle cx="160" cy="180" r="3" />
              <!-- col 3 -->
              <circle cx="200" cy="60" r="3" />
              <circle cx="200" cy="100" r="3" />
              <circle cx="200" cy="140" r="3" />
              <circle cx="200" cy="180" r="3" />
              <!-- col 4 -->
              <circle cx="240" cy="60" r="3" />
              <circle cx="240" cy="100" r="3" />
              <circle cx="240" cy="140" r="3" />
              <circle cx="240" cy="180" r="3" />
            </g>
            <!-- highlighted nodes forming a "K" stem + arms -->
            <g class="ink-fill">
              <circle cx="120" cy="60" r="7" />
              <circle cx="120" cy="100" r="7" />
              <circle cx="120" cy="140" r="7" />
              <circle cx="120" cy="180" r="7" />
              <circle cx="160" cy="120" r="7" />
              <circle cx="200" cy="80" r="7" />
              <circle cx="240" cy="60" r="7" />
              <circle cx="200" cy="160" r="7" />
              <circle cx="240" cy="180" r="7" />
            </g>
            <!-- connecting strokes (subtle) -->
            <g style="stroke:var(--ink);stroke-width:1.8;fill:none;stroke-linecap:round;">
              <line x1="120" y1="60" x2="120" y2="180" />
              <line x1="120" y1="120" x2="160" y2="120" />
              <line x1="160" y1="120" x2="240" y2="60" />
              <line x1="160" y1="120" x2="240" y2="180" />
            </g>
            <!-- copper accent at junction -->
            <circle cx="160" cy="120" r="4" class="copper-fill" />
            <text x="180" y="220" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" class="ink-fill">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">4 × 4 node grid</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">28 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">Product UI / platform</div></div>
        </div>
        <p class="plate-meaning">A K, but only as a consequence — the real subject is the lattice. Nodes that are not connected are still part of the system. The capability-pack architecture made visible.</p>
      </div>

      <!-- 05 STAMBHA -->
      <div class="plate dark">
        <div class="plate-head"><span class="plate-no">05 / Stambha</span><span class="plate-name">स्तम्भ · The Pillar</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Stambha mark">
            <!-- guide -->
            <g style="stroke:rgba(242,237,227,0.18);fill:none;stroke-width:1;">
              <line x1="180" y1="30" x2="180" y2="210" />
              <line x1="110" y1="120" x2="250" y2="120" />
            </g>
            <!-- 5-module pillar: 1.0, 0.85, 0.7, 0.85, 1.0 widths -->
            <g class="ivory-fill">
              <rect x="115" y="40" width="130" height="20" />
              <rect x="125" y="70" width="110" height="20" />
              <rect x="135" y="100" width="90" height="40" />
              <rect x="125" y="150" width="110" height="20" />
              <rect x="115" y="180" width="130" height="20" />
            </g>
            <!-- centre copper line -->
            <line x1="180" y1="105" x2="180" y2="135" style="stroke:var(--copper);stroke-width:3;" />
            <text x="180" y="225" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" fill="#F2EDE3">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">Temple-column proportion</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">18 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">Signage / monolithic</div></div>
        </div>
        <p class="plate-meaning">Five horizontal modules drawn at the proportions of a temple column — capital, neck, shaft, base, plinth. A workforce as architecture: load-bearing, symmetrical, eternal.</p>
      </div>

      <!-- 06 PRAVAHA -->
      <div class="plate">
        <div class="plate-head"><span class="plate-no">06 / Pravaha</span><span class="plate-name">प्रवाह · The Flow</span></div>
        <div class="plate-body">
          <svg width="360" height="240" viewBox="0 0 360 240" aria-label="Karamchari Pravaha mark">
            <!-- guide circles -->
            <g class="geo-line">
              <circle cx="150" cy="120" r="55" />
              <circle cx="210" cy="120" r="55" />
            </g>
            <!-- two arcs forming a continuous flow -->
            <g class="ink-stroke" stroke-width="10" stroke-linecap="round" fill="none">
              <path d="M95 120 A 55 55 0 0 1 205 120" />
              <path d="M155 120 A 55 55 0 0 0 265 120" />
            </g>
            <circle cx="180" cy="120" r="7" class="copper-fill" />
            <text x="180" y="220" text-anchor="middle" font-family="Newsreader, serif" font-size="22" letter-spacing="0.04em" class="ink-fill">karamchari</text>
          </svg>
        </div>
        <div class="plate-foot">
          <div><div class="plate-spec-k">Primitive</div><div class="plate-spec-v">Twin arc</div></div>
          <div><div class="plate-spec-k">Min size</div><div class="plate-spec-v">16 px</div></div>
          <div><div class="plate-spec-k">Best for</div><div class="plate-spec-v">Motion / Flow product</div></div>
        </div>
        <p class="plate-meaning">Two arcs meeting at a bindu — one ascending, one descending. The infinite loop of work, but mathematically closed. Reads as flow without lapsing into the cliché of a horizontal infinity.</p>
      </div>

    </div>

    <!-- Recommendation -->
    <div style="margin-top:64px;padding:36px 40px;background:var(--ink);color:var(--ivory);display:grid;grid-template-columns:160px 1fr 200px;gap:32px;align-items:center;">
      <div>
        <svg width="56" height="56" viewBox="0 0 100 100">
          <g class="ivory-stroke" stroke-width="6.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 78 L50 22 L80 78 Z" />
          </g>
          <line x1="34" y1="50" x2="66" y2="50" stroke="#F2EDE3" stroke-width="6.5" stroke-linecap="round" />
          <circle cx="50" cy="50" r="4" fill="#B16A3C" />
        </svg>
      </div>
      <div>
        <div class="mono" style="color:var(--copper-2);margin-bottom:8px;">Strategic recommendation</div>
        <div style="font-family:'Newsreader',serif;font-size:24px;line-height:1.3;letter-spacing:-0.01em;color:#F2EDE3;text-wrap:balance;">
          Adopt <strong style="font-weight:500;">Sutra</strong> as the primary mark, with <strong style="font-weight:500;">Bindu</strong> as the system seal for app icons and module differentiation. The remaining four become a sanctioned secondary glyph library.
        </div>
      </div>
      <div class="mono" style="color:rgba(242,237,227,0.55);">Refer §06</div>
    </div>

  </section>
</div>

<!-- ============================================================ -->
<!-- 03  CONSTRUCTION                                              -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="04 Construction">
    <div class="section-head">
      <div class="section-no">03 — Construction</div>
      <div>
        <h2 class="section-title">Drawn on a 12-unit grid. Provable on a napkin.</h2>
        <p class="section-kicker">
          Every primary mark resolves cleanly to a 12 × 12 module grid. Triangle altitudes
          sit on integer ratios; the bindu always falls at the centroid; clearspace is
          exactly one module on all sides.
        </p>
      </div>
    </div>

    <div class="construction-row">
      <!-- Construction A: grid + sutra -->
      <div class="ccard">
        <div class="ccard-svg">
          <svg viewBox="0 0 240 240" width="100%">
            <!-- 12x12 grid -->
            <g class="geo-line">
              <g>
                <line x1="20" y1="20" x2="20" y2="220" /><line x1="40" y1="20" x2="40" y2="220" />
                <line x1="60" y1="20" x2="60" y2="220" /><line x1="80" y1="20" x2="80" y2="220" />
                <line x1="100" y1="20" x2="100" y2="220" /><line x1="120" y1="20" x2="120" y2="220" />
                <line x1="140" y1="20" x2="140" y2="220" /><line x1="160" y1="20" x2="160" y2="220" />
                <line x1="180" y1="20" x2="180" y2="220" /><line x1="200" y1="20" x2="200" y2="220" />
                <line x1="220" y1="20" x2="220" y2="220" />
              </g>
              <g>
                <line x1="20" y1="20" x2="220" y2="20" /><line x1="20" y1="40" x2="220" y2="40" />
                <line x1="20" y1="60" x2="220" y2="60" /><line x1="20" y1="80" x2="220" y2="80" />
                <line x1="20" y1="100" x2="220" y2="100" /><line x1="20" y1="120" x2="220" y2="120" />
                <line x1="20" y1="140" x2="220" y2="140" /><line x1="20" y1="160" x2="220" y2="160" />
                <line x1="20" y1="180" x2="220" y2="180" /><line x1="20" y1="200" x2="220" y2="200" />
                <line x1="20" y1="220" x2="220" y2="220" />
              </g>
            </g>
            <!-- mark -->
            <g class="ink-stroke" stroke-width="9" stroke-linecap="round" stroke-linejoin="round">
              <path d="M60 180 L120 60 L180 180" />
              <line x1="80" y1="140" x2="160" y2="140" />
            </g>
            <circle cx="120" cy="140" r="7" class="copper-fill" />
          </svg>
        </div>
        <div class="ccard-cap">
          <span class="mono">A · Sutra on grid</span>
          <span class="mono copper">12 × 12</span>
        </div>
      </div>

      <!-- Construction B: bindu radial -->
      <div class="ccard">
        <div class="ccard-svg">
          <svg viewBox="0 0 240 240" width="100%">
            <g class="geo-line">
              <circle cx="120" cy="120" r="100" />
              <circle cx="120" cy="120" r="75" />
              <circle cx="120" cy="120" r="50" />
              <circle cx="120" cy="120" r="25" />
              <line x1="120" y1="20" x2="120" y2="220" />
              <line x1="20" y1="120" x2="220" y2="120" />
              <line x1="49" y1="49" x2="191" y2="191" />
              <line x1="191" y1="49" x2="49" y2="191" />
            </g>
            <g class="ink-fill">
              <circle cx="120" cy="20" r="4" />
              <circle cx="191" cy="49" r="4" />
              <circle cx="220" cy="120" r="4" />
              <circle cx="191" cy="191" r="4" />
              <circle cx="120" cy="220" r="4" />
              <circle cx="49" cy="191" r="4" />
              <circle cx="20" cy="120" r="4" />
              <circle cx="49" cy="49" r="4" />
            </g>
            <circle cx="120" cy="120" r="11" class="copper-fill" />
          </svg>
        </div>
        <div class="ccard-cap">
          <span class="mono">B · Bindu radii</span>
          <span class="mono copper">8-fold</span>
        </div>
      </div>

      <!-- Construction C: clearspace -->
      <div class="ccard">
        <div class="ccard-svg">
          <svg viewBox="0 0 240 240" width="100%">
            <rect x="20" y="60" width="200" height="120" class="geo-line" stroke-dasharray="3 4" />
            <rect x="60" y="80" width="120" height="80" class="geo-line" />
            <g class="ink-stroke" stroke-width="6" stroke-linecap="round" stroke-linejoin="round">
              <path d="M80 145 L120 90 L160 145" />
              <line x1="93" y1="120" x2="147" y2="120" />
            </g>
            <circle cx="120" cy="120" r="4.5" class="copper-fill" />
            <!-- clearspace ticks -->
            <g class="tick">
              <line x1="20" y1="60" x2="60" y2="60" />
              <line x1="20" y1="80" x2="60" y2="80" />
              <line x1="180" y1="60" x2="220" y2="60" />
              <line x1="180" y1="80" x2="220" y2="80" />
            </g>
            <text x="40" y="55" font-family="JetBrains Mono" font-size="10" fill="#B16A3C">x</text>
            <text x="200" y="55" font-family="JetBrains Mono" font-size="10" fill="#B16A3C">x</text>
          </svg>
        </div>
        <div class="ccard-cap">
          <span class="mono">C · Clearspace</span>
          <span class="mono copper">x = 1 module</span>
        </div>
      </div>
    </div>

    <!-- scale table -->
    <table class="spec-table" style="margin-top:64px;">
      <thead>
        <tr><th>Application</th><th>Min height</th><th>Use mark</th><th>Wordmark?</th></tr>
      </thead>
      <tbody>
        <tr><td class="serif">Favicon · App tile</td><td class="mono">16 px</td><td class="serif">Bindu</td><td>—</td></tr>
        <tr><td class="serif">Mobile UI · header</td><td class="mono">24 px</td><td class="serif">Sutra glyph</td><td>—</td></tr>
        <tr><td class="serif">Web nav · dashboard</td><td class="mono">32 px</td><td class="serif">Sutra + wordmark</td><td>Yes</td></tr>
        <tr><td class="serif">Investor deck · cover</td><td class="mono">96 px+</td><td class="serif">Sutra + wordmark</td><td>Yes</td></tr>
        <tr><td class="serif">Office signage · monolith</td><td class="mono">600 mm+</td><td class="serif">Stambha</td><td>Optional</td></tr>
      </tbody>
    </table>
  </section>
</div>

<!-- ============================================================ -->
<!-- 04  SYMBOL EXPLORATION                                        -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="05 Symbols">
    <div class="section-head">
      <div class="section-no">04 — Symbol Exploration</div>
      <div>
        <h2 class="section-title">The grammar beneath the marks.</h2>
        <p class="section-kicker">
          Six conceptual sources, each interrogated and reduced. We show the source —
          and the version we are willing to use. The discipline is in what we removed.
        </p>
      </div>
    </div>

    <div>
      <!-- concept rows -->
      <div class="concept">
        <div>
          <div class="concept-id">SRC-01</div>
          <div class="concept-name">Yantra</div>
          <div class="concept-trans">— ritual diagram, geometric instrument</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="geo-line">
                <rect x="20" y="20" width="160" height="160" />
                <circle cx="100" cy="100" r="70" />
              </g>
              <g class="ink-stroke" stroke-width="3" fill="none" stroke-linejoin="round">
                <path d="M100 40 L160 140 L40 140 Z" />
                <path d="M100 160 L40 60 L160 60 Z" />
              </g>
              <circle cx="100" cy="100" r="5" class="copper-fill" />
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div>
            <h4>What we take</h4>
            <p>The principle of dual triangles in a bounded square — agency descending, intelligence ascending — and the bindu as locus of decision.</p>
          </div>
          <div>
            <h4>What we leave</h4>
            <p>Petals, lotus rings, fire trails, deity associations, the visual density of traditional yantras. Anything that reads as religious artefact.</p>
          </div>
          <div>
            <h4>Operational meaning</h4>
            <p>Top-down policy meeting bottom-up execution. The square is the enterprise boundary. The point is where decisions are made.</p>
          </div>
          <div>
            <h4>Application</h4>
            <p>Used as the “seal of governance” — for audit trails, compliance certifications, and trust badges across the platform.</p>
          </div>
        </div>
      </div>

      <div class="concept">
        <div>
          <div class="concept-id">SRC-02</div>
          <div class="concept-name">Mandala</div>
          <div class="concept-trans">— centred radial system</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="geo-line">
                <circle cx="100" cy="100" r="80" />
                <circle cx="100" cy="100" r="55" />
                <circle cx="100" cy="100" r="30" />
              </g>
              <g class="ink-fill">
                <circle cx="100" cy="20" r="4" /><circle cx="156" cy="44" r="4" />
                <circle cx="180" cy="100" r="4" /><circle cx="156" cy="156" r="4" />
                <circle cx="100" cy="180" r="4" /><circle cx="44" cy="156" r="4" />
                <circle cx="20" cy="100" r="4" /><circle cx="44" cy="44" r="4" />
              </g>
              <circle cx="100" cy="100" r="7" class="copper-fill" />
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div><h4>What we take</h4><p>Centric organisation; every element addresses a single locus; rotational symmetry; the dignity of a still centre.</p></div>
          <div><h4>What we leave</h4><p>Concentric petalwork, anything that suggests meditation aids or wall art. The mandala becomes structure, not decoration.</p></div>
          <div><h4>Operational meaning</h4><p>The orchestration model. Every workflow, every workforce node, every module rotates around a governance core.</p></div>
          <div><h4>Application</h4><p>Loading states, AI assistant idle states, and the visual language of the Karamchari Pulse analytics surface.</p></div>
        </div>
      </div>

      <div class="concept">
        <div>
          <div class="concept-id">SRC-03</div>
          <div class="concept-name">Lattice</div>
          <div class="concept-trans">— modular interconnected mesh</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="geo-dot">
                <circle cx="40" cy="40" r="3" /><circle cx="80" cy="40" r="3" /><circle cx="120" cy="40" r="3" /><circle cx="160" cy="40" r="3" />
                <circle cx="40" cy="80" r="3" /><circle cx="80" cy="80" r="3" /><circle cx="120" cy="80" r="3" /><circle cx="160" cy="80" r="3" />
                <circle cx="40" cy="120" r="3" /><circle cx="80" cy="120" r="3" /><circle cx="120" cy="120" r="3" /><circle cx="160" cy="120" r="3" />
                <circle cx="40" cy="160" r="3" /><circle cx="80" cy="160" r="3" /><circle cx="120" cy="160" r="3" /><circle cx="160" cy="160" r="3" />
              </g>
              <g style="stroke:var(--ink);stroke-width:1.5;fill:none;">
                <line x1="40" y1="40" x2="120" y2="80" />
                <line x1="120" y1="80" x2="80" y2="160" />
                <line x1="120" y1="80" x2="160" y2="120" />
              </g>
              <g class="ink-fill">
                <circle cx="40" cy="40" r="6" /><circle cx="120" cy="80" r="6" /><circle cx="80" cy="160" r="6" /><circle cx="160" cy="120" r="6" />
              </g>
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div><h4>What we take</h4><p>The grammar of capability packs: discrete nodes, deliberate connections, idle nodes that exist as future capacity.</p></div>
          <div><h4>What we leave</h4><p>Tech-bro neural-net visuals, dense graph-database renders, anything that suggests crypto or generic AI startups.</p></div>
          <div><h4>Operational meaning</h4><p>The platform’s most honest self-portrait: a system whose value is in which dots you decide to connect, not in the dots themselves.</p></div>
          <div><h4>Application</h4><p>Module diagrams, capability marketplace tiles, dashboard topology views, board-deck architecture slides.</p></div>
        </div>
      </div>

      <div class="concept">
        <div>
          <div class="concept-id">SRC-04</div>
          <div class="concept-name">Stambha</div>
          <div class="concept-trans">— temple column, load-bearing form</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="ink-fill">
                <rect x="40" y="30" width="120" height="14" />
                <rect x="50" y="50" width="100" height="14" />
                <rect x="62" y="70" width="76" height="60" />
                <rect x="50" y="136" width="100" height="14" />
                <rect x="40" y="156" width="120" height="14" />
              </g>
              <line x1="100" y1="74" x2="100" y2="126" style="stroke:var(--copper);stroke-width:3;" />
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div><h4>What we take</h4><p>Proportional logic — 1.0, 0.85, 0.7, 0.85, 1.0 — borrowed from the canon of South Indian temple column ratios.</p></div>
          <div><h4>What we leave</h4><p>Carving, ornamentation, capital flourishes, anything that signals “heritage tourism”. The proportion stays. The decoration goes.</p></div>
          <div><h4>Operational meaning</h4><p>The platform’s monumental face. The column says: this is infrastructure your CFO can lean on.</p></div>
          <div><h4>Application</h4><p>Office signage, investor decks, government RFPs, conference backdrops, the brand’s “heavy moment”.</p></div>
        </div>
      </div>

      <div class="concept">
        <div>
          <div class="concept-id">SRC-05</div>
          <div class="concept-name">Sutra</div>
          <div class="concept-trans">— thread, aphorism, terse rule</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="ink-stroke" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">
                <path d="M50 150 L100 60 L150 150" />
                <line x1="68" y1="118" x2="132" y2="118" />
              </g>
              <circle cx="100" cy="118" r="6" class="copper-fill" />
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div><h4>What we take</h4><p>The Sanskritic idea that a single line — drawn or written — can encode an entire system. Compression as virtue.</p></div>
          <div><h4>What we leave</h4><p>Script-style flourishes, calligraphic motion, any visual reference to written devanagari. The sutra here is geometric, not literary.</p></div>
          <div><h4>Operational meaning</h4><p>Every Karamchari workflow is, at root, a sutra: a terse rule that compresses a whole organisational policy into something executable.</p></div>
          <div><h4>Application</h4><p>The primary mark. The face of the company. The thing that appears at 16 px and at 6 metres.</p></div>
        </div>
      </div>

      <div class="concept">
        <div>
          <div class="concept-id">SRC-06</div>
          <div class="concept-name">Pravaha</div>
          <div class="concept-trans">— flow, current, continuous motion</div>
          <div style="margin-top:24px;">
            <svg viewBox="0 0 200 200" width="200">
              <g class="ink-stroke" stroke-width="8" stroke-linecap="round" fill="none">
                <path d="M40 100 A 40 40 0 0 1 120 100" />
                <path d="M80 100 A 40 40 0 0 0 160 100" />
              </g>
              <circle cx="100" cy="100" r="6" class="copper-fill" />
            </svg>
          </div>
        </div>
        <div class="concept-grid">
          <div><h4>What we take</h4><p>The closed twin-arc: motion bounded by intention. A loop with a centre, not a directionless infinity.</p></div>
          <div><h4>What we leave</h4><p>The generic ∞ symbol, sine waves, ribbon-knots, anything that reads as marketing-ops or RPA branding.</p></div>
          <div><h4>Operational meaning</h4><p>Used wherever the brand needs to suggest movement: Karamchari Flow, automation routines, the live state of running workflows.</p></div>
          <div><h4>Application</h4><p>Motion logos, transitions, real-time states, the Flow sub-product mark.</p></div>
        </div>
      </div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- 05  TYPOGRAPHY                                                -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="06 Typography">
    <div class="section-head">
      <div class="section-no">05 — Typography</div>
      <div>
        <h2 class="section-title">A serif for the long thought. A grotesque for the operating system.</h2>
        <p class="section-kicker">
          Karamchari uses a humanist serif for the philosophical voice and a precision
          grotesque for the operational voice. The pairing performs the brand’s central
          claim — that intelligence and execution are the same act, written in different
          weights.
        </p>
      </div>
    </div>

    <div class="type-spec">
      <div class="type-row">
        <div class="type-name">Display</div>
        <div class="type-size">72 / -2%</div>
        <div class="type-sample-serif" style="font-size:64px;font-weight:300;">Karamchari.</div>
        <div class="type-meta">Newsreader 300 italic-capable. Editorial. Humanist apertures. Quietly Indic without orientalism.</div>
      </div>
      <div class="type-row">
        <div class="type-name">Headline</div>
        <div class="type-size">36 / -1%</div>
        <div class="type-sample-serif" style="font-size:32px;font-weight:400;font-style:italic;">the operator of operators</div>
        <div class="type-meta">Italic reserved for philosophical voice. Never used in UI.</div>
      </div>
      <div class="type-row">
        <div class="type-name">UI / Body</div>
        <div class="type-size">15 / 1.55</div>
        <div class="type-sample-sans" style="font-size:16px;">Geist is the operating voice — a modern grotesque tuned for dashboards, dense tables and decisive button copy. Readable from 12 px to 24 px without retuning.</div>
        <div class="type-meta">Geist 400/500/600. Replaces system stacks. No Inter, no Roboto, no Helvetica.</div>
      </div>
      <div class="type-row">
        <div class="type-name">Tabular</div>
        <div class="type-size">13 / num</div>
        <div class="type-sample-sans" style="font-size:14px;font-variant-numeric:tabular-nums;">1,284 active operators · 92.4% policy adherence · ₹ 18.6 Cr automated</div>
        <div class="type-meta">Tabular nums enabled platform-wide. Critical for trust.</div>
      </div>
      <div class="type-row">
        <div class="type-name">Code · Spec</div>
        <div class="type-size">11 / 0.12em</div>
        <div class="type-sample-mono" style="font-size:12px;">KARM-OPS-2026-Q2 · #1B2A4E · wf.run("policy.review")</div>
        <div class="type-meta">JetBrains Mono. Used in coordinates, IDs, telemetry, plate captions.</div>
      </div>
      <div class="type-row">
        <div class="type-name">Wordmark</div>
        <div class="type-size">custom</div>
        <div class="type-sample-serif" style="font-size:54px;letter-spacing:-0.025em;font-weight:300;">karam<em style="font-style:italic;color:var(--indigo);">chari</em></div>
        <div class="type-meta">Lowercase. Italic stress on the “chari” to mark the human operator. Single ligature opportunity between r &amp; c.</div>
      </div>
    </div>

    <div class="two-col" style="margin-top:64px;">
      <div></div>
      <div class="body-col">
        <p><strong>Why a serif at all.</strong> Most enterprise SaaS reaches for a sans because it feels “neutral”. The result is interchangeability. A serif headline tells the reader the brand believes its sentences are worth reading carefully — which is exactly the disposition Karamchari wants its customers to bring to their workforce.</p>
        <p><strong>Why Geist (not Inter).</strong> Inter is the default of the default. Geist offers the same operational clarity with a slightly more architectural skeleton; it sits beside Newsreader without flinching, and it does not appear on every other product on the internet.</p>
      </div>
      <div class="body-col">
        <p><strong>Letterform opportunities.</strong> A custom <em>k</em> with a single horizontal armature, echoing the Sutra mark. A <em>chari</em> tail that lifts very slightly to recall the curve of Pravaha. These are details — implemented only in the wordmark, never in body type.</p>
        <p><strong>Ligature.</strong> An optional <em>rc</em> ligature in the wordmark only. Off by default. Used in monumental and signage settings where the wordmark is its own composition.</p>
        <p><strong>Devanagari.</strong> A companion <em>कर्मचारी</em> rendered in Tiro Devanagari Sanskrit, sized to match the Latin x-height. Used selectively in Indian-market collateral. Never paired with English in the primary lockup — the brand is not bilingual; it is contextually multilingual.</p>
      </div>
    </div>

  </section>
</div>

<!-- ============================================================ -->
<!-- 06  COLOR                                                     -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="07 Color">
    <div class="section-head">
      <div class="section-no">06 — Color System</div>
      <div>
        <h2 class="section-title">Two anchors, two voices, two accents. Nothing else.</h2>
        <p class="section-kicker">
          The palette is built from two anchors (ink, ivory), two voices (indigo, forest)
          and two accents (copper, gold). Saturations stay low; chromas stay aligned in
          OKLCH. Nothing in this system looks like a SaaS gradient.
        </p>
      </div>
    </div>

    <div class="swatches">
      <!-- Anchor 1: ink -->
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#0E0F12;">
          <div class="sw-role">Anchor · 01</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Karam Ink</div><div class="sw-hex">#0E0F12 · oklch(0.20 0.005 280)</div></div>
      </div>
      <!-- Anchor 2: ivory -->
      <div class="sw">
        <div class="sw-chip" style="background:#F2EDE3;">
          <div class="sw-role">Anchor · 02</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Sutra Ivory</div><div class="sw-hex">#F2EDE3 · oklch(0.94 0.010 85)</div></div>
      </div>
      <!-- Voice 1: indigo -->
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#1B2A4E;">
          <div class="sw-role">Voice · Trust</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Yantra Indigo</div><div class="sw-hex">#1B2A4E · oklch(0.30 0.08 265)</div></div>
      </div>
      <!-- Voice 2: forest -->
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#2D5D4B;">
          <div class="sw-role">Voice · Governance</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Sthira Forest</div><div class="sw-hex">#2D5D4B · oklch(0.46 0.06 160)</div></div>
      </div>
      <!-- Accent 1: copper -->
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#B16A3C;">
          <div class="sw-role">Accent · Action</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Tamra Copper</div><div class="sw-hex">#B16A3C · oklch(0.62 0.10 50)</div></div>
      </div>
      <!-- Accent 2: gold -->
      <div class="sw">
        <div class="sw-chip" style="background:#C9A86A;">
          <div class="sw-role">Accent · Honour</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Hiranya Gold</div><div class="sw-hex">#C9A86A · oklch(0.74 0.09 80)</div></div>
      </div>
      <!-- Neutrals -->
      <div class="sw">
        <div class="sw-chip" style="background:#D9D2C2;">
          <div class="sw-role">Neutral · 01</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Cloud</div><div class="sw-hex">#D9D2C2</div></div>
      </div>
      <div class="sw">
        <div class="sw-chip" style="background:#9B9485;">
          <div class="sw-role">Neutral · 02</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Stone</div><div class="sw-hex">#9B9485</div></div>
      </div>
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#2A2D33;">
          <div class="sw-role">Neutral · 03</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Graphite</div><div class="sw-hex">#2A2D33</div></div>
      </div>
      <div class="sw">
        <div class="sw-chip on-dark" style="background:#5C5F66;">
          <div class="sw-role">Neutral · 04</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Iron</div><div class="sw-hex">#5C5F66</div></div>
      </div>
      <div class="sw">
        <div class="sw-chip" style="background:#7A2B2B;">
          <div class="sw-role">Status · Critical</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Rakta</div><div class="sw-hex">#7A2B2B</div></div>
      </div>
      <div class="sw">
        <div class="sw-chip" style="background:#9C842A;">
          <div class="sw-role">Status · Caution</div>
        </div>
        <div class="sw-meta"><div class="sw-name">Pita</div><div class="sw-hex">#9C842A</div></div>
      </div>
    </div>

    <div class="two-col" style="margin-top:64px;">
      <div></div>
      <div class="body-col">
        <p><strong>Emotional reasoning.</strong> Ink and ivory carry the entire identity in 80% of surfaces — like the page you are reading. Indigo signals trust without resorting to corporate blue. Forest carries governance and audit. Copper appears as a single point — the bindu — and only there.</p>
        <p><strong>OKLCH discipline.</strong> All accent hues are tuned to similar lightness (0.62–0.74) and similar chroma (0.06–0.10) so the palette feels of-a-piece under any tinting or theming.</p>
      </div>
      <div class="body-col">
        <p><strong>Accessibility.</strong> Ink on Ivory: AAA. Indigo on Ivory: AAA. Copper on Ink: AA Large. Copper on Ivory: AA Large (use only as accent, never as long-form text colour). Forest on Ivory: AAA.</p>
        <p><strong>No gradients.</strong> The brand explicitly forbids gradient fills on logo, type and ambient surfaces. The one sanctioned exception is a 4° tonal wash between two adjacent neutrals — used for spatial depth in dashboards, never for decoration.</p>
        <p><strong>Dark mode.</strong> Inverts to Karam Ink with Cloud type and Copper accents at 90% opacity. The indigo voice is replaced by a +12% lightness variant to maintain hierarchy.</p>
      </div>
    </div>

  </section>
</div>

<!-- ============================================================ -->
<!-- 07  BRAND ARCHITECTURE                                        -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="08 Architecture">
    <div class="section-head">
      <div class="section-no">07 — Brand Architecture</div>
      <div>
        <h2 class="section-title">One root. Five capabilities. A single visual vocabulary.</h2>
        <p class="section-kicker">
          Karamchari is the root brand. Each module is a capability of the platform, never a
          separate product. Names follow a single Sanskritic logic: the root verb, then the
          surface it operates on.
        </p>
      </div>
    </div>

    <div class="subbrands">
      <!-- Core -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="ink-stroke" stroke-width="6.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 78 L50 22 L80 78 Z" />
              <line x1="32" y1="58" x2="68" y2="58" />
            </g>
            <circle cx="50" cy="58" r="4" class="copper-fill" />
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">Core</span></div>
        <div class="sb-sansk">Mūla — the root</div>
        <div class="sb-desc">The substrate. People, roles, policies, the source of truth. Every module addresses Core; Core addresses everything.</div>
      </div>
      <!-- Flow -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="ink-stroke" stroke-width="6.5" stroke-linecap="round" fill="none">
              <path d="M20 50 A 20 20 0 0 1 60 50" />
              <path d="M40 50 A 20 20 0 0 0 80 50" />
            </g>
            <circle cx="50" cy="50" r="4" class="copper-fill" />
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">Flow</span></div>
        <div class="sb-sansk">Pravāha — the current</div>
        <div class="sb-desc">The workflow builder. Visual orchestration of multi-step, multi-actor processes with deterministic state and audit trails.</div>
      </div>
      <!-- Ops -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="ink-fill">
              <rect x="20" y="20" width="60" height="8" />
              <rect x="26" y="34" width="48" height="8" />
              <rect x="32" y="48" width="36" height="20" />
              <rect x="26" y="74" width="48" height="8" />
            </g>
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">Ops</span></div>
        <div class="sb-sansk">Karma — the action</div>
        <div class="sb-desc">The execution layer. Shifts, tasks, attendance, location-aware operations, field workforce intelligence.</div>
      </div>
      <!-- Pulse -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="geo-line"><circle cx="50" cy="50" r="35" /><circle cx="50" cy="50" r="20" /></g>
            <g class="ink-fill">
              <circle cx="50" cy="15" r="3" /><circle cx="80" cy="35" r="3" /><circle cx="80" cy="65" r="3" />
              <circle cx="50" cy="85" r="3" /><circle cx="20" cy="65" r="3" /><circle cx="20" cy="35" r="3" />
            </g>
            <circle cx="50" cy="50" r="5" class="copper-fill" />
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">Pulse</span></div>
        <div class="sb-sansk">Spanda — the rhythm</div>
        <div class="sb-desc">Operational intelligence. Real-time pulse on adherence, productivity and exception. The mandala source made into analytics.</div>
      </div>
      <!-- AI -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="ink-stroke" stroke-width="2" fill="none">
              <path d="M50 20 L78 70 L22 70 Z" />
              <path d="M50 80 L22 30 L78 30 Z" />
            </g>
            <circle cx="50" cy="50" r="6" class="copper-fill" />
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">AI</span></div>
        <div class="sb-sansk">Buddhi — the intellect</div>
        <div class="sb-desc">The assistant layer. Reads the lattice of Core, runs in Flow, observes Ops and Pulse, then proposes the next intelligent action.</div>
      </div>
      <!-- Future / Packs -->
      <div class="sb">
        <div class="sb-mark">
          <svg width="60" height="60" viewBox="0 0 100 100">
            <g class="geo-dot">
              <circle cx="25" cy="25" r="3" /><circle cx="50" cy="25" r="3" /><circle cx="75" cy="25" r="3" />
              <circle cx="25" cy="50" r="3" /><circle cx="50" cy="50" r="3" /><circle cx="75" cy="50" r="3" />
              <circle cx="25" cy="75" r="3" /><circle cx="50" cy="75" r="3" /><circle cx="75" cy="75" r="3" />
            </g>
            <g class="ink-fill">
              <circle cx="50" cy="25" r="6" />
              <circle cx="25" cy="50" r="6" />
              <circle cx="75" cy="75" r="6" />
            </g>
          </svg>
        </div>
        <div class="sb-name">Karamchari <span class="indigo">Packs</span></div>
        <div class="sb-sansk">Aṅga — the limbs</div>
        <div class="sb-desc">The modular capability marketplace. Compliance, payroll, learning, frontline safety — each a configurable lattice node.</div>
      </div>
    </div>

    <div class="two-col" style="margin-top:64px;">
      <div></div>
      <div class="body-col">
        <p><strong>Naming consistency.</strong> Every module is named with a one-syllable English noun (Core, Flow, Ops, Pulse, AI, Packs) backed by a Sanskrit operational concept (Mūla, Pravāha, Karma, Spanda, Buddhi, Aṅga). The English is the marketing surface; the Sanskrit is the internal vocabulary.</p>
        <p><strong>Visual hierarchy.</strong> Every module mark is a derivative of one of the six Section 02 concepts — none is invented from scratch. Module-level marks share stroke weight, optical sizing and the copper bindu.</p>
      </div>
      <div class="body-col">
        <p><strong>Icon consistency.</strong> All product iconography is drawn on the same 24-grid with the same 1.75-unit stroke. Icons may use copper accents only at decision points (CTA, primary status, AI agency).</p>
        <p><strong>Differentiation without divergence.</strong> Modules are differentiated by geometry (curve vs lattice vs column) rather than by colour. Colour stays constant so customers can buy capability packs without re-learning the platform.</p>
      </div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- 08  UI INTEGRATION                                            -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="09 UI Integration">
    <div class="section-head">
      <div class="section-no">08 — UI Integration</div>
      <div>
        <h2 class="section-title">The brand survives at 12 px and at 4K.</h2>
        <p class="section-kicker">
          Wherever the brand appears in product, it inherits the same proportional logic.
          Below: a dashboard frame, an AI assistant card, an onboarding moment, and the
          mobile operator view.
        </p>
      </div>
    </div>

    <div class="ui-grid">
      <!-- Dashboard mock -->
      <div class="ui-card">
        <div class="ui-cap"><span>Pulse · Operational Dashboard</span><span class="copper">01</span></div>
        <div style="display:grid;grid-template-columns:48px 1fr;gap:18px;">
          <div style="border-right:1px solid var(--hair);padding-right:14px;display:grid;gap:18px;justify-items:center;align-content:start;padding-top:4px;">
            <svg width="22" height="22" viewBox="0 0 100 100"><g class="ink-stroke" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"><path d="M20 78 L50 22 L80 78 Z"/><line x1="34" y1="58" x2="66" y2="58"/></g><circle cx="50" cy="58" r="6" class="copper-fill"/></svg>
            <svg width="18" height="18" viewBox="0 0 100 100"><g class="ink-stroke" stroke-width="7" fill="none"><path d="M20 50 A 18 18 0 0 1 56 50"/><path d="M44 50 A 18 18 0 0 0 80 50"/></g></svg>
            <svg width="18" height="18" viewBox="0 0 100 100"><g class="ink-fill"><rect x="20" y="22" width="60" height="8"/><rect x="26" y="36" width="48" height="8"/><rect x="32" y="50" width="36" height="22"/></g></svg>
            <svg width="18" height="18" viewBox="0 0 100 100"><g style="stroke:var(--ink);stroke-width:1.8;fill:none;"><circle cx="50" cy="50" r="28"/><circle cx="50" cy="50" r="15"/></g><circle cx="50" cy="50" r="5" class="copper-fill"/></svg>
          </div>
          <div>
            <div style="display:flex;justify-content:space-between;align-items:baseline;margin-bottom:18px;">
              <div>
                <div class="mono" style="color:var(--copper);">Workforce · Live</div>
                <div style="font-family:'Newsreader',serif;font-size:26px;letter-spacing:-0.01em;margin-top:4px;">Pulse, this morning</div>
              </div>
              <div style="font-family:'JetBrains Mono',monospace;font-size:11px;color:var(--graphite);">12 May · 09:42 IST</div>
            </div>
            <div style="display:grid;grid-template-columns:repeat(3,1fr);gap:14px;">
              <div style="border:1px solid var(--hair);padding:14px;">
                <div class="mono">Active operators</div>
                <div style="font-family:'Newsreader',serif;font-size:32px;letter-spacing:-0.01em;font-variant-numeric:tabular-nums;">1,284</div>
                <div style="font-size:12px;color:var(--graphite);font-variant-numeric:tabular-nums;">+ 4.1% vs Mon</div>
              </div>
              <div style="border:1px solid var(--hair);padding:14px;">
                <div class="mono">Policy adherence</div>
                <div style="font-family:'Newsreader',serif;font-size:32px;letter-spacing:-0.01em;font-variant-numeric:tabular-nums;">92.4<span style="font-size:18px;color:var(--graphite);">%</span></div>
                <div style="font-size:12px;color:var(--copper);font-variant-numeric:tabular-nums;">3 exceptions</div>
              </div>
              <div style="border:1px solid var(--hair);padding:14px;">
                <div class="mono">Flows running</div>
                <div style="font-family:'Newsreader',serif;font-size:32px;letter-spacing:-0.01em;font-variant-numeric:tabular-nums;">37</div>
                <div style="font-size:12px;color:var(--graphite);font-variant-numeric:tabular-nums;">2 awaiting review</div>
              </div>
            </div>
            <div style="border:1px solid var(--hair);margin-top:14px;padding:18px;background:var(--ivory-2);">
              <div class="mono copper" style="margin-bottom:10px;">Karamchari AI · suggestion</div>
              <div style="font-family:'Newsreader',serif;font-size:17px;line-height:1.45;color:var(--ink);text-wrap:pretty;">Three of your retail flows have exceeded their adherence threshold this week. Would you like Karamchari to draft a remediation policy for review?</div>
              <div style="display:flex;gap:10px;margin-top:14px;">
                <button style="background:var(--ink);color:var(--ivory);border:none;padding:9px 14px;font-family:'Geist',sans-serif;font-size:13px;letter-spacing:0.01em;">Draft policy</button>
                <button style="background:transparent;color:var(--ink);border:1px solid var(--hair-strong);padding:9px 14px;font-family:'Geist',sans-serif;font-size:13px;">Dismiss</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Mobile operator -->
      <div class="ui-card dark">
        <div class="ui-cap"><span>Ops · Mobile</span><span style="color:var(--copper-2);">02</span></div>
        <div style="display:flex;justify-content:space-between;align-items:center;margin-top:8px;">
          <svg width="22" height="22" viewBox="0 0 100 100"><g class="ivory-stroke" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"><path d="M20 78 L50 22 L80 78 Z"/><line x1="34" y1="58" x2="66" y2="58"/></g><circle cx="50" cy="58" r="6" class="copper-fill"/></svg>
          <div style="font-family:'JetBrains Mono',monospace;font-size:10px;letter-spacing:0.12em;color:rgba(242,237,227,0.5);">SHIFT · 09:00 — 18:00</div>
        </div>
        <div style="font-family:'Newsreader',serif;font-size:26px;line-height:1.2;letter-spacing:-0.01em;margin-top:32px;color:var(--ivory);">Good morning, <span style="color:var(--copper-2);font-style:italic;">Asha</span>.<br/>You have 4 tasks today.</div>
        <div style="margin-top:24px;display:grid;gap:10px;">
          <div style="border:1px solid rgba(242,237,227,0.12);padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--copper-2);letter-spacing:0.12em;">09:30 · INVENTORY</div>
            <div style="font-family:'Newsreader',serif;font-size:17px;color:var(--ivory);margin-top:4px;">Open-stock count, Bay 3</div>
          </div>
          <div style="border:1px solid rgba(242,237,227,0.12);padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--copper-2);letter-spacing:0.12em;">11:00 · COMPLIANCE</div>
            <div style="font-family:'Newsreader',serif;font-size:17px;color:var(--ivory);margin-top:4px;">Cold-chain temperature log</div>
          </div>
          <div style="border:1px solid rgba(242,237,227,0.12);padding:14px;opacity:0.6;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:10px;color:rgba(242,237,227,0.5);letter-spacing:0.12em;">14:00 · TRAINING</div>
            <div style="font-family:'Newsreader',serif;font-size:17px;color:var(--ivory);margin-top:4px;">Safety module, 12 min</div>
          </div>
        </div>
        <button style="margin-top:18px;width:100%;background:var(--copper);color:var(--ivory);border:none;padding:13px;font-family:'Geist',sans-serif;font-size:14px;font-weight:500;">Begin shift</button>
      </div>
    </div>

    <!-- Onboarding -->
    <div style="margin-top:24px;border:1px solid var(--hair);background:var(--ivory);padding:48px 56px;display:grid;grid-template-columns:1fr 1fr;gap:48px;align-items:center;">
      <div>
        <div class="mono copper" style="margin-bottom:18px;">Onboarding · welcome surface</div>
        <div style="font-family:'Newsreader',serif;font-size:44px;line-height:1.05;letter-spacing:-0.02em;color:var(--ink);text-wrap:balance;">Welcome to Karamchari.<br/><span style="font-style:italic;color:var(--indigo);">Let’s describe how your work happens.</span></div>
        <div style="margin-top:20px;font-size:15px;line-height:1.6;color:var(--ink-2);max-width:440px;">Three steps. Six minutes. We will model your operators, your shifts and your policies. Nothing is final — every answer is a sutra you can revise.</div>
        <div style="display:flex;gap:24px;margin-top:32px;align-items:center;">
          <button style="background:var(--ink);color:var(--ivory);border:none;padding:14px 22px;font-family:'Geist',sans-serif;font-size:14px;letter-spacing:0.01em;">Begin</button>
          <span class="mono">Step 1 of 3 · ~6 min</span>
        </div>
      </div>
      <div style="display:grid;place-items:center;">
        <svg width="220" height="220" viewBox="0 0 200 200">
          <g class="geo-line">
            <circle cx="100" cy="100" r="90" />
            <circle cx="100" cy="100" r="65" />
            <circle cx="100" cy="100" r="40" />
          </g>
          <g class="ink-fill">
            <circle cx="100" cy="10" r="3" /><circle cx="164" cy="36" r="3" />
            <circle cx="190" cy="100" r="3" /><circle cx="164" cy="164" r="3" />
            <circle cx="100" cy="190" r="3" /><circle cx="36" cy="164" r="3" />
            <circle cx="10" cy="100" r="3" /><circle cx="36" cy="36" r="3" />
          </g>
          <circle cx="100" cy="100" r="9" class="copper-fill" />
        </svg>
      </div>
    </div>

  </section>
</div>

<!-- ============================================================ -->
<!-- 09  MOTION                                                    -->
<!-- ============================================================ -->
<section class="dark-band" data-screen-label="10 Motion">
  <div class="page">
    <div class="section-head">
      <div class="section-no">09 — Motion</div>
      <div>
        <h2 class="section-title">Motion is the brand’s second voice.</h2>
        <p class="section-kicker">
          Karamchari’s motion is patient, mathematical and curved like breath. Easings sit
          on cubic-bezier(0.22, 1, 0.36, 1). Durations are quantised to 120 ms steps. The
          system never bounces, never wobbles, never decorates.
        </p>
      </div>
    </div>

    <div class="motion-strip" style="background:var(--ink);border-color:rgba(242,237,227,0.12);">
      <div class="motion-frame" style="border-right-color:rgba(242,237,227,0.12);">
        <span class="mono" style="color:rgba(242,237,227,0.5);">t = 0ms</span>
        <svg width="120" height="120" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="3.5" class="copper-fill" />
        </svg>
      </div>
      <div class="motion-frame" style="border-right-color:rgba(242,237,227,0.12);">
        <span class="mono" style="color:rgba(242,237,227,0.5);">t = 120ms</span>
        <svg width="120" height="120" viewBox="0 0 100 100">
          <g style="stroke:rgba(242,237,227,0.3);fill:none;stroke-width:1;"><circle cx="50" cy="50" r="22"/></g>
          <circle cx="50" cy="50" r="3.5" class="copper-fill" />
        </svg>
      </div>
      <div class="motion-frame" style="border-right-color:rgba(242,237,227,0.12);">
        <span class="mono" style="color:rgba(242,237,227,0.5);">t = 360ms</span>
        <svg width="120" height="120" viewBox="0 0 100 100">
          <g style="stroke:rgba(242,237,227,0.5);fill:none;stroke-width:1;"><circle cx="50" cy="50" r="32"/></g>
          <g class="ivory-stroke" stroke-width="6" stroke-linecap="round" fill="none">
            <path d="M30 70 L50 30 L70 70" />
          </g>
          <circle cx="50" cy="50" r="3.5" class="copper-fill" />
        </svg>
      </div>
      <div class="motion-frame" style="border-right-color:rgba(242,237,227,0.12);">
        <span class="mono" style="color:rgba(242,237,227,0.5);">t = 600ms</span>
        <svg width="120" height="120" viewBox="0 0 100 100">
          <g class="ivory-stroke" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" fill="none">
            <path d="M30 70 L50 30 L70 70" />
            <line x1="38" y1="55" x2="62" y2="55" />
          </g>
          <circle cx="50" cy="55" r="3.5" class="copper-fill" />
        </svg>
      </div>
      <div class="motion-frame">
        <span class="mono" style="color:rgba(242,237,227,0.5);">t = 840ms</span>
        <svg width="120" height="120" viewBox="0 0 100 100">
          <g class="ivory-stroke" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" fill="none">
            <path d="M30 70 L50 30 L70 70" />
            <line x1="38" y1="55" x2="62" y2="55" />
          </g>
          <circle cx="50" cy="55" r="4.5" class="copper-fill" />
          <text x="50" y="92" text-anchor="middle" font-family="Newsreader, serif" font-size="9" fill="#F2EDE3">karamchari</text>
        </svg>
      </div>
    </div>

    <div class="two-col" style="margin-top:64px;">
      <div></div>
      <div class="body-col">
        <p><strong>Logo build.</strong> The bindu always arrives first. The triangle is drawn around it, never to it. The thread (the horizontal cut) lands last. The mark is constructed in the order of its meaning: intent → form → rule.</p>
        <p><strong>Loading states.</strong> The orbital dot ring of the Bindu mark rotates at a constant 8 RPM. It does not accelerate. It does not bounce on completion — it simply stops and a single dot pulses once.</p>
      </div>
      <div class="body-col">
        <p><strong>Transitions.</strong> Surfaces slide along a 12-unit grid in 240 ms. Modal entries scale from 0.96 to 1.0. Nothing fades in alone — opacity is always paired with a 8 px positional move.</p>
        <p><strong>AI assistant idle.</strong> The Bindu’s eight orbital dots breathe — slowly cycling brightness on a 4-second loop, one dot at a time, clockwise. Designed to read as “listening”, never as “thinking hard”.</p>
        <p><strong>Workflow visualisation.</strong> Flow nodes connect with a single drawn line that traces along its path in 400 ms. Direction matters; arrows do not.</p>
      </div>
    </div>
  </div>
</section>

<!-- ============================================================ -->
<!-- 10  COMPETITIVE                                               -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="11 Competitive">
    <div class="section-head">
      <div class="section-no">10 — Competitive Positioning</div>
      <div>
        <h2 class="section-title">Where everyone else is loud, Karamchari is composed.</h2>
        <p class="section-kicker">
          The peer set we measure against is intentionally broad — operating systems,
          developer tools, AI labs, enterprise stalwarts. The differentiation is one of
          posture more than feature.
        </p>
      </div>
    </div>

    <div>
      <div class="compete">
        <div class="compete-name">Stripe</div>
        <div><h5>Their posture</h5><p>Developer-cinematic. Hyper-vivid gradients. Type-driven storytelling. Brand sits in the foreground.</p></div>
        <div><h5>Karamchari’s reply</h5><p>The opposite of cinematic. Composed, paper-feel, mathematical. We are not the spectacle; the workforce is.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">Linear</div>
        <div><h5>Their posture</h5><p>Dark, terse, gamer-developer minimalism. Speed as identity. A toolmaker’s tool.</p></div>
        <div><h5>Karamchari’s reply</h5><p>Light, classical, considered. Speed without theatre. A platform that takes itself seriously the way ledgers do, not the way IDEs do.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">Notion</div>
        <div><h5>Their posture</h5><p>Approachable maximalism. Friendly illustration. A blank page that wants to be everything.</p></div>
        <div><h5>Karamchari’s reply</h5><p>No illustration. No mascots. We are not a blank page; we are a frame. The customer fills it with their workforce, not their imagination.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">OpenAI / Frontier AI</div>
        <div><h5>Their posture</h5><p>Mystical-mathematical: orbs, monoliths, ambient glow. Performs depth.</p></div>
        <div><h5>Karamchari’s reply</h5><p>Mathematical without mysticism. Our depth is in the proportions, not the lighting. AI is a capability we use, not the show we sell.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">SAP</div>
        <div><h5>Their posture</h5><p>Legacy enterprise blue. Trustworthy, unmodern, immovable.</p></div>
        <div><h5>Karamchari’s reply</h5><p>Equal trust, modern grammar. We earn the CFO without losing the COO.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">Workday</div>
        <div><h5>Their posture</h5><p>Corporate blue + earnest illustration. The HR-software inheritance.</p></div>
        <div><h5>Karamchari’s reply</h5><p>No HR clichés. Our workforce is not “people” in a stock photo; it is operators in a system. Indigo, not corporate blue.</p></div>
      </div>
      <div class="compete">
        <div class="compete-name">ServiceNow</div>
        <div><h5>Their posture</h5><p>Workflow as enterprise plumbing. Heavy product, light identity.</p></div>
        <div><h5>Karamchari’s reply</h5><p>Same plumbing, deliberate identity. The brand carries as much intentionality as the platform.</p></div>
      </div>
    </div>

    <div style="margin-top:48px;display:grid;grid-template-columns:120px 1fr;gap:48px;padding:36px 0;border-top:1px solid var(--hair);">
      <div class="mono copper">In summary</div>
      <div class="pull-q">
        “The competitive moat is not aesthetics. It is the willingness to make every surface — including the logo — operate on the same standards we ask our customers to.”
      </div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- 11  ANTI-PATTERNS                                             -->
<!-- ============================================================ -->
<div class="page">
  <section class="section" data-screen-label="12 Anti-Patterns">
    <div class="section-head">
      <div class="section-no">11 — Anti-Patterns</div>
      <div>
        <h2 class="section-title">A list of things this brand will not do.</h2>
        <p class="section-kicker">
          Every premium brand is held together by what it refuses. This is Karamchari’s
          standing refusal list — for the design team, the marketing team, and any future
          agency commissioned to extend the system.
        </p>
      </div>
    </div>

    <div class="anti">
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No handshakes, no stick figures.</h4><p>Workforce branding clichés. We are about operators, not “people-people”.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No K-monograms without geometric necessity.</h4><p>The K is permitted only when it falls out of the lattice as a consequence. Never as the starting point.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No peacocks, paisleys, mandalas-as-decoration.</h4><p>Indian cultural reference is structural, never ornamental. If a motif is recognisable as “Indian decor”, it is wrong.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No Om, no devanagari ornamentation.</h4><p>The brand is rooted in Sanskritic thought, not religious iconography. The two are not the same.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No saffron-orange. No tricolour.</h4><p>Copper is the only warm accent. Saffron suggests state nationalism; copper suggests temperature and time.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No SaaS gradients.</h4><p>No purple-to-pink, no teal-to-blue. The brand’s depth comes from proportion, not from light.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No infinity loops.</h4><p>Lazy semantically, exhausted visually. The closed twin-arc of Pravaha is the only flow we sanction.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No 3D renders of the mark.</h4><p>The logo is flat geometry. Bevels, glows, and reflections will be rejected at review.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No Inter, no Roboto, no system stacks.</h4><p>Type discipline begins at the typeface choice. Generic faces produce generic brands.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No mascots, no illustration sets.</h4><p>The brand is not a personality; it is a posture. Add a mascot and the posture dissolves.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No “AI sparkle” iconography.</h4><p>Karamchari AI is a capability, not a vibe. The four-pointed star is forbidden.</p></div></div>
      <div class="anti-cell"><div class="anti-mark">×</div><div><h4>No emoji in product or marketing.</h4><p>Emoji are someone else’s typography. The brand carries its own voice.</p></div></div>
    </div>

    <div class="two-col" style="margin-top:64px;">
      <div></div>
      <div class="body-col">
        <p><strong>Why enterprise branding fails.</strong> Because most companies treat the identity as a logo and a font; the operating reality is that every form field, every email, every onboarding flow is the brand. Karamchari treats them as one system.</p>
        <p><strong>Why logos look cheap.</strong> Inconsistent stroke weights, off-grid construction, drop shadows, gratuitous gradients, and the use of motifs as substitutes for meaning.</p>
      </div>
      <div class="body-col">
        <p><strong>Why Indian-inspired branding becomes cliché.</strong> Because designers reach for the most recognisable motif (Om, lotus, mandala, paisley, peacock) instead of the underlying logic. The motif is the failure; the logic is the opportunity. Karamchari uses the logic.</p>
        <p><strong>Why brands become forgettable.</strong> Because they hedge. A brand becomes memorable when it refuses things — confidently, publicly, repeatedly. This list is that refusal.</p>
      </div>
    </div>
  </section>
</div>

<!-- ============================================================ -->
<!-- FOOTER                                                        -->
<!-- ============================================================ -->
<div class="page">
  <div class="footer">
    <div class="footer-l">
      <em>Karamchari — कर्मचारी.</em> The system for the operators of operators.
    </div>
    <div class="mono">End of document · v 1.0 · MMXXVI</div>
  </div>
</div>

</body>
</html>
