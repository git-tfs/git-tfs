## Getting Involved

1. **Read the source.** It's hopefully not that intimidating.

2. **Check for issues.** There are a few issues that are low-hanging fruit. Feel free to pick them.

3. **Ask questions.** Feel free to drop me a line, or ask a question over at the [google group](http://groups.google.com/group/git-tfs-dev/), or join `#git-tfs` on FreeNode.

## Pull Requests

Here are some tips on creating a pull request:

1. Write awesome code. :sparkles:

2. Make sure the existing unit tests don't break. We try to keep the unit tests
[easy to run](https://github.com/git-tfs/git-tfs/wiki/Running-the-Unit-Tests).

3. We like new unit tests. If you can unit test your code, do so.
One of the pain points of the current git-tfs codebase is that parts of it are very difficult to unit test.
It's slowly getting less painful to unit test. One thing to try is to write an integration test
that runs git-tfs with the VsFake driver, similar to how the clone tests are written.

4. If you modify code in any of the client adapters (GitTfs.Vs*), please try it out with as many versions of the TFS client libraries as you can.
When you submit the pull request, include a note about which versions you have tried to compile with, and which ones you have tested with.
