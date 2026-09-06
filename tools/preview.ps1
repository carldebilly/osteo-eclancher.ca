<#
.SYNOPSIS
	Builds the site with the exact container GitHub Pages runs, and optionally serves it.

.DESCRIPTION
	GitHub Pages builds this repository with actions/jekyll-build-pages, pinned to the
	github-pages gem. Running that same image locally means a build that succeeds here
	succeeds there, without installing Ruby on Windows.

	Output lands in _site/ at the repository root, which is git-ignored and is also where
	the output-level tests look.

.PARAMETER Serve
	After a successful build, serve _site on http://localhost:<Port>.

.PARAMETER Port
	Port for the local server. Defaults to 8080.

.EXAMPLE
	./tools/preview.ps1 -Serve
#>
[CmdletBinding()]
param(
	[switch] $Serve,
	[int] $Port = 8080
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path $PSScriptRoot -Parent
$image = 'ghcr.io/actions/jekyll-build-pages:v1.0.13'

Write-Host "Building $repository/docs with $image" -ForegroundColor Cyan

docker run --rm `
	-v "${repository}:/github/workspace" `
	-e GITHUB_WORKSPACE=/github/workspace `
	-e INPUT_SOURCE=docs `
	-e INPUT_DESTINATION=_site `
	-e INPUT_VERBOSE=true `
	-e INPUT_FUTURE=false `
	-e INPUT_BUILD_REVISION=local `
	-e INPUT_TOKEN= `
	-e GITHUB_REPOSITORY=carldebilly/osteo-eclancher.ca `
	-e GITHUB_API_URL=https://api.github.com `
	$image

if ($LASTEXITCODE -ne 0) {
	throw "Jekyll build failed with exit code $LASTEXITCODE."
}

Write-Host 'Build succeeded.' -ForegroundColor Green

if ($Serve) {
	$site = Join-Path $repository '_site'
	Write-Host "Serving $site on http://localhost:$Port/ (Ctrl+C to stop)" -ForegroundColor Cyan
	python -m http.server $Port -d $site
}
