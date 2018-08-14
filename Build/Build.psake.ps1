<#

    This psake build script will build and package the NGY.API.Authentication library and "Publish" it to a local folder in disk that will act as a
    psuedo NuGet repository. This NuGet repository can then be used by the various other projects in this repository allowing them to easily manage
    and update their packages accordingly.

#>

#region Build Properties

# Build properties. For each build property, make sure you don't clobber a parameter by the same name passed in by the user.
Properties {

    ##~~~~~~~~~~~~~~~~~~~##
    ## Common Properties ##
    ##~~~~~~~~~~~~~~~~~~~##

    # Path to the bin directory of the library build. Strip any trailing slashes.
    if ($null -eq $LibraryPath) { $LibraryPath = (Resolve-Path "$($psake.build_script_dir)\..\HmacAuthentication") }
    $LibraryPath = $LibraryPath -replace '\\$',''
    $SolutionPath = "$LibraryPath\NGY.API.Authentication.sln"
    $ProjectFilePath = "$LibraryPath\NGY.API.Authentication\NGY.API.Authentication.csproj"

    # Path to the package directory that will act as an NuGet repository. Strip any trailing slashes.
    if ($null -eq $NuGetPath) { $NuGetPath = "$($psake.build_script_dir)\NuGet" }
    $NuGetPath = $NuGetPath -replace '\\$',''

    # Path to temporary files. Strip any trailing slashes.
    if ($null -eq $TempFilePath) { $TempFilePath = "$($psake.build_script_dir)\Temp" }
    $TempFilePath = $TempFilePath -replace '\\$',''

    # The build configuration to use.
    if ($null -eq $BuildConfiguration) { $BuildConfiguration = 'TST' }

    # Version to use in build and packaging.
    if ($null -eq $Version) { $Version = '0.1.0' }

    # Verbose preference.
    if ($null -eq $IsVerbose) { $IsVerbose = if ($VerbosePreference -ne 'SilentlyContinue') { $true } else { $false } }

}

#endregion Build Properties

# Format each task name as such.
FormatTaskName ("`n" + ('-' * 25) + ' Task : {0} ' + ('-' * 25) + "`n")

# Default task. Must contain no code.
Task Default -Depends Build

# Build task that will just call other tasks.
Task Build -Depends PreReqs, UpdateVersion, BuildLibrary, PackageLibrary, CleanUp

# Clean the project of any build artifacts.
Task Clean -Depends CleanUp

#====================================================================================================================================================
###################
## Prerequisites ##
###################

#region Prerequisites

# Prerequisites for several tasks.
Task PreReqs {

    $PreReqsMissing = $false

    # Ensure dotnet.exe is installed.
    if (-not (Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue)) {
        $PreReqsMissing = $true
        $msg = 'The dotnet.exe command was not found. This build script requires dotnet.exe to be installed and located in the environment path.'
        Write-Error $msg
    }

    # Ensure nuget.exe is installed.
    if (-not (Get-Command 'nuget.exe' -ErrorAction SilentlyContinue)) {
        $PreReqsMissing = $true
        $msg = 'The nuget.exe command was not found. This build script requires nuget.exe to be installed and located in the environment path.'
        Write-Error $msg
    }

    # If PreReqs was not satisfied then fail the build. Otherwise, print a success message.
    if ($PreReqsMissing) {
        throw 'Prerequsites were not met and the build cannot continue. Please review any previous errors messages, fix the issue and try again.'
    } else {
        Write-Host 'All prerequisites are present.' -ForegroundColor Green
    }

}

#endregion Prerequisites

#====================================================================================================================================================

####################
## Update Version ##
####################

#region Update Version

