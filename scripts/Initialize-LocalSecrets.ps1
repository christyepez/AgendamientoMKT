param([string]$SqlContainer = "requirements-sqlserver", [Security.SecureString]$AdminPassword)

$ErrorActionPreference = "Stop"
$containerEnvironment = docker inspect $SqlContainer --format '{{json .Config.Env}}' | ConvertFrom-Json
$passwordEntry = $containerEnvironment | Where-Object { $_ -like "MSSQL_SA_PASSWORD=*" } | Select-Object -First 1
if (-not $passwordEntry) { throw "MSSQL_SA_PASSWORD was not found in container $SqlContainer." }
$sqlPassword = $passwordEntry.Substring("MSSQL_SA_PASSWORD=".Length)
$masterKeyBytes = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$jwtKeyBytes = [Security.Cryptography.RandomNumberGenerator]::GetBytes(48)
$passwordPointer = [IntPtr]::Zero
try {
    if (-not $AdminPassword) { $AdminPassword = Read-Host "Initial administrator password" -AsSecureString }
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword)
    $plainAdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $env:AGENDAMIENTO_MASTER_KEY = [Convert]::ToBase64String($masterKeyBytes)
    $secrets = [ordered]@{
        ConnectionStrings = @{ Default = "Server=requirements-sqlserver;Database=AgendamientoMKT;User Id=sa;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=True" }
        Jwt = @{ Key = [Convert]::ToBase64String($jwtKeyBytes) }
        Seed = @{ AdminPassword = $plainAdminPassword }
        MicrosoftGraph = @{ TenantId = ""; ClientId = ""; ClientSecret = "" }
        PowerPlatform = @{ PowerAutomateEndpoint = ""; PowerBiWorkspaceId = "" }
    }
    $plainPath = "config/secrets.plain.json"
    [IO.Directory]::CreateDirectory("config") | Out-Null
    [IO.File]::WriteAllText($plainPath, ($secrets | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
    & "$PSScriptRoot/Protect-Secrets.ps1" -InputPath $plainPath -OutputPath "config/secrets.aes256.json"
    Remove-Item -LiteralPath $plainPath -Force
    $envFile = "AGENDAMIENTO_MASTER_KEY=$($env:AGENDAMIENTO_MASTER_KEY)`nADMIN_EMAIL=admin@agendamientomkt.local`nAPI_PORT=5200`nWEB_PORT=3001`nAPP_PORT=8088`nREQUIREMENTS_NETWORK=requirements-platform_default`n"
    [IO.File]::WriteAllText(".env", $envFile, [Text.UTF8Encoding]::new($false))
}
finally {
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($masterKeyBytes)
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($jwtKeyBytes)
    $sqlPassword = $null
    $plainAdminPassword = $null
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
}

Write-Host "Local encrypted configuration created. The master key is stored only in the ignored .env file."
