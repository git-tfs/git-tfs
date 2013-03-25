BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Update the version in VERSION and Version.cs, commit, tag.

2. Build the release:

    msbuild Release.proj /t:Release

3. Upload the release somewhere.

4. Update [the download button](https://github.com/git-tfs/git-tfs.github.com/blob/master/_includes/download_button.html) on the website.
