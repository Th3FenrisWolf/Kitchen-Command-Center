// Syncs the project board from the specs, the bug folders and the pull requests:
//   node tools/project-board/sync.mjs [--dry-run] [--setup]
// It reads every branch on origin, so fetch first. The gh CLI supplies the token: locally the signed-in account,
// in the workflow GH_TOKEN.
import { execFileSync } from "node:child_process";
import {
  boardItemOf,
  config,
  CREATE_FIELDS,
  deriveCards,
  ITERATIONS,
  KINDS,
  planChanges,
  schemaProblems,
  STAGES,
  syncedBody,
  TEXT_FIELDS,
} from "./derive.mjs";

const dryRun = process.argv.includes("--dry-run");
const setup = process.argv.includes("--setup");

const STAGE_COLORS = {
  Idea: "GRAY",
  Specified: "BLUE",
  Building: "YELLOW",
  "In review": "ORANGE",
  Done: "GREEN",
};
const ITERATION_COLORS = { Current: "GREEN", Future: "GRAY" };
const KIND_COLORS = {
  Feature: "BLUE",
  Phase: "PURPLE",
  Bug: "RED",
  Idea: "GRAY",
};

function git(...args) {
  return execFileSync("git", args, {
    encoding: "utf8",
    maxBuffer: 64 * 1024 * 1024,
  });
}

function graphql(query, variables = {}) {
  const output = execFileSync("gh", ["api", "graphql", "--input", "-"], {
    input: JSON.stringify({ query, variables }),
    encoding: "utf8",
    maxBuffer: 64 * 1024 * 1024,
  });
  const result = JSON.parse(output);
  if (result.errors) {
    throw new Error(JSON.stringify(result.errors, null, 2));
  }
  return result.data;
}

function readSnapshots() {
  const refs = git(
    "for-each-ref",
    "--format=%(refname:short)",
    "refs/remotes/origin",
  )
    .split("\n")
    .filter((ref) => ref.startsWith("origin/") && ref !== "origin/HEAD");
  const specs = [];
  const bugs = [];
  for (const remoteRef of refs) {
    const ref = remoteRef.slice("origin/".length);
    const paths = git(
      "ls-tree",
      "-r",
      "--name-only",
      remoteRef,
      "--",
      "specs",
      ".specify/bugs",
    ).split("\n");
    const show = (path) =>
      paths.includes(path) ? git("show", `${remoteRef}:${path}`) : null;
    const filesUnder = (folder) =>
      paths
        .filter((path) => path.startsWith(`${folder}/`))
        .map((path) => path.slice(folder.length + 1));

    const specDirs = new Set(
      paths
        .map((path) => path.match(/^specs\/(\d{3}-[^/]+)\/spec\.md$/)?.[1])
        .filter(Boolean),
    );
    for (const dir of specDirs) {
      const folder = `specs/${dir}`;
      specs.push({
        dir,
        ref,
        files: filesUnder(folder),
        spec: show(`${folder}/spec.md`),
        tasks: show(`${folder}/tasks.md`),
      });
    }

    const bugSlugs = new Set(
      paths
        .map(
          (path) =>
            path.match(/^\.specify\/bugs\/([^/]+)\/assessment\.md$/)?.[1],
        )
        .filter(Boolean),
    );
    for (const slug of bugSlugs) {
      const folder = `.specify/bugs/${slug}`;
      bugs.push({
        slug,
        ref,
        files: filesUnder(folder),
        assessment: show(`${folder}/assessment.md`),
      });
    }
  }
  return { specs, bugs };
}

function readPulls() {
  const pulls = [];
  let after = null;
  do {
    const { repository } = graphql(
      `
        query ($owner: String!, $repo: String!, $after: String) {
          repository(owner: $owner, name: $repo) {
            pullRequests(first: 100, after: $after) {
              nodes {
                number
                url
                headRefName
                state
              }
              pageInfo {
                hasNextPage
                endCursor
              }
            }
          }
        }
      `,
      { owner: config.owner, repo: config.repo, after },
    );
    const page = repository.pullRequests;
    pulls.push(
      ...page.nodes.map(({ headRefName, ...pull }) => ({
        ...pull,
        head: headRefName,
      })),
    );
    after = page.pageInfo.hasNextPage ? page.pageInfo.endCursor : null;
  } while (after);
  return pulls;
}

