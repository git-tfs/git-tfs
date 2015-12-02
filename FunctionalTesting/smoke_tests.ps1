param (
    [string]$gittfsFolder = "c:\gittfs",
    [switch]$check = $true
)

$currentDir=[System.IO.Path]::GetFullPath(".")
$gittfsFolder=[System.IO.Path]::GetFullPath($gittfsFolder)
$env:Path = "$gittfsFolder;$currentDir;C:\Program Files\Common Files\microsoft shared\Team Foundation Server\14.0;" + $env:Path
$username = 'vtccds_cp@snd'
$password = 'vtccds'
$connectionParameter= "--username=$username --password=$password"
$beforeTime = Get-Date

#Write-Host "PATH: $env:Path"

function VerifyRepository($folder, $cloneCommand, $refToVerify)
{
	Write-Host "Scenario: $folder"
    if(Test-Path -Path $folder)
    {
        Remove-Item ".\$folder" -Recurse -Force
    }
	New-Item ".\$folder" -Force -ItemType directory | out-null
    cd ".\$folder"
	Write-Host "cmd: $cloneCommand"
	Invoke-Expression $cloneCommand
	if ($check -eq $true)
	{
		Write-Host "Checking references..."
		foreach($ref in $refs)
		{
			$refPath=$ref[1]
			$expectedSha1=$ref[0]
			#Write-Host "ref:"$refPath"/ hash:"$expectedSha1
			$sha1=& git rev-parse $refPath
			if ($LASTEXITCODE -ne 0) {
				throw "Reference $refPath not found!"
			}
			if($sha1 -ne $expectedSha1)
			{
				throw "Reference $refPath not good! Expected:$expectedSha1 / Found:$sha1"
			}
			Write-Host "ref:"$refPath" ...OK"
		}
	}
    cd ..
}

$refs=@(
@('3cdb2a311ac7cbda1e892a9b3371a76c871a696a', 'refs/heads/b1'                       ),
@('e6e79221fd35b2002367a41535de9c43b626150a', 'refs/heads/b1.1'                     ),
@('e7d54b14fbdcbbc184d58e82931b7c1ac4a2be70', 'refs/heads/master'                   ),
@('003ca02adfd9561418f05a61c7a999386957a146', 'refs/heads/renameFile'               ),
@('615ac5588d3cb6282c2c7d514f2828ad3aeaf5c7', 'refs/heads/renamed3'                 ),
@('3cdb2a311ac7cbda1e892a9b3371a76c871a696a', 'refs/remotes/tfs/b1'                 ),
@('e6e79221fd35b2002367a41535de9c43b626150a', 'refs/remotes/tfs/b1.1'               ),
@('9cb91c60d76d00af182ae9f16da6e6aa77b88a5e', 'refs/remotes/tfs/branch_from_nowhere'),
@('e7d54b14fbdcbbc184d58e82931b7c1ac4a2be70', 'refs/remotes/tfs/default'            ),
@('003ca02adfd9561418f05a61c7a999386957a146', 'refs/remotes/tfs/renameFile'         ),
@('615ac5588d3cb6282c2c7d514f2828ad3aeaf5c7', 'refs/remotes/tfs/renamed3'           )
)

VerifyRepository "WithBranches" "git tfs clone https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/trunk . --with-branches $connectionParameter" $refs

$refs=@(
@('e7d54b14fbdcbbc184d58e82931b7c1ac4a2be70', 'refs/heads/master'                   ),
@('0e127128039df6b2f0f160f482c4a3025200915d', 'refs/remotes/tfs/b1'                 ),
@('e7d54b14fbdcbbc184d58e82931b7c1ac4a2be70', 'refs/remotes/tfs/default'            )
)

VerifyRepository "AutoBranches" "git tfs clone https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/trunk . $connectionParameter" $refs

$refs=@(
@('0c7153033940a8b077ef50aa94fdfd3e7bae0cc4', 'refs/heads/master'                   ),
@('0c7153033940a8b077ef50aa94fdfd3e7bae0cc4', 'refs/remotes/tfs/default'            )
)

VerifyRepository "WithoutBranches" "git tfs clone https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/trunk . --ignore-branches $connectionParameter" $refs

$refs=@(
@('2d7fc48c4fafc4b4e13288a83a75adb56f412286', 'refs/heads/master'                   ),
@('2d7fc48c4fafc4b4e13288a83a75adb56f412286', 'refs/remotes/tfs/default'            )
)

VerifyRepository "CloneRoot" "git tfs clone https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/ . $connectionParameter" $refs

$refs=@(
@('9256b18ba788b41b758a04e41b2da524f01e83fe', 'refs/heads/master'                   ),
@('9256b18ba788b41b758a04e41b2da524f01e83fe', 'refs/remotes/tfs/default'            )
)

VerifyRepository "CloneAnotherBranch" "git tfs clone https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/b1.1 . $connectionParameter" $refs

$afterTime = Get-Date
$time = $afterTime - $beforeTime
$testTime = "{0}m {1}s" -f $time.Minutes, $time.Seconds

Write-Host "Check, finished! ($testTime)"
exit 0

#Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/SimpleTest gittfssandbox-test1 2 $username $password
#Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/UnicodeTest gittfssandbox-test2 1 $username $password
#Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/MergeTest/Main gittfssandbox-test3 39 $username $password
