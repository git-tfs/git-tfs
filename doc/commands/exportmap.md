## Summary

The exportmap command creates a mapping file of Tfs ChangeSet Id and Commit Id from an already migrated repository. 

## Synopsis

    Usage: git-tfs exportmap [options]
    where options are:
    
        -f=VALUE 	The path to the mapping file			
		-r=VALUE 	The path to the git repository
								
## Examples

### Create a mapping file from current working directory / git repository

		git tfs exportmap -f mapping.txt -r .