import { escapeHtml } from "./util.mjs";

const SEPARATORS = /\s·\s|__|[\\/]/;

export function caseParts(name) {
  const [page, ...rest] = name.split(SEPARATORS);
  return { page, rest };
}

export function caseLabel(name) {
  return name.split(SEPARATORS).join(" · ");
}

export function pairCases(beforeNames, afterNames) {
  const before = new Set(beforeNames);
  const after = new Set(afterNames);
  return {
    paired: [...after].filter((n) => before.has(n)).sort(),
    onlyBefore: [...before].filter((n) => !after.has(n)).sort(),
    onlyAfter: [...after].filter((n) => !before.has(n)).sort(),
  };
}

export function groupByPage(names) {
  const groups = new Map();
  for (const name of [...names].sort()) {
    const { page } = caseParts(name);
    if (!groups.has(page)) groups.set(page, []);
    groups.get(page).push(name);
  }
  return groups;
}

export function diffShare(differing, total) {
  return total === 0 ? 0 : differing / total;
}

export function formatShare(share) {
  const percent = share * 100;
  if (percent === 0) return "0%";
  return percent < 0.01 ? "<0.01%" : `${percent.toFixed(percent < 1 ? 2 : 1)}%`;
}

export function slug(value) {
  return value.replace(/[^a-z0-9]+/gi, "-").replace(/^-|-$/g, "").toLowerCase();
}

export function assetPath(round, side, name) {
  return `look-${round}-assets/${side}/${name.split(SEPARATORS).map(slug).join("--")}.png`;
}

export function nextRound(existingFiles) {
  const numbers = existingFiles.map((f) => /^look-(\d+)-/.exec(f)?.[1]).filter(Boolean).map(Number);
  return numbers.length === 0 ? 1 : Math.max(...numbers) + 1;
}

export function renderRoundPage(template, { round, page, cases }) {
  const figure = (label, src) =>
    `<figure><figcaption class="kcc-label">${label}</figcaption><a href="${escapeHtml(src)}"><img src="${escapeHtml(src)}" alt="${label}" loading="lazy"></a></figure>`;
  const rows = cases
    .map(
      (c) => `    <article class="pv-case" data-testid="case">
      <h3 class="kcc-label">${escapeHtml(caseLabel(c.name))} <span data-testid="diff-share">${formatShare(c.share)} of pixels differ</span></h3>
      <div class="pv-trio">${figure("Before", c.before)}${figure("After", c.after)}${figure("Heat map", c.heat)}</div>
    </article>`,
    )
    .join("\n");
  const title = `Look round ${round}: ${page}`;
  const section = `  <section class="pv-option" id="${escapeHtml(slug(page))}">
    <header>
      <p class="kcc-kick">Round ${round} · page type</p>
      <h2>${escapeHtml(page)}</h2>
      <p class="kcc-body">Before, after and heat map for each case. The share is a pointer for the eye, never a verdict.</p>
    </header>
${rows}
  </section>`;
  const style = `  .pv-case { margin-bottom: 36px; }
  .pv-trio { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
  .pv-trio figure { margin: 0; }
  .pv-trio img { width: 100%; height: auto; display: block; }
</style>`;
  return template
    .replace(/<title>[^<]*<\/title>/, `<title>${escapeHtml(title)}</title>`)
    .replace("</style>", style)
    .replace(/<h1>Round title<\/h1>/, `<h1>${escapeHtml(title)}</h1>`)
    .replace(/<nav aria-label="Options">[\s\S]*?<\/nav>/, `<nav aria-label="Options"><a class="kcc-link kcc-kick" href="#${escapeHtml(slug(page))}">${escapeHtml(page)}</a></nav>`)
    .replace(/  <section class="pv-option" id="direction">[\s\S]*?<\/section>/, section);
}

export const SCRIPTS_ON = "__scripts-on";
export const SCRIPTS_OFF = "__scripts-off";

export function scriptsOffPairs(names) {
  const all = new Set(names);
  const pairs = [];
  const unpaired = [];
  for (const name of [...all].sort()) {
    if (name.endsWith(SCRIPTS_ON)) {
      const base = name.slice(0, -SCRIPTS_ON.length);
      if (all.has(base + SCRIPTS_OFF)) pairs.push({ name: base, on: name, off: base + SCRIPTS_OFF });
      else unpaired.push(name);
    } else if (name.endsWith(SCRIPTS_OFF) && !all.has(name.slice(0, -SCRIPTS_OFF.length) + SCRIPTS_ON)) {
      unpaired.push(name);
    }
  }
  return { pairs, unpaired };
}

export function comparePixels(on, off) {
  if (on.width !== off.width || on.height !== off.height) return { sameSize: false, differing: 0, maxDelta: 0 };
  let differing = 0;
  let maxDelta = 0;
  for (let i = 0; i < on.data.length; i += 4) {
    let delta = 0;
    for (let k = 0; k < 4; k++) delta = Math.max(delta, Math.abs(on.data[i + k] - off.data[i + k]));
    if (delta > 0) differing++;
    maxDelta = Math.max(maxDelta, delta);
  }
  return { sameSize: true, differing, maxDelta };
}

export function scriptsOffPasses(result) {
  return result.sameSize && result.differing === 0;
}
