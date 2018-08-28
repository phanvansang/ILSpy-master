﻿$ErrorActionPreference = "Stop"

$baseCommit = "d779383cb85003d6dabeb976f0845631e07bf463";
$baseCommitRev = 1;

# make sure this list matches artifacts-only branches list in appveyor.yml!
$masterBranches = @("master", "3.2.x");

$globalAssemblyInfoTemplateFile = "ILSpy/Properties/AssemblyInfo.template.cs";

function Test-File([string]$filename) {
    return [System.IO.File]::Exists( (Join-Path (Get-Location) $filename) );
}

function Test-Dir([string]$name) {
    return [System.IO.Directory]::Exists( (Join-Path (Get-Location) $name) );
}

function Find-Git() {
	try {
		$executable = (get-command git).Path;
		return $executable -ne $null;
	} catch {
		#git not found in path, continue;
	}
	#we're on Windows
	if ($env:PROGRAMFILES -ne $null) {
		#hack for x86 powershell used by default (yuck!)
		if (${env:PROGRAMFILES(X86)} -eq ${env:PROGRAMFILES}) {
			$env:PROGRAMFILES = $env:PROGRAMFILES.Substring(0, $env:PROGRAMFILES.Length - 6);
		}
		#try to add git to path
		if ([System.IO.Directory]::Exists("$env:PROGRAMFILES\git\cmd\")) {
			$env:PATH = "$env:PATH;$env:PROGRAMFILES\git\cmd\";
			return $true;
		}
	}
	return $false;
}

function gitVersion() {
    if (-not ((Test-Dir ".git") -and (Find-Git))) {
        return 0;
    }
    return [Int32]::Parse((git rev-list --count "$baseCommit..HEAD")) + $baseCommitRev;
}

function gitCommitHash() {
    if (-not ((Test-Dir ".git") -and (Find-Git))) {
        return "0000000000000000000000000000000000000000";
    }
    return (git rev-list "$baseCommit..HEAD") | Select -First 1;
}

function gitBranch() {
    if (-not ((Test-Dir ".git") -and (Find-Git))) {
        return "no-branch";
    }

    if ($env:APPVEYOR_REPO_BRANCH -ne $null) {
        return $env:APPVEYOR_REPO_BRANCH;
    } else {
        return ((git branch --no-color).Split([System.Environment]::NewLine) | where { $_ -match "^\* " } | select -First 1).Substring(2);
    }
}

$templateFiles = (
	@{Input=$globalAssemblyInfoTemplateFile; Output="ILSpy/Properties/AssemblyInfo.cs"},
	@{Input="ICSharpCode.Decompiler/Properties/AssemblyInfo.template.cs"; Output="ICSharpCode.Decompiler/Properties/AssemblyInfo.cs"},
	@{Input="ICSharpCode.Decompiler/ICSharpCode.Decompiler.nuspec.template"; Output="ICSharpCode.Decompiler/ICSharpCode.Decompiler.nuspec"},
    @{Input="ILSpy/Properties/app.config.template"; Output = "ILSpy/app.config"},
    @{Input="ILSpy.AddIn/source.extension.vsixmanifest.template"; Output = "ILSpy.AddIn/source.extension.vsixmanifest"}
);
[string]$mutexId = "ILSpyUpdateAssemblyInfo" + (Get-Location).ToString().GetHashCode();
Write-Host $mutexId;
[bool]$createdNew = $false;
$mutex = New-Object System.Threading.Mutex($true, $mutexId, [ref]$createdNew);
try {
    if (-not $createdNew) {
        try {
		    $mutex.WaitOne(10000);
	    } catch [System.Threading.AbandonedMutexException] {
	    }
        return 0;
    }

    if (-not (Test-File "ILSpy.sln")) {
        Write-Host "Working directory must be the ILSpy repo root!";
        return 2;
    }

    $versionParts = @{};
    Get-Content $globalAssemblyInfoTemplateFile | where { $_ -match 'string (\w+) = "?(\w+)"?;' } | foreach { $versionParts.Add($Matches[1], $Matches[2]) }

    $major = $versionParts.Major;
    $minor = $versionParts.Minor;
    $build = $versionParts.Build;
    $versionName = $versionParts.VersionName;
    $revision = gitVersion;
    $branchName = gitBranch;
    $gitCommitHash = gitCommitHash;

    if ($masterBranches -contains $branchName) {
        $postfixBranchName = "";
    } else {
        $postfixBranchName = "-$branchName";
	}

    if ($versionName -eq "null") {
        $versionName = "";
        $postfixVersionName = "";
    } else {
        $postfixVersionName = "-$versionName";
    }
	
	$buildConfig = $args[0].ToString().ToLower();
	if ($buildConfig -eq "release") {
		$buildConfig = "";
	} else {
		$buildConfig = "-" + $buildConfig;
	}

    $fullVersionNumber = "$major.$minor.$build.$revision";
    
    foreach ($file in $templateFiles) {
        [string]$in = (Get-Content $file.Input) -Join [System.Environment]::NewLine;

		$out = $in.Replace('$INSERTVERSION$', $fullVersionNumber);
		$out = $out.Replace('$INSERTMAJORVERSION$', $major);
		$out = $out.Replace('$INSERTREVISION$', $revision);
		$out = $out.Replace('$INSERTCOMMITHASH$', $gitCommitHash);
		$out = $out.Replace('$INSERTSHORTCOMMITHASH$', $gitCommitHash.Substring(0, 8));
		$out = $out.Replace('$INSERTDATE$', [System.DateTime]::Now.ToString("MM/dd/yyyy"));
		$out = $out.Replace('$INSERTYEAR$', [System.DateTime]::Now.Year.ToString());
		$out = $out.Replace('$INSERTBRANCHNAME$', $branchName);
        $out = $out.Replace('$INSERTBRANCHPOSTFIX$', $postfixBranchName);
		$out = $out.Replace('$INSERTVERSIONNAME$', $versionName);
        $out = $out.Replace('$INSERTVERSIONNAMEPOSTFIX$', $postfixVersionName);
        $out = $out.Replace('$INSERTBUILDCONFIG$', $buildConfig);

        if (((Get-Content $file.Input) -Join [System.Environment]::NewLine) -ne $out) {
            $out | Out-File -Encoding utf8 $file.Output;
        }
    }
} finally {
    $mutex.ReleaseMutex();
    $mutex.Close();
}