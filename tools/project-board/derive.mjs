import { readFileSync } from "node:fs";

export const config = JSON.parse(
  readFileSync(new URL("./board.json", import.meta.url), "utf8"),
);

export const STAGES = ["Idea", "Specified", "Building", "In review", "Done"];
export const KINDS = ["Feature", "Phase", "Bug", "Idea"];
export const TEXT_FIELDS = ["Key", "Feature", "Phase"];
// Written when the sync creates a card or finds an issue card's Iteration blank, never over a set one, so the owner
// can move a card to another iteration and keep it there.
export const CREATE_FIELDS = { Iteration: "Current" };
export const ITERATIONS = ["Current", "Future"];

const SYNC_START = "<!-- sync -->";
const SYNC_END = "<!-- /sync -->";

export function statusLink(key) {
  return `[On the project board](https://github.com/users/${config.owner}/projects/${config.project}?filterQuery=key%3A${key})`;
}

export function syncedBody(text) {
  return `${SYNC_START}\n${text}\n${SYNC_END}`;
}

export function parsePhases(markdown) {
  const lines = markdown.split("\n");
  const header = lines.findIndex((line) =>
    /^\|\s*Phase\s*\|\s*Spec\s*\|\s*Covers\s*\|/i.test(line),
  );
  if (header === -1) {
    return [];
  }
  const rows = [];
  for (const line of lines.slice(header + 2)) {
    if (!line.trim().startsWith("|")) {
      break;
    }
    const [phase = "", spec = "", covers = ""] = line
      .split("|")
      .slice(1, -1)
      .map((cell) => cell.trim());
    const number = Number.parseInt(phase, 10);
    if (Number.isNaN(number) || !spec) {
      continue;
    }
    rows.push({
      phase: number,
      spec: spec.match(/\b(\d{3})\b/)?.[1] ?? null,
      covers,
    });
  }
  return rows;
}

