$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path $PSScriptRoot -Parent
Push-Location $workspaceRoot
try {
    python scripts/verify_naming.py
    if ($LASTEXITCODE -ne 0) { throw 'Dosya veya Python adlandırma denetimi başarısız.' }

    dotnet run --project tools/MedulaOnKontrol.NamingChecks -- $workspaceRoot
    if ($LASTEXITCODE -ne 0) { throw 'C# adlandırma denetimi başarısız.' }

    dotnet format style MedulaOnKontrol.sln --no-restore --verify-no-changes --diagnostics IDE1006 --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Editör adlandırma kurallarıyla uyumsuz kaynak bulundu.' }

    foreach ($scriptFile in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1') {
        $scriptTokens = $null
        $parseErrors = $null
        $syntaxTree = [System.Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName, [ref]$scriptTokens, [ref]$parseErrors)
        if ($parseErrors.Count -gt 0) { throw "PowerShell sözdizimi hatası: $($scriptFile.Name)" }
        $variables = $syntaxTree.FindAll({ param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        foreach ($variable in $variables) {
            $name = $variable.VariablePath.UserPath
            if ($name -cnotmatch '^(?:[a-zA-Z][a-zA-Z0-9]*|_|env:[A-Z][A-Z0-9_]*)$') {
                throw "PowerShell değişken adı geçersiz: $($scriptFile.Name): $name"
            }
        }
    }

    foreach ($scriptFile in @('playwright.config.mjs', 'src/MedulaOnKontrol.Web/wwwroot/js/site.js') + @(Get-ChildItem tests/ui -Filter '*.mjs' | ForEach-Object { $_.FullName })) {
        node --check $scriptFile
        if ($LASTEXITCODE -ne 0) { throw "JavaScript sözdizimi hatası: $scriptFile" }
    }
    Write-Output 'Çalışma alanı adlandırma kontrolleri başarılı.'
}
finally {
    Pop-Location
}
