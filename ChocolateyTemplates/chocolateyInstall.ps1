try {
  $sysDrive = $env:SystemDrive
  $gittfsPath = "$sysDrive\tools\gittfs"
  
  Install-ChocolateyZipPackage 'gittfs' '{{DownloadUrl}}' $gittfsPath
  Install-ChocolateyPath $gittfsPath

  write-host 'git-tfs has been installed. Call git tfs from the command line to see options. You may need to close and reopen the command shell.'
  Write-ChocolateySuccess 'gittfs'
} catch {
  Write-ChocolateyFailure 'gittfs' $($_.Exception.Message)
  throw 
}
