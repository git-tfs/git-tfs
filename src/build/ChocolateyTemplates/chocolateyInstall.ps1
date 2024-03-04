$sysDrive = $env:SystemDrive
$gittfsPath = "$sysDrive\tools\gittfs"

if(test-path $gittfsPath) {
	write-host "Cleaning out the contents of $gittfsPath"
	Remove-Item "$($gittfsPath)\*" -recurse -force
}

Install-ChocolateyZipPackage -Checksum '${Checksum}' -ChecksumType 'sha256' 'gittfs' '${DownloadUrl}' $gittfsPath
Install-ChocolateyPath $gittfsPath

write-host 'git-tfs has been installed. Call `git tfs` from the command line to see options. You may need to close and reopen the command shell.'
