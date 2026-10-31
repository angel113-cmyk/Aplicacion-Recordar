# Publica Suelta firmada, como APK (instalar directo) y como AAB (Google Play) (HT-15).
#
# Uso:   .\tools\publicar.ps1 -Keystore "$HOME\suelta.keystore" -Alias suelta -Version 4
# Pide las contraseñas sin mostrarlas y no las guarda en ningún archivo.

param(
    [Parameter(Mandatory)] [string]$Keystore,
    [string]$Alias = "suelta",
    [int]$Version = 0,                 # ApplicationVersion: debe subir en cada publicación
    [string]$Salida = "$HOME\Downloads"
)

if (-not (Test-Path $Keystore)) { throw "No existe el keystore: $Keystore" }

function Leer-Secreto($texto) {
    $s = Read-Host $texto -AsSecureString
    [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
}

$env:SUELTA_STORE_PASS = Leer-Secreto "Contraseña del keystore"
$env:SUELTA_KEY_PASS = Leer-Secreto "Contraseña de la clave (Enter si es la misma)"
if (-not $env:SUELTA_KEY_PASS) { $env:SUELTA_KEY_PASS = $env:SUELTA_STORE_PASS }

$proyecto = Join-Path $PSScriptRoot "..\src\Suelta.App"
$publicado = Join-Path $proyecto "bin\Release\net10.0-android\publish"

foreach ($formato in "apk", "aab") {
    $args = @(
        "publish", $proyecto, "-f", "net10.0-android", "-c", "Release",
        "-p:AndroidPackageFormat=$formato",
        "-p:AndroidKeyStore=true",
        "-p:AndroidSigningKeyStore=$Keystore",
        "-p:AndroidSigningKeyAlias=$Alias",
        "-p:AndroidSigningStorePass=env:SUELTA_STORE_PASS",
        "-p:AndroidSigningKeyPass=env:SUELTA_KEY_PASS"
    )
    if ($Version -gt 0) { $args += "-p:ApplicationVersion=$Version" }

    dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "Falló la publicación como $formato." }
}

Remove-Item Env:SUELTA_STORE_PASS, Env:SUELTA_KEY_PASS

Get-ChildItem $publicado -Include "*-Signed.apk", "*-Signed.aab" -Recurse | ForEach-Object {
    Copy-Item $_.FullName $Salida -Force
    Write-Host "Listo: $(Join-Path $Salida $_.Name)"
}
