BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

0. Put the S3 credentials in Deploy.properties

    [example goes here]

1. Update the version in VERSION and Version.cs, commit, tag.

2. Build the release: build in release mode, upload to S3, update the website to point to the latest release.
   Deploy.proj would be happy to do this for you.

    msbuild Release.proj /t:Release
