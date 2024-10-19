## Summary

The exportmap command creates a mapping file of Tfs ChangeSet Id and Commit Id from an already migrated repository. 

## Synopsis

    Usage: git-tfs exportmap [options]
    where options are:
    
        -f=VALUE 	The path to the mapping file
        -m, --include-multiple-branches 	Outputs the commitid for each branch the changeset touches
								
## Examples

### Create a mapping file from current working directory / git repository

		git tfs exportmap -f mapping.txt
		
## Use Cases

Supports migration of Work Items with changesets to the new Git repository. 