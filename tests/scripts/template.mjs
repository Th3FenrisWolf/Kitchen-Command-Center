import { escapeHtml, fmtDuration } from "./util.mjs";

const COLORS = {
  passed: "var(--emerald)", failed: "var(--rose)", skipped: "var(--amber)",
  cancelled: "var(--slate)", timedOut: "var(--indigo)",
};

const CSS = `
:root{--bg:#0b0d11;--s1:#181c25;--s2:#1f2430;--s3:#282e3a;--text:#e2e4e9;--text2:#9ba1b0;--text3:#5f6678;--border:rgba(255,255,255,.07);--emerald:#34d399;--rose:#fb7185;--amber:#fbbf24;--slate:#94a3b8;--indigo:#818cf8;--mono:'Cascadia Code','JetBrains Mono',ui-monospace,monospace;--font:'Segoe UI Variable','Segoe UI',system-ui,sans-serif}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:var(--font);font-size:14px}
.wrap{max-width:1100px;margin:0 auto;padding:24px}
header h1{font-size:18px;margin:0 0 6px}.muted{color:var(--text2)}
.cards{display:flex;gap:12px;margin:16px 0}
.card{background:var(--s1);border:1px solid var(--border);border-radius:10px;padding:12px 16px;min-width:96px}
.card .n{font-size:22px;font-weight:600}.card .l{color:var(--text2);font-size:12px}
.tabs{display:flex;gap:4px;border-bottom:1px solid var(--border);margin:16px 0 0;flex-wrap:wrap}
.tab{background:none;border:0;color:var(--text2);padding:9px 14px;cursor:pointer;border-bottom:2px solid transparent;font-size:13px}
.tab.active{color:var(--text);border-bottom-color:var(--indigo)}
.tab .b{display:inline-block;width:7px;height:7px;border-radius:50%;margin-left:6px;vertical-align:middle}
.panel{padding:16px 0}
.chips{display:flex;gap:8px;flex-wrap:wrap;margin:8px 0}
.chip{background:var(--s2);border:1px solid var(--border);border-radius:999px;padding:3px 10px;color:var(--text2);font-size:12px}
.filters{display:flex;gap:6px;margin:12px 0}
.pill{background:var(--s1);border:1px solid var(--border);color:var(--text2);border-radius:999px;padding:5px 12px;cursor:pointer;font-size:12px}
.pill.active{color:var(--text);border-color:var(--indigo)}
.group{border:1px solid var(--border);border-radius:10px;margin:10px 0;overflow:hidden}
.ghead{display:flex;align-items:center;gap:10px;background:var(--s1);padding:9px 14px;font-weight:600;cursor:pointer;list-style:none;user-select:none}
.ghead::-webkit-details-marker{display:none}
.ghead:hover{background:var(--s2)}
.chev{color:var(--text3);font-size:10px;transition:transform .15s;flex:0 0 auto}
details[open]>.ghead .chev{transform:rotate(90deg)}
.ghead .ns{color:var(--text3);font-weight:400;font-size:12px}
.gsum{margin-left:auto;display:flex;gap:12px}
.gs{display:inline-flex;align-items:center;gap:5px;color:var(--text2);font-size:12px;font-variant-numeric:tabular-nums}
.test{display:flex;align-items:center;gap:10px;padding:7px 14px;border-top:1px solid var(--border);flex-wrap:wrap}
.dot{width:8px;height:8px;border-radius:50%;flex:0 0 auto}
.tname{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.tdur{color:var(--text3);font-size:12px;font-variant-numeric:tabular-nums}
.err{flex-basis:100%;margin:6px 0 2px}.err summary{cursor:pointer;color:var(--rose);font-size:12px}
.err pre{background:var(--s2);border:1px solid var(--border);border-radius:8px;padding:10px;overflow:auto;font-family:var(--mono);font-size:12px;white-space:pre-wrap}
.err .loc{color:var(--text3);font-family:var(--mono);font-size:11px;margin-top:4px}
.notice{background:var(--s1);border:1px dashed var(--border);border-radius:10px;padding:24px;text-align:center;color:var(--text2)}
table.roll{width:100%;border-collapse:collapse;margin-top:8px}
table.roll td,table.roll th{text-align:left;padding:8px 10px;border-bottom:1px solid var(--border);font-size:13px}
table.roll tr.jump{cursor:pointer}table.roll tr.jump:hover{background:var(--s1)}
`;

const CLIENT_JS = `
(function(){
  var tabs=[].slice.call(document.querySelectorAll('.tab'));
  var panels=[].slice.call(document.querySelectorAll('[data-panel]'));
  function show(id){tabs.forEach(function(t){t.classList.toggle('active',t.dataset.tab===id)});panels.forEach(function(p){p.hidden=(p.dataset.panel!==id)});}
  tabs.forEach(function(t){t.addEventListener('click',function(){show(t.dataset.tab)})});
  document.querySelectorAll('tr.jump').forEach(function(r){r.addEventListener('click',function(){show(r.dataset.jump)})});
  document.querySelectorAll('[data-panel] .filters').forEach(function(bar){
    bar.addEventListener('click',function(e){
      var b=e.target.closest('.pill'); if(!b) return;
      var panel=bar.closest('[data-panel]'); var f=b.dataset.filter;
      panel.querySelectorAll('.pill').forEach(function(p){p.classList.toggle('active',p===b)});
      panel.querySelectorAll('.test').forEach(function(row){row.style.display=(f==='all'||row.dataset.status===f)?'':'none'});
      panel.querySelectorAll('.group').forEach(function(g){
        var any=[].slice.call(g.querySelectorAll('.test')).some(function(r){return r.style.display!=='none'});
        g.style.display=any?'':'none';
        if(f!=='all') g.open=any;
      });
    });
  });
})();
`;

