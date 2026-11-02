#requires -Version 7.0
<#
.SYNOPSIS
  Quality gate for total and changed-code line/branch coverage.
.DESCRIPTION
  Requires coverlet.collector in VSTest-based test projects. Uses Cobertura XML.
  New code means executable lines added or changed since the merge-base with BaseBranch.
  Only tracked, committed changes through HEAD are considered. Run from a clean tree.
  Paths must be resolvable unambiguously to tracked files.
.EXAMPLE
  pwsh ./coverage-gate.ps1 -Project ./App.sln -MinLineCoverage 80 -MinBranchCoverage 70 -MinNewLineCoverage 90 -MinNewBranchCoverage 85 -BaseBranch origin/main
#>
[CmdletBinding()]
param(
    [ValidateRange(0,100)][double] $MinLineCoverage = 0,
    [ValidateRange(0,100)][double] $MinBranchCoverage = 0,
    [ValidateRange(0,100)][double] $MinNewLineCoverage = 0,
    [ValidateRange(0,100)][double] $MinNewBranchCoverage = 0,
    [string] $Project = '.',
    [string] $BaseBranch = 'main',
    [ValidateSet('Pass','Fail')][string] $NoNewCode = 'Pass',
    [switch] $SkipTests,
    [string] $ReportsDirectory,
    [switch] $AllowDirtyTree
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$culture = [Globalization.CultureInfo]::InvariantCulture
$workDir = $null

function Invoke-Git([string[]]$Arguments) {
    $result = @(& git @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Git falhou: git $($Arguments -join ' ')`n$($result -join "`n")" }
    return $result
}
function Normalize-Path([string]$Path) {
    return (($Path -replace '\\','/') -replace '^\./','').TrimStart('/')
}
function Get-Percentage([long]$Covered, [long]$Total) {
    if ($Total -eq 0) { return $null }
    return 100.0 * $Covered / $Total
}
function Get-Count([System.Xml.XmlElement]$Element, [string]$Name) {
    if (-not $Element.HasAttribute($Name)) { throw "Atributo '$Name' ausente em $($Element.Name)." }
    return [long]::Parse($Element.GetAttribute($Name), $culture)
}
function Get-Status($Percentage, [double]$Minimum, [bool]$EmptyAccepted) {
    if ($null -eq $Percentage) { return $EmptyAccepted }
    return ($Percentage -ge $Minimum)
}
function Print-Metric([string]$Label, $Percentage, [double]$Minimum, [bool]$Passed) {
    $value = if ($null -eq $Percentage) { 'N/A' } else { '{0:F2}%' -f $Percentage }
    $status = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ('{0,-22} {1,8} (min {2:F2}%) [{3}]' -f $Label, $value, $Minimum, $status)
}
function Add-LineDetail([hashtable]$Details, [string]$Path, [int]$Number) {
    $key = "$Path`:$Number"
    if (-not $Details.ContainsKey($key)) {
        $Details[$key] = [pscustomobject]@{ Path = $Path; Number = $Number }
    }
}
function Add-BranchDetail([hashtable]$Details, [string]$Path, [int]$Number, [long]$Covered, [long]$Valid) {
    $key = "$Path`:$Number"
    if (-not $Details.ContainsKey($key)) {
        $Details[$key] = [pscustomobject]@{ Path = $Path; Number = $Number; Covered = $Covered; Valid = $Valid }
    }
}
function Print-LineDetails([string]$Title, [hashtable]$Details) {
    if ($Details.Count -eq 0) { return }
    Write-Host "`n$Title"
    foreach ($detail in @($Details.Values | Sort-Object Path, Number)) {
        Write-Host ('  {0}:{1}' -f $detail.Path, $detail.Number)
    }
}
function Print-BranchDetails([string]$Title, [hashtable]$Details) {
    if ($Details.Count -eq 0) { return }
    Write-Host "`n$Title"
    foreach ($detail in @($Details.Values | Sort-Object Path, Number)) {
        Write-Host ('  {0}:{1} ({2}/{3} branches covered)' -f $detail.Path, $detail.Number, $detail.Covered, $detail.Valid)
    }
}

try {
    $repoRoot = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $repoRoot) { throw 'Execute dentro de um repositório Git.' }
    $repoRoot = [IO.Path]::GetFullPath(($repoRoot | Select-Object -First 1).Trim())
    Push-Location $repoRoot
    try {
        if (-not $AllowDirtyTree) {
            $dirty = @(Invoke-Git -Arguments @('status','--porcelain','--untracked-files=no'))
            if ($dirty.Count -gt 0) { throw 'Working tree possui alterações tracked não commitadas. Commit antes de medir ou use -AllowDirtyTree (não recomendado).' }
        }
        $mergeBase = @(Invoke-Git -Arguments @('merge-base', $BaseBranch, 'HEAD'))[0].Trim()

        # Derive NEW-side line numbers from zero-context hunks. The +++ header
        # identifies the post-rename path; no hunk means no changed lines.
        $diff = @(Invoke-Git -Arguments @('-c','core.quotePath=false','diff','--no-ext-diff','--no-color','--unified=0','--find-renames','--diff-filter=ACMR',$mergeBase,'HEAD','--'))
        $changed = @{}
        $currentPath = $null
        foreach ($item in $diff) {
            $line = [string]$item
            if ($line.StartsWith('diff --git ')) { $currentPath = $null; continue }
            if ($line -match '^\+\+\+ b/(.+)$') {
                $currentPath = Normalize-Path $Matches[1]
                if ($currentPath.StartsWith('"') -or $currentPath.Contains('"')) {
                    throw "Git path com escape não suportado: $currentPath"
                }
                if (-not $changed.ContainsKey($currentPath)) { $changed[$currentPath] = [Collections.Generic.HashSet[int]]::new() }
                continue
            }
            if ($null -ne $currentPath -and $line -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@') {
                $start = [int]$Matches[1]
                $count = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
                for ($n = $start; $n -lt ($start + $count); $n++) { [void]$changed[$currentPath].Add($n) }
            }
        }

        if ($SkipTests) {
            if (-not $ReportsDirectory) { throw '-ReportsDirectory é obrigatório com -SkipTests.' }
            $resultsDir = [IO.Path]::GetFullPath($ReportsDirectory)
        } else {
            if ($ReportsDirectory) { throw 'Use -ReportsDirectory apenas com -SkipTests.' }
            $workDir = Join-Path ([IO.Path]::GetTempPath()) ('coverage-gate-' + [guid]::NewGuid().ToString('N'))
            $resultsDir = $workDir
            [void](New-Item -ItemType Directory -Path $resultsDir -Force)
            Write-Host "Executando: dotnet test $Project --collect:XPlat Code Coverage"
            & dotnet test $Project '--collect:XPlat Code Coverage' '--results-directory' $resultsDir 2>&1 | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) { throw "dotnet test falhou (exit code $LASTEXITCODE)." }
        }

        $reports = @(Get-ChildItem -LiteralPath $resultsDir -Filter 'coverage.cobertura.xml' -File -Recurse)
        if ($reports.Count -eq 0) { throw "Nenhum coverage.cobertura.xml encontrado em $resultsDir" }

        $totals = @{ lineCovered = [long]0; lineValid = [long]0; branchCovered = [long]0; branchValid = [long]0;
                     newLineCovered = [long]0; newLineValid = [long]0; newBranchCovered = [long]0; newBranchValid = [long]0 }
        $matchedChangedLines = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $uncoveredLines = @{}
        $uncoveredBranches = @{}
        $uncoveredNewLines = @{}
        $uncoveredNewBranches = @{}
        $tracked = @(Invoke-Git -Arguments @('-c','core.quotePath=false','ls-files','--cached'))
        $trackedSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $tracked) { [void]$trackedSet.Add((Normalize-Path ([string]$file))) }

        foreach ($report in $reports) {
            [xml]$doc = Get-Content -LiteralPath $report.FullName -Raw
            $coverage = $doc.DocumentElement
            if ($coverage.LocalName -ne 'coverage') { throw "XML inválido: $($report.FullName)" }
            $totals.lineCovered += Get-Count $coverage 'lines-covered'
            $totals.lineValid += Get-Count $coverage 'lines-valid'
            $totals.branchCovered += Get-Count $coverage 'branches-covered'
            $totals.branchValid += Get-Count $coverage 'branches-valid'

            $sources = @($coverage.SelectNodes('./sources/source') | ForEach-Object { $_.InnerText })
            foreach ($classNode in $coverage.SelectNodes('./packages/package/classes/class')) {
                $fileName = Normalize-Path ([string]$classNode.GetAttribute('filename'))
                $candidates = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                # An exact repo-relative match is preferred to a suffix match.
                if ($trackedSet.Contains($fileName)) { [void]$candidates.Add($fileName) }
                foreach ($source in $sources) {
                    $combined = Normalize-Path (([string]$source).TrimEnd('/','\') + '/' + $fileName)
                    $prefix = (Normalize-Path $repoRoot).TrimEnd('/') + '/'
                    if ($combined.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                        $relative = $combined.Substring($prefix.Length)
                        if ($trackedSet.Contains($relative)) { [void]$candidates.Add($relative) }
                    }
                }
                # Deterministic coverlet paths (/_/Project/File.cs) and absolute paths.
                if ($candidates.Count -eq 0) {
                    foreach ($trackedFile in $trackedSet) {
                        if ($fileName -eq $trackedFile -or $fileName.EndsWith('/' + $trackedFile, [StringComparison]::Ordinal)) {
                            [void]$candidates.Add($trackedFile)
                        }
                    }
                }
                if ($candidates.Count -gt 1) { throw "Mapeamento ambíguo: '$fileName' -> $($candidates -join ', ')" }
                if ($candidates.Count -eq 0) {
                    # Non-repo/generated files don't participate in changed-code coverage.
                    continue
                }
                $resolved = @($candidates)[0]
                $isChanged = $changed.ContainsKey($resolved)
                foreach ($node in $classNode.SelectNodes('./lines/line')) {
                    $number = [int]$node.GetAttribute('number')
                    $hits = [long]$node.GetAttribute('hits')
                    $hasBranch = $node.GetAttribute('branch') -ieq 'true'
                    $branchCovered = [long]0
                    $branchValid = [long]0
                    if ($hasBranch) {
                        $detail = $node.GetAttribute('condition-coverage')
                        if ($detail -notmatch '\((\d+)\s*/\s*(\d+)\)') { throw "Branch sem condition-coverage: $resolved`:$number" }
                        $branchCovered = [long]$Matches[1]
                        $branchValid = [long]$Matches[2]
                        if ($branchCovered -gt $branchValid) { throw "Branch inconsistente: $resolved`:$number" }
                        if ($branchCovered -lt $branchValid) {
                            Add-BranchDetail $uncoveredBranches $resolved $number $branchCovered $branchValid
                        }
                    }
                    if ($hits -eq 0) {
                        Add-LineDetail $uncoveredLines $resolved $number
                    }
                    if (-not $isChanged -or -not $changed[$resolved].Contains($number)) { continue }
                    # Avoid double-counting the same file/line when Cobertura repeats
                    # classes, but different test reports may legitimately overlap;
                    # flag those as unsupported rather than silently overcounting.
                    $key = "$resolved`:$number"
                    if (-not $matchedChangedLines.Add($key)) { throw "Linha de código novo duplicada nos relatórios: $key. Gere relatório mesclado sem duplicatas." }
                    $totals.newLineValid++
                    if ($hits -gt 0) { $totals.newLineCovered++ }
                    if ($hits -eq 0) { Add-LineDetail $uncoveredNewLines $resolved $number }
                    if ($hasBranch) {
                        $totals.newBranchCovered += $branchCovered
                        $totals.newBranchValid += $branchValid
                        if ($branchCovered -lt $branchValid) {
                            Add-BranchDetail $uncoveredNewBranches $resolved $number $branchCovered $branchValid
                        }
                    }
                }
            }
        }

        # A modified source line that cannot be matched must not silently pass.
        # Ignore non-executable changed lines, but report the count for transparency.
        $diffCount = 0
        foreach ($path in $changed.Keys) { if ($path -match '\.cs$') { $diffCount += $changed[$path].Count } }
        Write-Host "Relatórios: $($reports.Count) | Linhas C# modificadas no Git: $diffCount | Linhas instrumentadas modificadas: $($totals.newLineValid)"

        if ($totals.lineValid -eq 0) { throw 'Nenhuma linha instrumentada encontrada.' }
        $line = Get-Percentage $totals.lineCovered $totals.lineValid
        $branch = Get-Percentage $totals.branchCovered $totals.branchValid
        $newLine = Get-Percentage $totals.newLineCovered $totals.newLineValid
        $newBranch = Get-Percentage $totals.newBranchCovered $totals.newBranchValid
        $emptyOK = $NoNewCode -eq 'Pass'
        $lineOK = Get-Status $line $MinLineCoverage $false
        $branchOK = Get-Status $branch $MinBranchCoverage ($MinBranchCoverage -eq 0)
        $newLineOK = Get-Status $newLine $MinNewLineCoverage $emptyOK
        $newBranchOK = Get-Status $newBranch $MinNewBranchCoverage $emptyOK
        Write-Host "`n=========== QUALITY GATE ==========="
        Print-Metric 'Line Coverage' $line $MinLineCoverage $lineOK
        Print-Metric 'Branch Coverage' $branch $MinBranchCoverage $branchOK
        Print-Metric 'New Line Coverage' $newLine $MinNewLineCoverage $newLineOK
        Print-Metric 'New Branch Coverage' $newBranch $MinNewBranchCoverage $newBranchOK
        if (-not $lineOK) { Print-LineDetails 'Uncovered lines' $uncoveredLines }
        if (-not $branchOK) { Print-BranchDetails 'Uncovered branches' $uncoveredBranches }
        if (-not $newLineOK) { Print-LineDetails 'Uncovered new lines' $uncoveredNewLines }
        if (-not $newBranchOK) { Print-BranchDetails 'Uncovered new branches' $uncoveredNewBranches }
        $passed = $lineOK -and $branchOK -and $newLineOK -and $newBranchOK
        Write-Host ('RESULTADO: ' + $(if ($passed) { 'true' } else { 'false' }))
        if ($passed) { exit 0 } else { exit 1 }
    } finally { Pop-Location }
} catch {
    Write-Host "ERRO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'RESULTADO: false'
    exit 1
} finally {
    if ($workDir -and (Test-Path -LiteralPath $workDir)) { Remove-Item -LiteralPath $workDir -Recurse -Force }
}