function readProject() {
  const fieldsQuery = `
    fields(first: 50) {
      nodes {
        ... on ProjectV2FieldCommon { id name dataType }
        ... on ProjectV2SingleSelectField { options { id name } }
      }
    }`;
  let project = null;
  const items = [];
  let after = null;
  do {
    const { user } = graphql(
      `query($owner: String!, $number: Int!, $after: String) {
        user(login: $owner) {
          projectV2(number: $number) {
            id
            ${fieldsQuery}
            items(first: 100, after: $after) {
              nodes {
                id
                content {
                  __typename
                  ... on DraftIssue { id title body }
                  ... on Issue { id title body state parent { id } }
                  ... on PullRequest { id }
                }
                fieldValues(first: 20) {
                  nodes {
                    ... on ProjectV2ItemFieldTextValue { text field { ... on ProjectV2FieldCommon { name } } }
                    ... on ProjectV2ItemFieldSingleSelectValue { name field { ... on ProjectV2FieldCommon { name } } }
                  }
                }
              }
              pageInfo { hasNextPage endCursor }
            }
          }
        }
      }`,
      { owner: config.owner, number: config.project, after },
    );
    project ??= user.projectV2;
    const page = user.projectV2.items;
    for (const node of page.nodes) {
      const fields = {};
      for (const value of node.fieldValues.nodes) {
        if (value.field) {
          fields[value.field.name] = value.text ?? value.name ?? "";
        }
      }
      items.push({
        id: node.id,
        type: node.content?.__typename,
        contentId: node.content?.id,
        title: node.content?.title,
        body: node.content?.body,
        state: node.content?.state,
        parentId: node.content?.parent?.id ?? null,
        fields,
      });
    }
    after = page.pageInfo.hasNextPage ? page.pageInfo.endCursor : null;
  } while (after);
  return {
    id: project.id,
    fields: new Map(project.fields.nodes.map((field) => [field.name, field])),
    items,
  };
}

// An option sent without its id is recreated, which clears it from every card that had it.
function selectOptions(names, colors, existing = []) {
  return names.map((name) => ({
    ...(existing.find((option) => option.name === name)
      ? { id: existing.find((option) => option.name === name).id }
      : {}),
    name,
    color: colors[name],
    description: "",
  }));
}

function ensureFields(project) {
  const status = project.fields.get("Status");
  const stageNames = status?.options.map((option) => option.name) ?? [];
  if (stageNames.join("|") !== STAGES.join("|")) {
    console.log("Setting the Status options");
    graphql(
      `
        mutation (
          $field: ID!
          $options: [ProjectV2SingleSelectFieldOptionInput!]
        ) {
          updateProjectV2Field(
            input: { fieldId: $field, singleSelectOptions: $options }
          ) {
            clientMutationId
          }
        }
      `,
      {
        field: status.id,
        options: selectOptions(STAGES, STAGE_COLORS, status.options),
      },
    );
  }
  const wanted = [
    {
      name: "Kind",
      dataType: "SINGLE_SELECT",
      singleSelectOptions: selectOptions(KINDS, KIND_COLORS),
    },
    {
      name: "Iteration",
      dataType: "SINGLE_SELECT",
      singleSelectOptions: selectOptions(ITERATIONS, ITERATION_COLORS),
    },
    ...TEXT_FIELDS.map((name) => ({ name, dataType: "TEXT" })),
  ];
  for (const field of wanted.filter(({ name }) => !project.fields.has(name))) {
    console.log(`Creating the ${field.name} field`);
    graphql(
      `
        mutation ($input: CreateProjectV2FieldInput!) {
          createProjectV2Field(input: $input) {
            clientMutationId
          }
        }
      `,
      { input: { projectId: project.id, ...field } },
    );
  }
}

