param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [string]$OutputPath = "config/secrets.aes256.json"
)

$ErrorActionPreference = "Stop"
$encodedKey = $env:AGENDAMIENTO_MASTER_KEY
if ([string]::IsNullOrWhiteSpace($encodedKey)) {
    throw "AGENDAMIENTO_MASTER_KEY is required. Generate 32 random bytes and encode them as Base64."
}

$key = [Convert]::FromBase64String($encodedKey)
if ($key.Length -ne 32) { throw "AGENDAMIENTO_MASTER_KEY must decode to exactly 32 bytes." }
$plaintext = [IO.File]::ReadAllBytes((Resolve-Path $InputPath))
$nonce = [Security.Cryptography.RandomNumberGenerator]::GetBytes(12)
$ciphertext = [byte[]]::new($plaintext.Length)
$tag = [byte[]]::new(16)
$aes = [Security.Cryptography.AesGcm]::new($key, 16)
try {
    $aes.Encrypt($nonce, $plaintext, $ciphertext, $tag)
    $envelope = [ordered]@{
        version = 1
        algorithm = "AES-256-GCM"
        nonce = [Convert]::ToBase64String($nonce)
        tag = [Convert]::ToBase64String($tag)
        ciphertext = [Convert]::ToBase64String($ciphertext)
    } | ConvertTo-Json -Compress
    $directory = Split-Path $OutputPath -Parent
    if ($directory) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
    [IO.File]::WriteAllText($OutputPath, $envelope, [Text.UTF8Encoding]::new($false))
}
finally {
    $aes.Dispose()
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($key)
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($plaintext)
}

Write-Host "Encrypted secrets written to $OutputPath"
