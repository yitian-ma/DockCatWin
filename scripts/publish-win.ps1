param(
    [string]$Configuration = "Release",
    [string]$Output = ".\artifacts\DockCatWin"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "DockCatWin\DockCatWin.csproj"
$publishDir = Join-Path $repoRoot $Output
$artifactsRoot = Join-Path $repoRoot "artifacts"
$resolvedPublishParent = Resolve-Path (Split-Path $publishDir -Parent)
$resolvedArtifactsRoot = if (Test-Path $artifactsRoot) {
    Resolve-Path $artifactsRoot
} else {
    New-Item -ItemType Directory -Force -Path $artifactsRoot
}

if (-not $resolvedPublishParent.Path.StartsWith($resolvedArtifactsRoot.Path, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish output must stay under $($resolvedArtifactsRoot.Path): $publishDir"
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $publishDir

Write-Host "Published DockCatWin to $publishDir"
