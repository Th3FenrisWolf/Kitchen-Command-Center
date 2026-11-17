import { execFileSync } from "node:child_process";
import {
  IDEA_GATE_QUERY,
  IDEA_TEXT_QUERY,
  config,
  ideaArgumentError,
  ideaGateOverflow,
  ideaRefusal,
  ideaViewerError,
} from "./derive.mjs";

function graphql(query, variables = {}) {
  const output = execFileSync("gh", ["api", "graphql", "--input", "-"], {
    input: JSON.stringify({ query, variables }),
    encoding: "utf8",
    stdio: ["pipe", "pipe", "pipe"],
    maxBuffer: 64 * 1024 * 1024,
  });
  return JSON.parse(output);
}

function readIssue(query, variables) {
  let result;
  try {
    result = graphql(query, variables);
  } catch (error) {
    const text = `${error.stdout ?? ""}${error.stderr ?? ""}`;
    if (text.includes("Could not resolve to an Issue")) {
      return null;
    }
    throw error;
  }
  if (result.errors) {
    if (
      result.errors.every((e) =>
        /could not resolve to an issue/i.test(e.message),
      )
    ) {
      return null;
    }
    throw new Error(JSON.stringify(result.errors));
  }
  return result.data.repository.issue;
}

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

function print(answer) {
  process.stdout.write(`${JSON.stringify(answer)}\n`);
}

const arg = process.argv[2];
const argumentError = ideaArgumentError(arg);
if (argumentError) {
  fail(argumentError);
}
const number = Number(arg);
const variables = { owner: config.owner, repo: config.repo, number };

try {
  const viewer = graphql("query { viewer { login } }");
  if (viewer.errors) {
    throw new Error(JSON.stringify(viewer.errors));
  }
  const viewerError = ideaViewerError(viewer.data.viewer.login);
  if (viewerError) {
    fail(viewerError);
  }

  const project = graphql(
    `
      query ($owner: String!, $number: Int!) {
        user(login: $owner) {
          projectV2(number: $number) {
            id
          }
        }
      }
    `,
    { owner: config.owner, number: config.project },
  );
  if (project.errors) {
    throw new Error(JSON.stringify(project.errors));
  }
  const projectId = project.data.user.projectV2.id;

  const gate = readIssue(IDEA_GATE_QUERY, variables);
  const overflow = gate && ideaGateOverflow(number, gate.projectItems);
  if (overflow) {
    fail(overflow);
  }
  const gated = gate && {
    author: gate.author?.login ?? null,
    key:
      gate.projectItems.nodes
        .filter((item) => item.project.id === projectId)
        .map((item) => item.fieldValueByName?.text)
        .find(Boolean) ?? null,
    synced: false,
  };
  const refused = ideaRefusal(number, gated);
  if (refused) {
    print({ number, refused });
  } else {
    const text = readIssue(IDEA_TEXT_QUERY, variables);
    const body = text.body ?? "";
    const synced = ideaRefusal(number, {
      ...gated,
      synced: body.includes("<!-- sync -->"),
    });
    print(
      synced
        ? { number, refused: synced }
        : { number, url: text.url, title: text.title, body },
    );
  }
} catch (error) {
  fail(`could not read #${number}: ${error.message}`);
}
