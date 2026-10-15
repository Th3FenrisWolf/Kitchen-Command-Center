// Every test shares one SQLite database. Umbraco's relation update after a content, media or member save reads and
// then writes without a write lock, so a commit by a parallel test in between leaves it retrying a stale snapshot
// for minutes. One test at a time keeps the suite deterministic.
[assembly: NotInParallel]

// A stranded SQLite write retries for about ten minutes; a limit makes it fail in two and leaves the job time to finish.
[assembly: Timeout(120_000)]