const cap = (s) => s.charAt(0).toUpperCase() + s.slice(1);
const dotColor = (s) => COLORS[s] || "var(--slate)";

function suiteBadge(suite) {
  if (suite.status !== "ok") return `<span class="b" style="background:var(--slate)"></span>`;
  if (suite.summary.failed > 0) return `<span class="b" style="background:var(--rose)"></span>`;
  return `<span class="b" style="background:var(--emerald)"></span>`;
}

function renderTest(t) {
  const loc = t.filePath ? `${t.filePath}${t.lineNumber ? ":" + t.lineNumber : ""}` : "";
  const err =
    t.status !== "passed" && t.errorMessage
      ? `<details class="err"><summary>error</summary><pre>${escapeHtml(t.errorMessage)}</pre>${loc ? `<div class="loc">${escapeHtml(loc)}</div>` : ""}</details>`
      : "";
  return `<div class="test" data-status="${escapeHtml(t.status)}"><span class="dot" style="background:${dotColor(t.status)}"></span><span class="tname">${escapeHtml(t.name)}</span><span class="tdur">${fmtDuration(t.durationMs)}</span>${err}</div>`;
}

function renderGroup(g) {
  const s = g.summary;
  const sum =
    `<span class="gsum">` +
    `<span class="gs"><span class="dot" style="background:var(--emerald)"></span>${s.passed}</span>` +
    `<span class="gs"><span class="dot" style="background:var(--rose)"></span>${s.failed}</span>` +
    `<span class="gs"><span class="dot" style="background:var(--amber)"></span>${s.skipped}</span>` +
    `</span>`;
  const open = s.failed > 0 ? " open" : "";
  return `<details class="group"${open}><summary class="ghead"><span class="chev">▸</span>${escapeHtml(g.name)}${g.namespace ? `<span class="ns">${escapeHtml(g.namespace)}</span>` : ""}${sum}</summary>${g.tests.map(renderTest).join("")}</details>`;
}

function renderChips(suite) {
  const m = suite.meta || {};
  const items = [m.framework && m.frameworkVersion ? `${m.framework} ${m.frameworkVersion}` : m.framework, m.runtimeVersion, m.machineName, m.operatingSystem].filter(Boolean);
  return items.map((c) => `<span class="chip">${escapeHtml(c)}</span>`).join("");
}

function renderFilters(suite) {
  return `<div class="filters">${["all", "passed", "failed", "skipped"]
    .map((f) => {
      const n = f === "all" ? suite.summary.total : suite.summary[f] || 0;
      return `<button class="pill${f === "all" ? " active" : ""}" data-filter="${f}">${cap(f)} <span>${n}</span></button>`;
    })
    .join("")}</div>`;
}

function renderSuitePanel(suite) {
  const body =
    suite.status !== "ok"
      ? `<div class="notice">${escapeHtml(suite.errorDetail || "No results")}</div>`
      : `<div class="muted">${suite.summary.passed} passed · ${suite.summary.failed} failed · ${suite.summary.skipped} skipped · ${fmtDuration(suite.durationMs)}</div><div class="chips">${renderChips(suite)}</div>${renderFilters(suite)}${suite.groups.map(renderGroup).join("")}`;
  return `<section class="panel" data-panel="suite-${escapeHtml(suite.id)}" hidden><h2>${escapeHtml(suite.label)}</h2>${body}</section>`;
}

function renderOverview(report) {
  const s = report.summary;
  const cards = [["total", "Total"], ["passed", "Passed"], ["failed", "Failed"], ["skipped", "Skipped"]]
    .map(([k, l]) => `<div class="card"><div class="n">${s[k]}</div><div class="l">${l}</div></div>`)
    .join("");
  const rows = report.suites
    .map((su) => `<tr class="jump" data-jump="suite-${escapeHtml(su.id)}"><td><span class="dot" style="background:${su.status !== "ok" ? "var(--slate)" : su.summary.failed ? "var(--rose)" : "var(--emerald)"}"></span> ${escapeHtml(su.label)}</td><td class="muted">${escapeHtml(su.type)}</td><td>${su.summary.passed}/${su.summary.total}</td><td style="color:var(--rose)">${su.summary.failed || ""}</td><td class="muted">${su.status === "ok" ? fmtDuration(su.durationMs) : escapeHtml(su.status)}</td></tr>`)
    .join("");
  return `<section class="panel" data-panel="overview"><div class="cards">${cards}</div><table class="roll"><thead><tr><th>Suite</th><th>Type</th><th>Pass/Total</th><th>Failed</th><th>Duration</th></tr></thead><tbody>${rows}</tbody></table></section>`;
}

export function renderHtml(report) {
  const tabbar = [`<button class="tab active" data-tab="overview">Overview</button>`]
    .concat(report.suites.map((s) => `<button class="tab" data-tab="suite-${escapeHtml(s.id)}">${escapeHtml(s.label)}${suiteBadge(s)}</button>`))
    .join("");
  const panels = [renderOverview(report)].concat(report.suites.map(renderSuitePanel)).join("\n");
  const embedded = JSON.stringify(report).replace(/</g, "\\u003c");
  const s = report.summary;
  return `<!DOCTYPE html>
<html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>Combined Test Report</title><style>${CSS}</style></head>
<body><div class="wrap">
<header><h1>Combined Test Report</h1><div class="muted">${s.passed} passed · ${s.failed} failed · ${s.skipped} skipped · generated ${escapeHtml(report.generatedAt)}</div></header>
<nav class="tabs">${tabbar}</nav>
<main>${panels}</main>
</div>
<script id="report-data" type="application/json">${embedded}</script>
<script>${CLIENT_JS}</script>
</body></html>`;
}
