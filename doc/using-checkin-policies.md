Since v0.28.0, git-tfs doesn't support checkin policies out of the box anymore on 64bits version of Windows (no problem for 32bits version).

Checkin policies assemblies are distributed by Microsoft only in x86 mode.
Since v0.28.0, Git-tfs is distributed as "AnyCPU".
Consequently it will run in x64 mode on 64bits version of Windows and so won't be able to load the checkin policies by default.

If you need to use checkin policies, you must force git-tfs to run in x86 mode to be able to load the checkin policies assemblies.
To do that, go in the git-tfs folder and run the script `enable_checkin_policies_support.bat`.

If you want to revert the change, run the script `disable_checkin_policies_support.bat`.
