param (
    [switch]$check = $true
)
$env:Path = "c:\Work\git-tfs\GitTfs\bin\Release;" + $env:Path
Remove-Item gittfssandbox-test* -Force -Recurse
Remove-Item vtccds-test* -Force -Recurse
Remove-Item stat-*.txt -Force
$username = 'vtccds_cp@snd'
$password = 'vtccds'
Function Clone-TFS ($url, $path, $folder, $maxChangesets)
{
	Measure-Command { git-tfs clone $url $path $folder --max-changesets=$maxChangesets --username=$username --password=$password | Out-Default } | Tee-Object -file stat-$folder.txt
	if (!$?) {
		throw  "git-tfs clone error"
	}
}

Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/SimpleTest gittfssandbox-test1 2 $username $password
Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/UnicodeTest gittfssandbox-test2 1 $username $password
Clone-TFS https://tfs.codeplex.com:443/tfs/TFS28 $/gittfssandbox/Tests/MergeTest/Main gittfssandbox-test3 39 $username $password
Clone-TFS https://tfs.codeplex.com:443/tfs/TFS16 $/vtccds/trunk vtccds-test1 16 $username $password
Clone-TFS https://tfs.codeplex.com:443/tfs/TFS16 $/valtechgittfs/trunk vtccds-test2 105  $username $password

if ($check -eq $true)
{
	$errorCount = 0
	if ($username)
	{
		$repositories = @("gittfssandbox-test1", "gittfssandbox-test2", "gittfssandbox-test3", "vtccds-test1", "vtccds-test2")
	}
	else
	{
		$repositories = @("vtccds-test1", "vtccds-test2")
	}
	foreach ($repository in $repositories) {
		pushd $repository
		$repository
		$tfsCommitCount = [int](git rev-list HEAD --count)
		git remote add origin https://github.com/KindDragon/$repository.git | Out-Null
		git fetch origin
		$gitCommitCount = [int](git rev-list HEAD --count)
		if ($tfsCommitCount -ne $gitCommitCount)
		{
			$errorCount++
			Write-Error "Commit count different in repository $repository"
			git diff --stat origin/master
			popd
		}
		else
		{
			"Repository is equal"
			popd
			compare-object (get-content .\git-$repository.txt | Select-Object -First 6) (get-content .\stat-$repository.txt | Select-Object -First 6)
		}
		""
	}
    exit $errorCount
}