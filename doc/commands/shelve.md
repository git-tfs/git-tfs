## Summary
Creates a TFS shelveset from a Git branch.

## Features
[Special actions in commit messages](../special-actions-in-commit-messages.md) can be inserted, to associate TFS work items.

## Synopsis

    Usage: git-tfs shelve [options] shelveset-name [ref-to-shelve]
    where options are:

        -i, --id, --remote, --tfs-remote
            (Type: Value required, Value Type:[String])
            An optional remote ID, useful if this repository will track multiple
                TFS repositories.

        -d, --debug
            (Type: Flag, Value Type:[Boolean])
            Show lots of output.

        -h, -H, --help
            (Type: Flag, Value Type:[Boolean])
            ShowHelp

        -V, --version
            (Type: Flag, Value Type:[Boolean])
            ShowVersion
        -p, --evaluate-policies
            (Type: Flag, Value Type:[Boolean])
            Evaluate checkin policies
        -m, --comment
            (Type: Value required, Value Type:[String])
            A comment for the changeset.

        --build-default-comment
            (Type: Flag, Value Type:[Boolean])
            Use the comments from the commits on the current branch to create a
                default checkin message (checkintool only)

        -f, --force
            (Type: Value optional, Value Type:[String])
            To force a checkin, supply the policy override reason as an argument
                to this flag.

        -w, --associated-work-item
            (Type: 0 to many values accepted, Value Type:[String])
            WorkItemsToAssociate

        --resolved-work-item
            (Type: 0 to many values accepted, Value Type:[String])
            WorkItemsToResolve

## Examples

### Shelve current branch

`git tfs shelve MyShelvesetName`

### Shelve another branch

`git tfs shelve MyShlevesetName MyBranch`

### Shelve a branch from a different remote

`git tfs shelve MyShelveset OtherRemote/MyBranch`

## See also

* [unshelve](unshelve.md)
* [shelve-list](shelve-list.md)
