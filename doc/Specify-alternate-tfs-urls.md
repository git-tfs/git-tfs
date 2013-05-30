If you upgrade TFS to 2010 (or otherwise change the server URL), you will want to tell git-tfs about the change (so that it doesn't refetch the entire repository). 

    git config tfs-remote.default.legacy-urls http://tfs:8080 # comma-separated list of old URLs.
    git config tfs-remote.default.url http://tfs:8080/tfs/DefaultCollection # current URL.
    git tfs fetch