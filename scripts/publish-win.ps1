param(
    [string]$Configuration = "Release",
    [string]$Output = ".\artifacts\DockCatWin"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "DockCatWin\DockCatWin.csproj"
$publishDir = Join-Path $repoRoot $Output

dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -o $publishDir

Write-Host "Published DockCatWin to $publishDir"
