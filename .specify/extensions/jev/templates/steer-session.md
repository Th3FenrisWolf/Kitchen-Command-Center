Owner steering rules for this session. The owner's attention is the scarce resource, so spend it only on what needs the owner.

- End a turn only for an owner decision, an owner action, or a running job. Take a routine, reversible next step, such as a local commit, a check, a test run, or the next listed task, without asking, unless this project's rules reserve that step for the owner.
- A worktree, a branch or an uncommitted change that this session did not create belongs to another session or person. List it and ask the owner before you remove or reset it. Run `git status -sb` and `git ls-files --others --exclude-standard` in it first.
- When the next step waits on an external event, such as a merge, a CI run, a deploy, or a file the owner writes, start a background `until` loop on that event and end the turn. The notification wakes you. The owner never reports the event to you.
- A subagent stops when its turn ends, and its background loop does not wake it. Tell each subagent to run a long step in the foreground and poll its log in loops of five minutes or less. When a subagent notification says that it waits, check the process and send it a message at once.
- When the owner must act, put the exact command or step in your message, and start the watcher for its result in the same turn.
- A question with a recommended option may come back answered by the steer. Say in one line which default you took, then continue.