# Update the version strings in the solution to the request version.
Task UpdateVersion -Depends PreReqs {

    Write-Host 'Updating versions...' -ForegroundColor Green

    # Validate versioning.
    $Matches.Clear()
    if ($Version -match '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.?(?<patch>0|[1-9]\d*)?(?:-(?<preRelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:[\+\.](?<buildMetaData>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$') {
        $major = $Matches['major']
        $minor = $Matches['minor']
        $patch = $Matches['patch']
        $preRelease = $Matches['preRelease']
        $buildMetaData = $Matches['buildMetaData']
    } else {
        Write-Error -Message "Incorrect version syntax. Please use Semantic Version 2.0 specification. : Version = $Version"
        exit 1
    }

    $NuGetPackageVersion = $Version

    # Assembly version cannot have pre-release tags. Needs to be Microsoft versioning.
    # If the build metadata is just a number then we can include it.
    $AssemblyVersion = "$major.$minor.$patch"
    if ($buildMetaData -match '^\d+$') { "$AssemblyVersion.$buildMetaData" }

    # Updated *.csproj (dll version update).
    try {

        # Save a copy of the project file to be restored later as we don't want to make actual changes to the project files during a build.
        $null = New-Item -Path $TempFilePath -ItemType Directory -Verbose:$IsVerbose -ErrorAction Stop
        Copy-Item -Path $ProjectFilePath -Destination "$TempFilePath\$(Split-Path $ProjectFilePath -Leaf)" -Verbose:$IsVerbose -ErrorAction Stop

        $content = Get-Content -Path $ProjectFilePath -ErrorAction Stop

        $content | ForEach-Object { $_ -replace '( +)<Version>(.*?)</Version>$',"`$1<Version>$NuGetPackageVersion</Version>" } |
                   ForEach-Object { $_ -replace '( +)<AssemblyVersion>(.*?)</AssemblyVersion>$',"`$1<AssemblyVersion>$AssemblyVersion</AssemblyVersion>" } |
                   ForEach-Object { $_ -replace '( +)<FileVersion>(.*?)</FileVersion>$',"`$1<FileVersion>$AssemblyVersion</FileVersion>" } |
                   Out-File -FilePath $ProjectFilePath -Encoding utf8 -ErrorAction Stop

        Write-Host "Project file versions updated:" -ForegroundColor Green
        Write-Host "    Package Version   : $NuGetPackageVersion" -ForegroundColor Green
        Write-Host "    Assembly Version  : $AssemblyVersion" -ForegroundColor Green
        Write-Host "    File Version      : $AssemblyVersion" -ForegroundColor Green

    } catch { throw "Error attempting to update versions: $($_.Exception.Message)" }

}

#endregion Update Version

#====================================================================================================================================================
###################
## Build Library ##
###################

#region Build Library

# Task to build the shared library.
Task BuildLibrary -Depends UpdateVersion {

    Write-Host "Building shared authentication library...`n" -ForegroundColor Green

    try {

        dotnet.exe build --configuration $BuildConfiguration --verbosity minimal $SolutionPath

        if ($LASTEXITCODE -ne 0) { throw "Build operation finsihed with exit code : $LASTEXITCODE" }

    } catch {
        Write-Error -Message "Building the shared library did not complete successfully and the build cannot continue." -Exception $_.Exception
        exit 1
    }

}

#endregion Build Library

#====================================================================================================================================================
#####################
## Package Library ##
#####################

#region Package Library

# Task to build the shared library.
Task PackageLibrary -Depends BuildLibrary {

    Write-Host "Packaging shared authentication library...`n" -ForegroundColor Green

    try {

        dotnet.exe pack --no-build --configuration $BuildConfiguration --verbosity minimal --output $NuGetPath $SolutionPath

        if ($LASTEXITCODE -ne 0) { throw "Package operation finsihed with exit code : $LASTEXITCODE" }

    } catch {
        Write-Error -Message "Packaging the shared library did not complete successfully and the build cannot continue." -Exception $_.Exception
        exit 1
    }

}

#endregion Build Library

#====================================================================================================================================================
##############
## Clean Up ##
##############

#region Clean Up

# Clean up tasks.
Task CleanUp -Depends PackageLibrary {

    Write-Host "Cleaning up...`n" -ForegroundColor Green

    try {

        Copy-Item -Path "$TempFilePath\$(Split-Path $ProjectFilePath -Leaf)" -Destination $ProjectFilePath -Force -Verbose:$IsVerbose -ErrorAction Stop

        # Before we go deleting things with -Recurse -Force, lets make sure we aren't clobbering the filesystem.
        $resolvedPath = Resolve-Path -Path $TempFilePath -ErrorAction Stop
        if ($resolvedPath.Drive.Root -ne $resolvedPath.Path) {
            Remove-Item -Path $TempFilePath -Recurse -Force -Verbose:$IsVerbose -ErrorAction Stop
        } else { throw "Can't delete the root of a drive : $TempFilePath" }

    } catch {
        Write-Error -Message "Error cleaning up temporary project files." -Exception $_.Exception
        exit 1
    }

}

#endregion Clean Up
