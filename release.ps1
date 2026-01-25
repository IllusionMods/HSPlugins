$dir = $PSScriptRoot + "/bin/"

$Folder = $dir + "out"

# Ensure the folder exists
if (-not (Test-Path $Folder -PathType Container)) {
    Write-Error "Folder '$Folder' does not exist."
    exit 1
}

# Get all zip files in the folder
$zipFiles = Get-ChildItem -Path $Folder -Filter '*.zip' | Where-Object { -not $_.PSIsContainer }

# Get current date in YYYY-MM-DD format
$currentDate = Get-Date -Format 'yyyy-MM-dd'

# Group files by prefix (before first '_')
$groups = $zipFiles | Group-Object { $_.BaseName.Split('_')[0] }

foreach ($group in $groups) {
    $prefix = $group.Name
    $archiveName = $dir + $prefix + "_HSPlugins_" + $currentDate + ".zip"

    Remove-Item $archiveName -ErrorAction SilentlyContinue

    # Create a temporary directory for renamed files
    $tempDir = Join-Path $dir "temp_$prefix"
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    # Copy and rename files to temp directory (remove prefix and underscore)
    $renamedFiles = @()
    foreach ($file in $group.Group) {
        $originalName = $file.Name
        $newName = $originalName -replace "^$prefix[_]", ''
        $destPath = Join-Path $tempDir $newName
        Copy-Item $file.FullName $destPath
        $renamedFiles += $destPath
    }

    $licensePath = Join-Path $dir "..\LICENSE"
    $readmePath = Join-Path $dir "..\Readme.md"
    $renamedFiles += $licensePath
    $renamedFiles += $readmePath
    Compress-Archive -Path $renamedFiles -DestinationPath $archiveName
    Write-Host "Created archive: $archiveName"

    # Clean up temp directory
    Remove-Item $tempDir -Recurse -Force
}