function setField(project, itemId, name, value) {
  const field = project.fields.get(name);
  if (!field) {
    throw new Error(`The project has no ${name} field: run with --setup`);
  }
  if (field.dataType === "SINGLE_SELECT") {
    const option = field.options.find((candidate) => candidate.name === value);
    if (!option) {
      throw new Error(
        `The ${name} field has no ${value} option: run with --setup`,
      );
    }
    graphql(
      `
        mutation ($project: ID!, $item: ID!, $field: ID!, $option: String!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $project
              itemId: $item
              fieldId: $field
              value: { singleSelectOptionId: $option }
            }
          ) {
            clientMutationId
          }
        }
      `,
      { project: project.id, item: itemId, field: field.id, option: option.id },
    );
  } else if (value) {
    graphql(
      `
        mutation ($project: ID!, $item: ID!, $field: ID!, $text: String!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $project
              itemId: $item
              fieldId: $field
              value: { text: $text }
            }
          ) {
            clientMutationId
          }
        }
      `,
      { project: project.id, item: itemId, field: field.id, text: value },
    );
  } else {
    graphql(
      `
        mutation ($project: ID!, $item: ID!, $field: ID!) {
          clearProjectV2ItemFieldValue(
            input: { projectId: $project, itemId: $item, fieldId: $field }
          ) {
            clientMutationId
          }
        }
      `,
      { project: project.id, item: itemId, field: field.id },
    );
  }
}

function readRepositoryId() {
  return graphql(
    `
      query ($owner: String!, $repo: String!) {
        repository(owner: $owner, name: $repo) {
          id
        }
      }
    `,
    { owner: config.owner, repo: config.repo },
  ).repository.id;
}

function setIssueState(issueId, state) {
  if (state === "CLOSED") {
    graphql(
      `
        mutation ($issue: ID!) {
          closeIssue(input: { issueId: $issue, stateReason: COMPLETED }) {
            clientMutationId
          }
        }
      `,
      { issue: issueId },
    );
  } else {
    graphql(
      `
        mutation ($issue: ID!) {
          reopenIssue(input: { issueId: $issue }) {
            clientMutationId
          }
        }
      `,
      { issue: issueId },
    );
  }
}

function createItem(project, repositoryId, op) {
  const body = syncedBody(op.card.body);
  if (op.as === "draft") {
    return {
      itemId: graphql(
        `
          mutation ($project: ID!, $title: String!, $body: String!) {
            addProjectV2DraftIssue(
              input: { projectId: $project, title: $title, body: $body }
            ) {
              projectItem {
                id
              }
            }
          }
        `,
        { project: project.id, title: op.card.title, body },
      ).addProjectV2DraftIssue.projectItem.id,
    };
  }
  // Created inside the project in one call: an issue added in a second call would be left off the board, with no
  // Key, whenever that call failed, and the next run would create it again.
  const { issue } = graphql(
    `
      mutation (
        $repository: ID!
        $project: ID!
        $title: String!
        $body: String!
      ) {
        createIssue(
          input: {
            repositoryId: $repository
            projectV2Ids: [$project]
            title: $title
            body: $body
          }
        ) {
          issue {
            id
            projectItems(first: 20) {
              nodes {
                id
                project {
                  id
                }
              }
            }
          }
        }
      }
    `,
    {
      repository: repositoryId,
      project: project.id,
      title: op.card.title,
      body,
    },
  ).createIssue;
  // Safe even when createIssue did place the issue: addProjectV2ItemById then returns the existing item.
  const itemId =
    boardItemOf(issue, project.id) ??
    graphql(
      `
        mutation ($project: ID!, $content: ID!) {
          addProjectV2ItemById(
            input: { projectId: $project, contentId: $content }
          ) {
            item {
              id
            }
          }
        }
      `,
      { project: project.id, content: issue.id },
    ).addProjectV2ItemById.item.id;
  return { itemId, issueId: issue.id };
}

