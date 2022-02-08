* Keep already committed files (e.g. `.gitignore`) when cloning with `--changeset` (#1382 by @boogisha)
* Correct WorkItem URL in the changeset metadata (#1396 by @siprbaum)
* Fix a rare error fetching the workitems associated to a changeset (#1395 @drolevar)
* Remove support for TFS 2008 (#1397 @siprbaum)
* Fix #1398: no automatic line ending conversion when git tfs clone was called with a 
  `--gitignore` parameter (#1399 by siprbaum)
* Speed up git-tfs startup time by removing a useless `git rev-parse --show-prefix` invocation.
  In addition, make a lot of small internal cleanups eliminating dead code (#1400 by siprbaum)
* Added support for TFS 2022 (#1406)
* Fix rare crash when checkin policy support as enabled (#1408 by siprbaum)
* Add support for git-init's --initial-branch option and make git-tfs agnostic with
  respect to initial branch names (#1405 by fineol)
* Fix issue #1409: cloning a TFS project whose first changeset is a rename quietly
  fails if you specify the `--gitignore` option (#1412 by fineol)
