* Custom TFS check-in notes are not exported to commit message (#1004, @EdwinEngelen)
* Log in a file git-tfs actions (#999, @pmiossec)
* Add support of `.gitignore` to ignore files in `clone` and `init` commands (#897)
* Improve branch point changeset detection (#1017 & #973, @fourpastmidnight & @jeremy-sylvis-tmg)
* Add support for deleting TFS shelvesets using the shelve-delete command
* Allow using both "--changeset" and "--up-to" options at the same time (#1057) to fetch a specific range of TFS changesets
* Added a TraceWarning message when the authors file fails to copy to the cache location (#1071)
* Prevent infinite loop when parent changeset cannot be found
* Added a `--check-lock` option for `rcheckin` command