== Git# --> Git for .NET ==
... a native Windows version of the fast & free open source version control system

Git# is the most advanced C# implementation of git for the .NET framework and Mono. 
It is aimed to be fully compatible to the original git for linux and can be used as stand 
alone command line application or as library for windows applications such as gui 
frontends or plugins for IDEs.

Git# is released under the BSD license. It is derived from the Java version jgit.
Please refer to the LICENSE.txt files for the complete license.

For more info check out the Git# website at http://www.eqqon.com/index.php/GitSharp

== WARNINGS / CAVEATS   ==

- Symbolic links are not supported because Windows does not directly support them.
  Such links could be damaged.

- Only the timestamp of the index is used by git check if  the index
  is dirty.

- CRLF conversion is never performed. You should therefore
  make sure your projects and workspaces are configured to save files
  with Unix (LF) line endings.

== Features ==

    * Read loose and packed commits, trees, blobs, including
      deltafied objects.

    * Read objects from shared repositories

    * Add files to the index and commit changes

    * Copy trees to local directory, or local directory to a tree.

    * Lazily loads objects as necessary.

    * Read and write .git/config files.

    * Create a new repository.

    * Checkout in dirty working directory if trivial.

    * Walk the history from a given set of commits looking for commits
      introducing changes in files under a specified path.

    * Object transport  
      Fetch via ssh, git, http and bundles.
      Push via ssh, git. Git# does not yet deltify
      the pushed packs so they may be a lot larger than C Git packs.

== Missing Features ==

There are a lot of missing features in GitSharp. You need the real Git 
for those.

- Merging. 

- Repacking.

- Generate a GIT format patch.

- Apply a GIT format patch.

- Documentation. :-)

- gitattributes support
  In particular CRLF conversion is not implemented. Files are treated
  as byte sequences.

- submodule support
  Submodules are not supported or even recognized.

== Support ==

  Post question, comments or patches to the official Git# mailing list at 
  http://groups.google.com/group/gitsharp/.

== Tools and Dependencies ==

GitSharp contains the following open source components:

Included as Source:
* MiscUtil by Jon Skeet and Marc Gravell
* NDesk.Options 

Binary Dependencies:
* DiffieHellman
* ICSharpCode.SharpZipLib
* Org.Mentalis.Security
* Tamir.SharpSSH
* Winterdom.IO.Filemap

Tools:
* NAnt 
* NUnit

== About GIT itself ==

More information about GIT, its repository format, and the canonical
C based implementation can be obtained from the GIT websites:

  http://git.or.cz/
  http://www.kernel.org/pub/software/scm/git/
  http://www.kernel.org/pub/software/scm/git/docs/

More information about the Java implemetation which Git# stems from:
  http://git.or.cz/gitwiki/EclipsePlugin