export function parseIdeaLine(markdown) {
  const header = markdown.split(/^## /m)[0];
  const number = header.match(/^(?:- )?\*\*Idea\*\*:\s*\[?#(\d+)/m)?.[1];
  return number === undefined ? null : Number.parseInt(number, 10);
}

function withIdea(card, markdown) {
  const idea = parseIdeaLine(markdown ?? "");
  return idea === null ? card : { ...card, idea };
}

function heading(markdown, prefix) {
  const line = markdown.split("\n").find((text) => text.startsWith("# ")) ?? "";
  return line.slice(2).replace(prefix, "").trim();
}

function featureName(title) {
  return title.replace(/,?\s*Phase\s+\d+.*$/i, "").trim();
}

function countTasks(tasks) {
  const marks = [...(tasks ?? "").matchAll(/^\s*- \[([ xX])\]/gm)].map(
    (match) => match[1],
  );
  return {
    done: marks.filter((mark) => mark !== " ").length,
    total: marks.length,
  };
}

function pullFor(pulls, head) {
  const latest = (state) =>
    pulls
      .filter((pull) => pull.head === head && pull.state === state)
      .sort((a, b) => b.number - a.number)[0];
  return latest("OPEN") ?? latest("MERGED") ?? null;
}

function blob(ref, path) {
  return `https://github.com/${config.owner}/${config.repo}/blob/${ref}/${path}`;
}

function pick(snapshots, preferred) {
  for (const ref of preferred) {
    const found = snapshots.find((snapshot) => snapshot.ref === ref);
    if (found) {
      return found;
    }
  }
  return snapshots[0];
}

function groupBy(items, key) {
  const groups = new Map();
  for (const item of items) {
    groups.set(item[key], [...(groups.get(item[key]) ?? []), item]);
  }
  return groups;
}

function specCard(dir, snapshots, pulls) {
  const branch =
    snapshots[0].spec.match(/\*\*Feature Branch\*\*:\s*`([^`]+)`/)?.[1] ?? dir;
  const pull = pullFor(pulls, branch);
  const open = pull?.state === "OPEN";
  // A spec merges from the branch its build later reuses, so only a ticked task tells a build from a spec.
  const working = countTasks(pick(snapshots, [branch, "main"]).tasks);
  const landed = countTasks(
    snapshots.find((candidate) => candidate.ref === "main")?.tasks,
  );
  const mergedOnce = pulls.some(
    (candidate) => candidate.head === branch && candidate.state === "MERGED",
  );

  let stage = "Specified";
  if (open && working.done > 0) {
    stage = "In review";
  } else if (mergedOnce && landed.done > 0) {
    stage = "Done";
  } else if (working.done > 0) {
    stage = "Building";
  }

  const settled = stage === "Done" || (stage === "Specified" && mergedOnce);
  const snapshot = pick(
    snapshots,
    settled ? ["main", branch] : [branch, "main"],
  );
  const tasks = countTasks(snapshot.tasks);
  const has = (file) => snapshot.files.includes(file);

  const title = heading(snapshot.spec, "Feature Specification:");
  const links = [
    `[Spec](${blob(snapshot.ref, `specs/${dir}/spec.md`)})`,
    ...(has("plan.md")
      ? [`[Plan](${blob(snapshot.ref, `specs/${dir}/plan.md`)})`]
      : []),
    ...(has("tasks.md") ? [`Tasks ${tasks.done}/${tasks.total}`] : []),
    `Branch \`${branch}\``,
    ...(pull ? [`PR [#${pull.number}](${pull.url})`] : []),
  ];
  return withIdea(
    {
      key: dir,
      aliases: [],
      title: `${dir.slice(0, 3)} · ${title}`,
      body: links.join(" · "),
      specTitle: title,
      ref: snapshot.ref,
      fields: {
        Key: dir,
        Status: stage,
        Kind: "Feature",
        Feature: `${dir.slice(0, 3)} ${title}`,
        Phase: "",
      },
    },
    snapshot.spec,
  );
}

function bugCard(slug, snapshots, pulls) {
  const pull = pullFor(pulls, `fix/${slug}`);
  const withFix = snapshots.filter((candidate) =>
    candidate.files.includes("fix.md"),
  );
  const snapshot = pick(withFix.length > 0 ? withFix : snapshots, [
    "main",
    `fix/${slug}`,
  ]);
  const fixed = withFix.length > 0;

  let stage = "Specified";
  if (pull?.state === "MERGED" || (snapshot.ref === "main" && fixed)) {
    stage = "Done";
  } else if (pull) {
    stage = "In review";
  } else if (fixed) {
    stage = "Building";
  }

  const folder = `.specify/bugs/${slug}`;
  const links = [
    `[Assessment](${blob(snapshot.ref, `${folder}/assessment.md`)})`,
    ...(fixed ? [`[Fix](${blob(snapshot.ref, `${folder}/fix.md`)})`] : []),
    ...(pull ? [`PR [#${pull.number}](${pull.url})`] : []),
  ];
  return withIdea(
    {
      key: `bug/${slug}`,
      aliases: [],
      title: `Bug · ${heading(snapshot.assessment ?? "", "Bug Assessment:") || slug}`,
      body: links.join(" · "),
      fields: {
        Key: `bug/${slug}`,
        Status: stage,
        Kind: "Bug",
        Feature: "",
        Phase: "",
      },
    },
    snapshot.assessment,
  );
}

function numberOf(dir) {
  return dir.slice(0, 3);
}

export function numberClashes(specs) {
  const foldersByNumber = new Map();
  for (const { dir, ref } of specs) {
    const folders = foldersByNumber.get(numberOf(dir)) ?? new Map();
    folders.set(dir, new Set([...(folders.get(dir) ?? []), ref]));
    foldersByNumber.set(numberOf(dir), folders);
  }
  return [...foldersByNumber]
    .filter(([, folders]) => folders.size > 1)
    .map(([number, folders]) => ({
      number,
      folders: [...folders]
        .map(([dir, refs]) => ({ dir, refs: [...refs].sort() }))
        .sort((a, b) => a.dir.localeCompare(b.dir)),
    }))
    .sort((a, b) => a.number.localeCompare(b.number));
}

export function clashReport(clashes, cards) {
  const held = cards.filter((card) => card.held).map((card) => card.key);
  return [
    ...clashes.map(
      ({ number, folders }) =>
        `Spec number ${number} names more than one folder: ${folders
          .map(({ dir, refs }) => `${dir} on ${refs.join(", ")}`)
          .join("; ")}.`,
    ),
    `Renumber all but one of them. Until then the sync leaves these cards as they are: ${held.join(", ")}.`,
  ].join("\n");
}

function applyPhases(specs, cards, clashing) {
  const dirByNumber = new Map(
    [...cards.keys()]
      .filter((dir) => !clashing.has(numberOf(dir)))
      .map((dir) => [numberOf(dir), dir]),
  );
  const features = new Map();
  const named = new Map();
  for (const snapshot of specs) {
    const rows = parsePhases(snapshot.spec);
    const rootNumber = rows.find((row) => row.phase === 1)?.spec;
    if (
      !rootNumber ||
      !(dirByNumber.has(rootNumber) || clashing.has(rootNumber))
    ) {
      continue;
    }
    const numbers = named.get(rootNumber) ?? new Set();
    const merged = features.get(rootNumber) ?? new Map();
    for (const row of rows) {
      if (row.spec) {
        numbers.add(row.spec);
      }
      const known = merged.get(row.phase);
      merged.set(row.phase, {
        ...row,
        ...known,
        spec: known?.spec ?? row.spec,
      });
    }
    features.set(rootNumber, merged);
    named.set(rootNumber, numbers);
  }

  const placeholders = [];
  for (const [rootNumber, rows] of features) {
    // Every branch's table counts here, not just the merged rows, so a clash named only by a table the merge dropped
    // still holds the feature.
    const numbers = [...named.get(rootNumber)];
    if (numbers.some((number) => clashing.has(number))) {
      for (const number of numbers) {
        const card = cards.get(dirByNumber.get(number));
        if (card) {
          card.held = true;
        }
      }
      continue;
    }
    const rootDir = dirByNumber.get(rootNumber);
    const root = cards.get(rootDir);
    const feature = `${rootNumber} ${featureName(root.specTitle)}`;
    const total = Math.max(...rows.keys());
    for (const row of rows.values()) {
      const fields = {
        Kind: row.phase === 1 ? "Feature" : "Phase",
        Feature: feature,
        Phase: `${row.phase} of ${total}`,
      };
      const placeholderKey = `${rootDir}/phase-${row.phase}`;
      const card = row.spec ? cards.get(dirByNumber.get(row.spec)) : undefined;
      if (card) {
        Object.assign(card.fields, fields);
        if (row.phase > 1) {
          card.aliases = [placeholderKey];
          card.parentKey = rootDir;
        }
      } else if (row.phase > 1) {
        placeholders.push({
          key: placeholderKey,
          aliases: [],
          title: `${rootNumber} · Phase ${row.phase}: ${row.covers}`,
          body: `Phase ${row.phase} of [${rootDir}](${blob(root.ref, `specs/${rootDir}/spec.md`)}) has no spec yet.`,
          fields: { Key: placeholderKey, Status: "Idea", ...fields },
        });
      }
    }
  }
  return placeholders;
}

export function deriveCards({ specs, bugs, pulls }) {
  const clashing = new Set(numberClashes(specs).map(({ number }) => number));
  const cards = new Map();
  for (const [dir, snapshots] of groupBy(specs, "dir")) {
    const card = specCard(dir, snapshots, pulls);
    if (clashing.has(numberOf(dir))) {
      card.held = true;
    }
    cards.set(dir, card);
  }
  const placeholders = applyPhases(specs, cards, clashing);
  const bugCards = [...groupBy(bugs, "slug")].map(([slug, snapshots]) =>
    bugCard(slug, snapshots, pulls),
  );
  return [...cards.values(), ...placeholders, ...bugCards]
    .map(({ specTitle, ref, ...card }) => card)
    .sort((a, b) => a.key.localeCompare(b.key));
}

function mergeBody(existing, text) {
  const block = syncedBody(text);
  const current = (existing ?? "").replace(/\r\n/g, "\n");
  const start = current.indexOf(SYNC_START);
  const end = current.indexOf(SYNC_END);
  if (start !== -1 && end > start) {
    return (
      current.slice(0, start) + block + current.slice(end + SYNC_END.length)
    );
  }
  return current.trim() ? `${block}\n\n${current.trim()}` : block;
}

export function schemaProblems(fields, cards) {
  const problems = new Set();
  for (const card of cards) {
    for (const [name, value] of Object.entries({
      ...card.fields,
      ...CREATE_FIELDS,
    })) {
      const field = fields.get(name);
      if (!field) {
        problems.add(`The project has no ${name} field`);
      } else if (
        field.dataType === "SINGLE_SELECT" &&
        !field.options.some((option) => option.name === value)
      ) {
        problems.add(`The ${name} field has no ${value} option`);
      }
    }
  }
  return [...problems];
}

export function boardItemOf(issue, projectId) {
  return (
    issue?.projectItems.nodes.find((item) => item.project.id === projectId)
      ?.id ?? null
  );
}

const ITEM_PAUSES_MS = [1000, 2000, 4000, 8000];

// createIssue can answer before GitHub has put the issue on the board named in projectV2Ids, so the item may show only
// on a later read of the issue.
export function waitForBoardItem(issue, projectId, reread, pause) {
  let itemId = boardItemOf(issue, projectId);
  for (const ms of ITEM_PAUSES_MS) {
    if (itemId) {
      return itemId;
    }
    pause(ms);
    itemId = boardItemOf(tryRead(reread, issue.id), projectId);
  }
  return itemId;
}

// The issue already exists, so a failed read must not end the run here: the caller's add then says which Key to set
// by hand, where a bare read error would leave a Key-less issue for the next run to duplicate.
function tryRead(reread, issueId) {
  try {
    return reread(issueId);
  } catch (error) {
    console.log(
      `warning: reading the board items of issue ${issueId} failed: ${error.message}`,
    );
    return null;
  }
}

export function emptyReadError(specs, bugs) {
  return specs.length > 0 || bugs.length > 0
    ? null
    : "the read found no spec and no bug on any origin branch, so the sync writes nothing: run git fetch and try again";
}

export function strayKeys(cards, items) {
  const known = new Set(cards.flatMap((card) => [card.key, ...card.aliases]));
  const heldPrefixes = cards
    .filter((card) => card.held)
    .map((card) => `${card.key}/`);
  return items
    .filter((item) => !item.archived)
    .map((item) => item.fields.Key)
    .filter(
      (key) =>
        key &&
        !known.has(key) &&
        !heldPrefixes.some((prefix) => key.startsWith(prefix)),
    );
}

const EDITABLE = new Set(["DraftIssue", "Issue"]);

export function indexByKey(items) {
  const byKey = new Map();
  for (const item of items) {
    const key = item.fields.Key;
    const kept = byKey.get(key);
    if (key && (!kept || (kept.archived && !item.archived))) {
      byKey.set(key, item);
    }
  }
  return byKey;
}

export function matchItem(byKey, card) {
  return (
    byKey.get(card.key) ??
    card.aliases.map((alias) => byKey.get(alias)).find(Boolean)
  );
}

export const BUILDING_LABEL = "building";

export function pickupStage(stage, item) {
  const label =
    item.type === "Issue"
      ? item.labels?.find((candidate) => candidate.name === BUILDING_LABEL)
      : undefined;
  if (!label || stage === "Idea") {
    return { stage };
  }
  return stage === "Specified"
    ? { stage: "Building" }
    : { stage, unlabel: label.id };
}

export function planChanges(cards, items, claims = new Map()) {
  const byKey = indexByKey(items);
  const keyByContent = new Map(
    items
      .filter((item) => item.fields.Key && item.contentId)
      .map((item) => [item.contentId, item.fields.Key]),
  );

  const ops = [];
  for (const card of cards.filter((candidate) => !candidate.held)) {
    const issue = card.fields.Status !== "Idea";
    const state = card.fields.Status === "Done" ? "CLOSED" : "OPEN";
    const parent = issue ? card.parentKey : undefined;
    const claim = claims.get(card.key);
    const item = matchItem(byKey, card) ?? claim?.item;
    if (!item) {
      ops.push({
        op: "create",
        card,
        as: issue ? "issue" : "draft",
        ...(issue && state === "CLOSED" ? { state } : {}),
        ...(parent ? { parent } : {}),
      });
      continue;
    }
    if (item.archived && !claim) {
      continue;
    }

    const change = {};
    if (claim) {
      change.takes = claim.issue.number;
      if (item.archived) {
        change.unarchive = true;
      }
      if (!item.id) {
        change.adopt = true;
      }
    }
    const editable = EDITABLE.has(item.type);
    if (editable && item.title !== card.title) {
      change.title = card.title;
    }
    const body = mergeBody(item.body, card.body);
    if (editable && body !== (item.body ?? "").replace(/\r\n/g, "\n")) {
      change.body = body;
    }
    const { stage, unlabel } = pickupStage(card.fields.Status, item);
    if (unlabel) {
      change.unlabel = unlabel;
    }
    const fields = Object.fromEntries(
      Object.entries({
        ...card.fields,
        Status: stage,
        ...((claim || item.type === "Issue") && !item.fields.Iteration
          ? CREATE_FIELDS
          : {}),
      }).filter(([name, value]) => (item.fields[name] ?? "") !== value),
    );
    if (Object.keys(fields).length > 0) {
      change.fields = fields;
    }
    const convert = issue && item.type === "DraftIssue";
    if (convert) {
      change.convert = true;
    }
    if (issue && (convert || item.type === "Issue")) {
      if ((convert ? "OPEN" : item.state) !== state) {
        change.state = state;
      }
      if (parent && keyByContent.get(item.parentId) !== parent) {
        change.parent = parent;
      }
    }
    if (Object.keys(change).length > 0) {
      ops.push({
        op: "update",
        itemId: item.id,
        type: item.type,
        contentId: item.contentId,
        card,
        ...change,
      });
    }
  }
  return ops;
}

function adoptItem(issue) {
  return {
    type: "Issue",
    contentId: issue.id,
    state: issue.state,
    body: issue.body,
    fields: {},
  };
}

export function claimIdeas(cards, issues, items) {
  const byKey = indexByKey(items);
  const ideas = new Map(
    ownerIdeas(issues, items).map((idea) => [idea.issue.number, idea]),
  );
  const claims = new Map();
  const warnings = [];
  const seekers = new Map();
  for (const card of cards.filter((candidate) => !candidate.held)) {
    if (card.idea === undefined) {
      continue;
    }
    const matched = matchItem(byKey, card);
    if (matched) {
      const named = issues.find((issue) => issue.number === card.idea);
      if (!named || matched.contentId !== named.id) {
        warnings.push(
          `${card.key} is on the board as another item than #${card.idea}`,
        );
      }
    } else if (ideas.has(card.idea)) {
      seekers.set(card.idea, [...(seekers.get(card.idea) ?? []), card.key]);
    } else {
      const named = issues.find((issue) => issue.number === card.idea);
      const holder = named
        ? items.find((item) => item.contentId === named.id && item.fields.Key)
        : undefined;
      warnings.push(
        holder
          ? `${card.key} names #${card.idea}, which ${holder.fields.Key} already holds`
          : `${card.key} names #${card.idea}, which is not an idea`,
      );
    }
  }
  for (const [number, keys] of seekers) {
    const [winner, ...losers] = keys.sort((a, b) => a.localeCompare(b));
    const { issue, item } = ideas.get(number);
    claims.set(winner, { issue, item: item ?? adoptItem(issue) });
    for (const key of losers) {
      warnings.push(`${key} names #${number}, which ${winner} takes over`);
    }
  }
  return { claims, warnings };
}

const IDEA_FIELDS = { Status: "Idea", Kind: "Idea", Iteration: "Current" };

export function ownerIdeas(issues, items) {
  const itemByContent = new Map();
  for (const item of items) {
    const kept = itemByContent.get(item.contentId);
    if (item.contentId && (!kept || (kept.archived && !item.archived))) {
      itemByContent.set(item.contentId, item);
    }
  }
  return issues
    .map((issue) => ({ issue, item: itemByContent.get(issue.id) ?? null }))
    .filter(({ issue, item }) => !issue.synced && !item?.fields.Key);
}

function blankIdeaFields(item) {
  return Object.fromEntries(
    Object.entries(IDEA_FIELDS).filter(
      ([name]) => (item?.fields[name] ?? "") === "",
    ),
  );
}

export function planIdeas(ideas, claimed = new Set()) {
  const ops = [];
  for (const { issue, item } of ideas) {
    if (claimed.has(issue.number)) {
      continue;
    }
    const base = { op: "idea", issue: issue.number, contentId: issue.id };
    if (issue.state !== "OPEN") {
      if (item && !item.archived) {
        ops.push({ ...base, itemId: item.id, archive: true });
      }
      continue;
    }
    const fields = blankIdeaFields(item);
    if (!item) {
      ops.push({ ...base, add: true, fields });
    } else if (item.archived) {
      ops.push({
        ...base,
        itemId: item.id,
        restore: true,
        ...(Object.keys(fields).length > 0 ? { fields } : {}),
      });
    } else if (Object.keys(fields).length > 0) {
      ops.push({ ...base, itemId: item.id, fields });
    }
  }
  return ops;
}

export function describeOp(op) {
  if (op.op === "create") {
    return `create ${op.card.key} as ${op.as}: ${op.card.fields.Status}${op.parent ? `, under ${op.parent}` : ""}`;
  }
  const fields = Object.entries(op.fields ?? {}).map(
    ([name, value]) => `${name}=${value || "(empty)"}`,
  );
  if (op.op === "idea") {
    const verb = op.archive
      ? "archive"
      : op.restore
        ? "restore"
        : op.add
          ? "add"
          : "fill";
    return `idea #${op.issue}: ${[verb, ...fields].join(", ")}`;
  }
  const parts = [
    ...(op.convert ? ["to an issue"] : []),
    ...(op.title !== undefined ? ["title"] : []),
    ...(op.body !== undefined ? ["body"] : []),
    ...fields,
    ...(op.state ? [op.state === "CLOSED" ? "close" : "reopen"] : []),
    ...(op.parent ? [`under ${op.parent}`] : []),
    ...(op.takes ? [`takes over #${op.takes}`] : []),
    ...(op.unlabel ? [`unlabel ${BUILDING_LABEL}`] : []),
  ];
  return `update ${op.card.key}: ${parts.join(", ")}`;
}

export function ideaArgumentError(arg) {
  if (arg === undefined) {
    return "no issue number given";
  }
  return /^\d+$/.test(arg) ? null : `${arg} is not an issue number`;
}

export function ideaViewerError(login) {
  return login === config.owner
    ? null
    : `gh is signed in as ${login}, not ${config.owner}: run gh auth switch -u ${config.owner}`;
}

const CARD_KEY =
  /^(?:\d{3}-[a-z0-9]+(?:-[a-z0-9]+)*|bug\/[a-z0-9]+(?:-[a-z0-9]+)*)$/;

export function pickupArgumentError(arg) {
  if (arg === undefined) {
    return "no card key given";
  }
  return CARD_KEY.test(arg) ? null : `${arg} is not a card key`;
}

export function pickupTarget(key, items) {
  const item = indexByKey(items).get(key);
  if (!item) {
    return { key, refused: `no card on the board carries the Key ${key}` };
  }
  if (item.type === "DraftIssue") {
    return { key, refused: `${key}'s card is a draft, not an issue` };
  }
  if (item.type !== "Issue") {
    return { key, refused: `${key}'s card is not an issue` };
  }
  if (item.archived) {
    return { key, refused: `${key}'s card is archived` };
  }
  if (item.state !== "OPEN") {
    return { key, refused: `${key}'s issue is closed` };
  }
  return {
    key,
    number: item.number,
    repository: `${config.owner}/${config.repo}`,
  };
}

export function ideaRefusal(number, issue) {
  if (!issue) {
    return `#${number} does not exist in this repository`;
  }
  if (issue.author !== config.owner) {
    return `#${number} was opened by another account`;
  }
  if (issue.key) {
    return `#${number} already carries the Key ${issue.key}`;
  }
  if (issue.synced) {
    return `#${number} is a board card the sync made`;
  }
  return null;
}

export function ideaGateOverflow(number, projectItems) {
  return projectItems.pageInfo.hasNextPage
    ? `#${number} is on more boards than the gate reads, so its Key on this board cannot be checked`
    : null;
}

export const IDEA_GATE_QUERY = `query($owner: String!, $repo: String!, $number: Int!) {
  repository(owner: $owner, name: $repo) {
    issue(number: $number) {
      author { login }
      projectItems(includeArchived: true, first: 20) {
        pageInfo { hasNextPage }
        nodes {
          project { id }
          fieldValueByName(name: "Key") {
            ... on ProjectV2ItemFieldTextValue { text }
          }
        }
      }
    }
  }
}`;

export const IDEA_TEXT_QUERY = `query($owner: String!, $repo: String!, $number: Int!) {
  repository(owner: $owner, name: $repo) {
    issue(number: $number) {
      title
      body
      url
    }
  }
}`;
