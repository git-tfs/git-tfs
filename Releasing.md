BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Make sure your HEAD is clean and that it is the same as the upstream master!

    git ls-remote https://github.com/git-tfs/git-tfs.git refs/heads/master
    git rev-parse HEAD
    git status

2. Build the release and include the version (e.g. X.Y.Z).

    msbuild Release.proj /t:Release /p:Version=X.Y.Z

3. Upload the release somewhere.

4. Update [the download button](https://github.com/git-tfs/git-tfs.github.com/blob/master/_includes/download_button.html) on the website.