function apply(project, repositoryId, op, issueIds) {
  if (op.op === "create") {
    const { itemId, issueId } = createItem(project, repositoryId, op);
    for (const [name, value] of Object.entries({
      ...op.card.fields,
      ...CREATE_FIELDS,
    })) {
      setField(project, itemId, name, value);
    }
    if (issueId) {
      issueIds.set(op.card.key, issueId);
      if (op.state) {
        setIssueState(issueId, op.state);
      }
    }
    return;
  }

  let { itemId, type, contentId } = op;
  if (op.convert) {
    const { item } = graphql(
      `
        mutation ($item: ID!, $repository: ID!) {
          convertProjectV2DraftIssueItemToIssue(
            input: { itemId: $item, repositoryId: $repository }
          ) {
            item {
              id
              content {
                ... on Issue {
                  id
                }
              }
            }
          }
        }
      `,
      { item: itemId, repository: repositoryId },
    ).convertProjectV2DraftIssueItemToIssue;
    itemId = item.id;
    contentId = item.content.id;
    type = "Issue";
  }
  if (op.title !== undefined || op.body !== undefined) {
    if (type === "Issue") {
      graphql(
        `
          mutation ($issue: ID!, $title: String, $body: String) {
            updateIssue(input: { id: $issue, title: $title, body: $body }) {
              clientMutationId
            }
          }
        `,
        { issue: contentId, title: op.title, body: op.body },
      );
    } else {
      graphql(
        `
          mutation ($draft: ID!, $title: String, $body: String) {
            updateProjectV2DraftIssue(
              input: { draftIssueId: $draft, title: $title, body: $body }
            ) {
              clientMutationId
            }
          }
        `,
        { draft: contentId, title: op.title, body: op.body },
      );
    }
  }
  for (const [name, value] of Object.entries(op.fields ?? {})) {
    setField(project, itemId, name, value);
  }
  if (type === "Issue") {
    issueIds.set(op.card.key, contentId);
    if (op.state) {
      setIssueState(contentId, op.state);
    }
  }
}

function linkParent(op, issueIds) {
  const child = issueIds.get(op.card.key);
  const parent = issueIds.get(op.parent);
  if (!child || !parent) {
    console.log(
      `warning: ${op.card.key} stays without a parent: ${op.parent} has no issue`,
    );
    return;
  }
  graphql(
    `
      mutation ($parent: ID!, $child: ID!) {
        addSubIssue(
          input: { issueId: $parent, subIssueId: $child, replaceParent: true }
        ) {
          clientMutationId
        }
      }
    `,
    { parent, child },
  );
}

function describe(op) {
  if (op.op === "create") {
    return `create ${op.card.key} as ${op.as}: ${op.card.fields.Status}${op.parent ? `, under ${op.parent}` : ""}`;
  }
  const parts = [
    ...(op.convert ? ["to an issue"] : []),
    ...(op.title !== undefined ? ["title"] : []),
    ...(op.body !== undefined ? ["body"] : []),
    ...Object.entries(op.fields ?? {}).map(
      ([name, value]) => `${name}=${value || "(empty)"}`,
    ),
    ...(op.state ? [op.state === "CLOSED" ? "close" : "reopen"] : []),
    ...(op.parent ? [`under ${op.parent}`] : []),
  ];
  return `update ${op.card.key}: ${parts.join(", ")}`;
}

if (setup && dryRun) {
  console.error(
    "--setup changes the project's fields, so it cannot run with --dry-run",
  );
  process.exit(1);
}

let project = readProject();
if (setup) {
  ensureFields(project);
  project = readProject();
}
const { specs, bugs } = readSnapshots();
const cards = deriveCards({ specs, bugs, pulls: readPulls() });
const ops = planChanges(cards, project.items);

// A field write that fails after its draft was created leaves a card with no Key, which every later run duplicates.
const problems = schemaProblems(project.fields, cards);
if (problems.length > 0) {
  console.error(`${problems.join("\n")}\nRun with --setup.`);
  process.exit(1);
}

const known = new Set(cards.flatMap((card) => [card.key, ...card.aliases]));
for (const item of project.items.filter(
  (candidate) => candidate.fields.Key && !known.has(candidate.fields.Key),
)) {
  console.log(
    `warning: ${item.fields.Key} is on the board but no longer in the repository`,
  );
}
const repositoryId = dryRun ? null : readRepositoryId();
const issueIds = new Map(
  project.items
    .filter((item) => item.type === "Issue" && item.fields.Key)
    .map((item) => [item.fields.Key, item.contentId]),
);
for (const op of ops) {
  console.log(`${dryRun ? "[dry run] " : ""}${describe(op)}`);
  if (!dryRun) {
    apply(project, repositoryId, op, issueIds);
  }
}
// A parent may become an issue in the same run as its sub-issue, so links wait until every card is written.
if (!dryRun) {
  for (const op of ops.filter((candidate) => candidate.parent)) {
    linkParent(op, issueIds);
  }
}
console.log(`${cards.length} cards, ${ops.length} changes`);
