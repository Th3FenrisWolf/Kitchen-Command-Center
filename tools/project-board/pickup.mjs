import { execFileSync } from "node:child_process";
import {
  config,
  ideaViewerError,
  pickupArgumentError,
  pickupTarget,
} from "./derive.mjs";

function graphql(query, variables = {}) {
  const output = execFileSync("gh", ["api", "graphql", "--input", "-"], {
    input: JSON.stringify({ query, variables }),
    encoding: "utf8",
    stdio: ["pipe", "pipe", "pipe"],
    maxBuffer: 64 * 1024 * 1024,
  });
  const result = JSON.parse(output);
  if (result.errors) {
    throw new Error(JSON.stringify(result.errors));
  }
  return result.data;
}

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

function readItems() {
  const items = [];
  let after = null;
  do {
    const { user } = graphql(
      `
        query ($owner: String!, $number: Int!, $after: String) {
          user(login: $owner) {
            projectV2(number: $number) {
              items(
                first: 100
                after: $after
                archivedStates: [ARCHIVED, NOT_ARCHIVED]
              ) {
                nodes {
                  isArchived
                  content {
                    __typename
                    ... on Issue {
                      number
                      state
                    }
                  }
                  fieldValueByName(name: "Key") {
                    ... on ProjectV2ItemFieldTextValue {
                      text
                    }
                  }
                }
                pageInfo {
                  hasNextPage
                  endCursor
                }
              }
            }
          }
        }
      `,
      { owner: config.owner, number: config.project, after },
    );
    const page = user.projectV2.items;
    for (const node of page.nodes) {
      items.push({
        type: node.content?.__typename,
        number: node.content?.number,
        state: node.content?.state,
        archived: node.isArchived,
        fields: { Key: node.fieldValueByName?.text ?? "" },
      });
    }
    after = page.pageInfo.hasNextPage ? page.pageInfo.endCursor : null;
  } while (after);
  return items;
}

const key = process.argv[2];
const argumentError = pickupArgumentError(key);
if (argumentError) {
  fail(argumentError);
}

try {
  const viewerError = ideaViewerError(
    graphql("query { viewer { login } }").viewer.login,
  );
  if (viewerError) {
    fail(viewerError);
  }
  process.stdout.write(`${JSON.stringify(pickupTarget(key, readItems()))}\n`);
} catch (error) {
  fail(`could not read the card for ${key}: ${error.message}`);
}
