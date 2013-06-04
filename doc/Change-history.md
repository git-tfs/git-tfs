
v0.17.1
-------

- Fixed `git tfs clone` broken in some cases in 0.17 (#330)

[Full diff](https://github.com/git-tfs/git-tfs/compare/v0.17.0...v0.17.1)


v0.17.0
-------

- [[branch]]
- [[labels]] (#256)
- git tfs pull --rebase (#254)
- git tfs clone --with-branches (#255)
- unicode support (#204)
- Use a custom workspace (#266)
- Clean workspaces directory (#269)
- Add a note on the commit to keep trace of the workitems (#276)
- Remove orphan folders (except in specific cases) (#323)

[Full diff](https://github.com/git-tfs/git-tfs/compare/v0.16.1...v0.17.0)

v0.16.1
-------

- Fixed `git tfs unshelve` (broken in 0.16.0) (#253).

[Full diff](https://github.com/git-tfs/git-tfs/compare/v0.16.0...v0.16.1)

v0.16.0
-------

- [[init-branch]]!! (#232)
- Faster clone (#226) and quick-clone.
- Add `git tfs info` (#219)
- Better metadata processing during rcheckin: remove the flags (#237), ignore whitespace (#238), add `git-tfs-force:` reason (#219).
- Always use CRLF in TFS checkin comments (#239)
- Checkin notes (#245)
- Use authors file more, and save it so you don't have to tell git-tfs about it every time you need it. (#252)

[Full diff](https://github.com/git-tfs/git-tfs/compare/v0.15.0...v0.16.0)

v0.15.0
-------

- Use [libgit2sharp](https://github.com/libgit2/libgit2sharp).
- Add default comment for shelves (#187)
- Add support for files with international characters (#200)
- Fix the mixed case problem (once and for all?) (#213)
- Add support for authors file
- Set up CI with [travis](http://travis-ci.org/git-tfs/git-tfs) and [teamcity](http://teamcity.codebetter.com/)

[Full diff](https://github.com/git-tfs/git-tfs/compare/v0.14.0...v0.15.0)

[v0.14.0](https://github.com/downloads/git-tfs/git-tfs/git-tfs-0.14.0.zip)
-------
- Fixed a bug in shelve (#133).
- Fixed rename problem in checkintool (#148).
- Fixed shelve -f (#157).
- Fixed (or unfixed) case sensitivity (#159).
- When a git subprocess exits with error, show the return/error code (#151).
- Add support for VS11.

[Full diff from 0.13](https://github.com/git-tfs/git-tfs/compare/v0.13.0...v0.14.0)

...


v0.12.1
-------
- Fixed: 'TF14045: The identity MYDOMAIN\John Doe is not a recognized identity' (#76, #81)
- Fixed: exception on unshelve if some items was renamed (#77)
- Fixed: rare problem when TFS' mixed mode assemblies cannot be loaded correctly (#93)
- Some fixes for Unicode filenames and TFS usernames (#80)
- git-tfs exit codes are now positive
- git-tfs cleans up files if clone command resulted in exception (#94)
- Restored VS2008 functionality (#99)