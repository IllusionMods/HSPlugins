# Env setup ---------------
if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx\plugins" 
Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
Remove-Item -Force -Path ($dir + "\out\") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($dir + "\out\")

foreach ($subdir in Get-ChildItem -Path $dir -Directory -Exclude "Out")
{
    if ((Get-ChildItem -Path $subdir -Filter *.dll).Length -gt 0)
    {
    
        $pluginDir = $subdir
    }
    else
    {
        $pluginDir = Get-Item ($subdir.FullName + "\BepInEx\plugins") 
    }
    Write-Information -MessageData ("Using " + $pluginDir + " as plugin directory")
    
    $outdir = $dir + "\out\" + $subdir.BaseName
    New-Item -ItemType Directory -Force -Path $outdir

    Copy-Item -Path ($dir + "\..\LICENSE") -Destination $outdir
    Copy-Item -Path ($dir + "\..\Readme.md") -Destination $outdir

    # Create releases ---------
    function CreateZip ($pluginFile)
    {
        Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force -Path $copy

        # the actual dll
        Copy-Item -Path $pluginFile.FullName -Destination $copy -Recurse -Force
        # the docs xml if it exists
        Copy-Item -Path ($pluginFile.DirectoryName + "\" + $pluginFile.BaseName + ".xml") -Destination $copy -Recurse -Force -ErrorAction Ignore
        Copy-Item -Path ($pluginFile.DirectoryName + "\" + $pluginFile.BaseName) -Destination $copy -Recurse -Force -ErrorAction Ignore

        if($pluginFile.Name -clike "*PE.dll")
        {
            New-Item -ItemType Directory -Force -Path ($dir + "\copy\mods")
            Copy-Item -Path ($pluginDir.FullName + "\Joan6694DynamicBoneColliders.zipmod") -Destination ($dir + "\copy\mods") -Force 
        }

        # the replace removes .0 from the end of version up until it hits a non-0 or there are only 2 version parts remaining (e.g. v1.0 v1.0.1)
        $ver = (Get-ChildItem -Path ($copy) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "^([\d+\.]+?\d+)[\.0]*$", '${1}'

        Compress-Archive -Path ($dir + "\copy\*") -Force -CompressionLevel "Optimal" -DestinationPath ($outdir + "\" + $pluginFile.BaseName + "_" + "v" + $ver + ".zip")
    }

    foreach ($pluginFile in Get-ChildItem -Path $pluginDir -Filter *.dll) 
    {
        try
        {
            CreateZip ($pluginFile)
        }
        catch 
        {
            # retry
            CreateZip ($pluginFile)
        }
    }

    Compress-Archive -Path ($outdir + "\*") -Force -CompressionLevel "NoCompression" -DestinationPath ($dir + "\out\" + $subdir.BaseName + "_HSPlugins_" + (Get-Date).ToString("yyyy.MM.dd") + ".zip")
    
}

Remove-Item -Force -Path ($dir + "\copy") -Recurse
