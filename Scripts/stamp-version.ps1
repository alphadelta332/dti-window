param(
    [string]$JsonPath,
    [string]$OutputPath
)

$v = Get-Content $JsonPath -Raw | ConvertFrom-Json
$version = "$($v.Major).$($v.Minor).$($v.Build).0"

$content = @"
// Auto-generated from Version.json — do not edit manually.
using System.Reflection;
[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
"@

Set-Content -Path $OutputPath -Value $content -Encoding UTF8NoBOM
