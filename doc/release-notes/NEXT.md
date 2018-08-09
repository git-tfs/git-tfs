* When cloning a whole TFS Project Collection (``$/``) without specifying a local repository name, clone into "tfs-collection" instead of erroring with "Invalid Path". (#1202 by @0x53A)
* FindMergeChangesetParent now also takes into account the path of the changes. This should avoid detection of incorrect parent when a changeset has merges in different branches at once. (#1204 by @Laibalion)
* Provide way to delete all remotes in a single call (#1204 by @Laibalion)
