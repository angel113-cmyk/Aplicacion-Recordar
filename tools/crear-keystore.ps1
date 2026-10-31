# Crea el keystore con el que se firma Suelta para Google Play (HT-15).
#
# GUARDA el archivo .keystore y las contraseñas en un lugar seguro, FUERA del repositorio.
# Si se pierden no se puede volver a publicar actualizaciones de la app en Google Play.

param(
    [string]$Ruta = "$HOME\suelta.keystore",
    [string]$Alias = "suelta",
    [string]$Keytool = "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe"
)

if (Test-Path $Ruta) { throw "Ya existe $Ruta. No lo piso: elige otra ruta o bórralo a propósito." }
if (-not (Test-Path $Keytool)) { throw "No encuentro keytool en $Keytool. Pasa la ruta con -Keytool." }

# keytool pide las contraseñas y los datos del certificado por su cuenta.
& $Keytool -genkeypair -v -keystore $Ruta -alias $Alias -keyalg RSA -keysize 2048 -validity 10000

Write-Host ""
Write-Host "Listo: $Ruta"
Write-Host "Haz una copia de seguridad y no lo subas a GitHub (los .keystore ya están en .gitignore)."
