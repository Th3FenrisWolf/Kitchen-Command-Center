# acli gotchas

Nine things about the Atlassian CLI and the Jira REST API that cost real time on a prior project.
Read this before scripting an `acli` call this extension's commands do not
already spell out for you.

- **Authenticate with a Jira API token, never a password.** Create one at
  https://id.atlassian.com/manage-profile/security/api-tokens, then run
  `acli jira auth login --site <site> --email <email> --token` with the token
  on standard input, from a file or a pipe, never as an argument. A bare
  `acli jira auth login` does not complete. `acli jira auth status` confirms
  it. A token that expires or is revoked fails the same way as no login.
- **`view` returns six fields unless told otherwise**: key, type, summary,
  status, assignee and description. The parent and every custom field need
  `--fields`, for example
  `acli jira workitem view <KEY> --fields "summary,description,parent,customfield_<id>" --json`.

- **Use `--csv`, not `--json`, for a search you only need to read.** A search
  returns four columns either way; `--json` returns the full work-item
  envelope per row and costs far more context for the same answer.
- **`--json` returns a top-level array**, never `{"issues": [...]}`. Parse it
  as a list.
- **`search --fields` rejects `type` and `parent`.** Get either one from
  `acli jira workitem view <KEY>`, which takes the key as a positional
  argument — there is no `--key` flag.
- **A transition's name and its target status differ.** `--transition` takes
  the transition name (what the workflow calls the button), not the status
  it lands on. List the transitions available on an item before guessing.
- **The API cannot move a work item between Story and Sub-task.** Jira
  validates a parent against the item's current type, so a create-then-edit
  or an edit-then-edit both fail. Only the UI's Move operation changes level.
  Get the level right the first time; a card created at the wrong level gets
  recreated at the right one, and the original is closed with a comment
  naming its replacement — never reported as delivered.
- **Write each key to the registry the moment it lands, not at the end of a run.**
  A long run can be interrupted after a card is created but before its key is
  recorded. The next run has no way to know the card exists, and creates it a
  second time. Record-as-you-go is what makes a resume safe.
- **A v3 write of a plain string to a text custom field fails** with
  `Operation value must be an Atlassian Document`. A multi-line text custom
  field takes an Atlassian Document, also when it renders as plain text. Find
  the field id on this instance with
  `acli jira workitem view <KEY> --fields '*all' --json | grep -i customfield`, because ids
  differ between instances. Send a URL as one paragraph with one plain text
  node and no `link` mark, since Jira links a bare URL on display:
  `{"type":"doc","version":1,"content":[{"type":"paragraph","content":[{"type":"text","text":"<url>"}]}]}`.
  Read the field back and confirm the stored text.
