# Regenerates docs/bom-party-mapping-report.md from the live API.
# Requires the API running (e.g. dotnet run from POApprovalAPI) with DB access.

param(
    [string]$ApiBase = "http://localhost:5115",
    [string]$OutFile = "$PSScriptRoot\..\docs\bom-party-mapping-report.md"
)

$ErrorActionPreference = "Stop"

Write-Host "Fetching party mapping from $ApiBase ..."
$groups = Invoke-RestMethod -Uri "$ApiBase/api/bom/party-mapping" -TimeoutSec 120
$customers = Invoke-RestMethod -Uri "$ApiBase/api/bom/customers" -TimeoutSec 120

$multiAlias = @($groups | Where-Object { $_.aliases.Count -gt 1 } | Sort-Object { -$_.aliases.Count })
$official = @($groups | Where-Object { $_.fromMaster })
$cluster = @($groups | Where-Object { -not $_.fromMaster -and $_.mappingType -eq "Cluster" })

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# BOM Party Name Mapping Report")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')")
[void]$sb.AppendLine("Source: ``GET /api/bom/party-mapping`` (``BomPartyMappingService``)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("> BOM records keep their stored party names. Mapping is for filters/search only.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Summary")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Metric | Count |")
[void]$sb.AppendLine("|--------|------:|")
[void]$sb.AppendLine("| Filter dropdown groups | $($customers.Count) |")
[void]$sb.AppendLine("| Total mapping groups | $($groups.Count) |")
[void]$sb.AppendLine("| Groups with 2+ spellings | $($multiAlias.Count) |")
[void]$sb.AppendLine("| Official master groups | $($official.Count) |")
[void]$sb.AppendLine("| BOM-only cluster groups | $($cluster.Count) |")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Mapping types (implemented)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Type | Meaning |")
[void]$sb.AppendLine("|------|---------|")
[void]$sb.AppendLine("| **Official** | Exact ``CompanyMaster`` row |")
[void]$sb.AppendLine("| **Official+Region** | Official row + BOM variants mapped by country (Greif, LC, Cesur, NNZ, Storsack, Boxon) |")
[void]$sb.AppendLine("| **Cluster** | BOM-only variants grouped by normalized name (e.g. Alkimia + ALKIMIA GROUP) |")
[void]$sb.AppendLine("| **Singleton** | Single BOM spelling, no master |")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## All mapping groups")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Display name (filter) | Type | Official master | Aliases | BOM spellings |")
[void]$sb.AppendLine("|----------------------|------|-----------------|--------:|---------------|")

foreach ($g in ($groups | Sort-Object displayName)) {
    $display = ($g.displayName -replace '\|', '\|')
    $officialName = if ($g.officialName) { $g.officialName } else { "—" }
    $officialName = ($officialName -replace '\|', '\|')
    $aliasList = ($g.aliases -join " · ") -replace '\|', '\|'
    if ($aliasList.Length -gt 200) { $aliasList = $aliasList.Substring(0, 197) + "..." }
    [void]$sb.AppendLine("| $display | $($g.mappingType) | $officialName | $($g.aliases.Count) | $aliasList |")
}

$sb.ToString() | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "Written: $OutFile ($($groups.Count) groups)"
