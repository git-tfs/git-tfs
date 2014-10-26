VsFake is a stub TFS connection for git-tfs that allows git-tfs to run in the absence of a TFS server. This should enable a couple of things:

1. Reproduction of bugs in a repeatable and portable fashion.
2. [Development on non-Windows workstations, running Mono](develop-on-mono.md).

## Usage

To use the VsFake driver, you will need to set the `GIT_TFS_CLIENT` environment variable.

In cmd:

```
> set GIT_TFS_CLIENT=Fake
> git-tfs --version
```

In bash (or other bourne-style shells, e.g. zsh):

```
$ export GIT_TFS_CLIENT=Fake
$ git-tfs --version
# or
$ GIT_TFS_CLIENT=Fake git-tfs --version
```

## Status

The plan for VsFake is to implement a mock TFS endpoint whose behaviour is defined by configuration files. In development (not just on Mono), this plugin should make it easier to reproduce and investigate bugs.

As of 30 Jan 2011, VsFake is only implemented enough for git-tfs to start and show help.
