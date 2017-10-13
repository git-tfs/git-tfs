$sysDrive = $env:SystemDrive
$gittfsPath = "$sysDrive\tools\gittfs"

if(test-path $gittfsPath) {
	write-host "Cleaning out the contents of $gittfsPath"
	Remove-Item "$($gittfsPath)\*" -recurse -force
}

write-host 'git-tfs has been uninstalled.'
