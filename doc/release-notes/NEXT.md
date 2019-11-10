* Remove support for building an MSI package. It was broken since 6732259 ("Moving source in the 'src' folder", 2017-10-13)
  and the official distribution channel for binaries is a ZIP file or a chocolatey package.

* Added a new command to create a mapping file of changet ids and commit ids. 