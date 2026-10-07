# Linking specs to Jira cards

Two rules every jira command, and any command that reads a card, follows.

## The Jira line

A spec's link to its card is one line in the header block of its `spec.md`,
directly below `**Feature Branch**`:

```markdown
**Jira**: [KEY-2](https://bizstream.atlassian.net/browse/KEY-2)
```

A spec may feed several cards, for example one card per user story. The line
then lists every key, comma separated, in the order the cards were linked:

```markdown
**Jira**: [KEY-2](https://bizstream.atlassian.net/browse/KEY-2), [KEY-5](https://bizstream.atlassian.net/browse/KEY-5)
```

This line is the only home of the link. Find a spec's cards by reading every
key on it. Find a card's specs by searching `specs/*/spec.md` for its key on
a `**Jira**:` line. To link a spec that already carries a Jira line, append
the new key to that line; never replace a key on it, and never write a
second `**Jira**:` line.

A pull request for a spec with a Jira line carries every key at the start of
its title, `KEY-2: <title>` or `KEY-2, KEY-5: <title>`, so the Jira
development panel links it and `speckit.jira.cards pr` can find it.

## Card criteria

A card made in Jira may carry acceptance criteria of its own. They are the
card's **criteria**, numbered `C1` to `Cn` in the order the live card lists
them. They come from the `acceptance_criteria_field_id` field when
`.specify/extensions/jira/jira-config.yml` sets it, and otherwise from the
list under a heading or label named Acceptance Criteria in the description.
A card with neither has no criteria, which is normal.

A spec written from the card covers every criterion with at least one
acceptance scenario, and ends that scenario with the id in brackets: `[C2]`.
A spec that feeds several cards puts the key before the id, `[KEY-5 C2]`, so
each id names one card. When a second key joins the line, qualify every bare
`[Cn]` already in the spec with the first key.
The registry snapshot `speckit.jira.pull` writes lists them under
`Card criteria:`.
