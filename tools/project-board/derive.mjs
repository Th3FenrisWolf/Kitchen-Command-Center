import { readFileSync } from "node:fs";

export const config = JSON.parse(
  readFileSync(new URL("./board.json", import.meta.url), "utf8"),
);

export const STAGES = ["Idea", "Specified", "Building", "In review", "Done"];
export const KINDS = ["Feature", "Phase", "Bug", "Idea"];
export const TEXT_FIELDS = ["Key", "Feature", "Phase"];
// Written once, when the sync creates a card, so the owner can move a card to another iteration and keep it there.
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
  const mine = pulls.filter((pull) => pull.head === head);
  return (
    mine.find((pull) => pull.state === "MERGED") ??
    mine.find((pull) => pull.state === "OPEN") ??
    null
  );
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
  const merged = pull?.state === "MERGED";
  const snapshot = pick(
    snapshots,
    merged ? ["main", branch] : [branch, "main"],
  );
  const tasks = countTasks(snapshot.tasks);
  const has = (file) => snapshot.files.includes(file);

  let stage = "Specified";
  if (merged) {
    stage = "Done";
  } else if (pull) {
    stage = "In review";
  } else if (tasks.done > 0) {
    stage = "Building";
  }

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
  return {
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
  };
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
  return {
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
  };
}

function applyPhases(specs, cards) {
  const dirByNumber = new Map(
    [...cards.keys()].map((dir) => [dir.slice(0, 3), dir]),
  );
  const features = new Map();
  for (const snapshot of specs) {
    const rows = parsePhases(snapshot.spec);
    const rootNumber = rows.find((row) => row.phase === 1)?.spec;
    if (!rootNumber || !dirByNumber.has(rootNumber)) {
      continue;
    }
    const merged = features.get(rootNumber) ?? new Map();
    for (const row of rows) {
      const known = merged.get(row.phase);
      merged.set(row.phase, {
        ...row,
        ...known,
        spec: known?.spec ?? row.spec,
      });
    }
    features.set(rootNumber, merged);
  }

  const placeholders = [];
  for (const [rootNumber, rows] of features) {
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
  const cards = new Map();
  for (const [dir, snapshots] of groupBy(specs, "dir")) {
    cards.set(dir, specCard(dir, snapshots, pulls));
  }
  const placeholders = applyPhases(specs, cards);
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

const EDITABLE = new Set(["DraftIssue", "Issue"]);

export function planChanges(cards, items) {
  const byKey = new Map();
  for (const item of items) {
    const key = item.fields.Key;
    if (key && !byKey.has(key)) {
      byKey.set(key, item);
    }
  }
  const keyByContent = new Map(
    items
      .filter((item) => item.fields.Key && item.contentId)
      .map((item) => [item.contentId, item.fields.Key]),
  );

  const ops = [];
  for (const card of cards) {
    const issue = card.fields.Status !== "Idea";
    const state = card.fields.Status === "Done" ? "CLOSED" : "OPEN";
    const parent = issue ? card.parentKey : undefined;
    const item =
      byKey.get(card.key) ??
      card.aliases.map((alias) => byKey.get(alias)).find(Boolean);
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

    const change = {};
    const editable = EDITABLE.has(item.type);
    if (editable && item.title !== card.title) {
      change.title = card.title;
    }
    const body = mergeBody(item.body, card.body);
    if (editable && body !== (item.body ?? "").replace(/\r\n/g, "\n")) {
      change.body = body;
    }
    const fields = Object.fromEntries(
      Object.entries(card.fields).filter(
        ([name, value]) => (item.fields[name] ?? "") !== value,
      ),
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
