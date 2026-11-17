# Contract: Idea link

Three parties share it: the two commands write the Idea line, `idea.mjs` answers the commands, and the sync reads the
line.

## The Idea line

In a spec, on its own line above the `**Input**:` line, so no line of the quoted issue body comes before it:

```markdown
**Idea**: [#148](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/148)
```

In a bug assessment, as a bullet after `- **Source**:`, in the header bullets above the `## Report` section:

```markdown
- **Idea**: [#150](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/150)
```

The sync reads only the header, the text above the first `## ` heading, and takes its first line matching
`^(?:- )?\*\*Idea\*\*:\s*\[?#(\d+)`. Anything after the number is ignored. A spec or assessment with no such line
in its header names no idea. The owner and repository in the URL come from `tools/project-board/board.json`.

## `node tools/project-board/idea.mjs <number>`

Reads one issue for `/speckit-specify #NNN` and `/speckit-bug-assess #NNN`. `<number>` must match `^\d+$`.

It prints one JSON object on stdout and exits 0:

```json
{ "number": 148, "url": "https://github.com/…/issues/148", "title": "Companion App", "body": "…" }
```

or a refusal, with no title or body:

```json
{ "number": 200, "refused": "#200 was opened by another account" }
```

| Refusal | When |
|---|---|
| `#N does not exist in this repository` | No issue by that number (a pull request number counts as none) |
| `#N was opened by another account` | Its author is not the board owner. Its body is never read |
| `#N already carries the Key <key>` | Its item on this board has a Key |
| `#N is a board card the sync made` | Its body carries `<!-- sync -->` |

It exits non-zero, with a message on stderr, when `gh` is signed in as an account other than the board owner, or a
read fails. The board is private, so another account would see no Key.

The command quotes neither field into a shell command: it reads the JSON and uses `title` and `body` as the feature
description or the bug report.
