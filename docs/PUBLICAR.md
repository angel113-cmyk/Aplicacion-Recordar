# Publicar Suelta (APK y AAB)

## 1. Para probar en tu teléfono (APK, sin keystore propio)

```powershell
dotnet publish src/Suelta.App -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

El archivo queda en `src/Suelta.App/bin/Release/net10.0-android/publish/com.suelta.app-Signed.apk`.
Se instala directo en el teléfono. Va firmado con la firma de depuración de .NET: sirve para
probar, **no** para Google Play.

Sin `-p:AndroidPackageFormat=apk`, el modo Release genera un **.aab** (el formato de Google Play).

## 2. Para Google Play (AAB firmado con tu propio keystore)

Play exige AAB en apps nuevas y que todas las versiones se firmen con la misma clave.

1. Crea el keystore **una sola vez**: `.\tools\crear-keystore.ps1`
   Guarda el archivo y las contraseñas fuera del repositorio. Si se pierden, no podrás actualizar la app.
2. Publica: `.\tools\publicar.ps1 -Keystore "$HOME\suelta.keystore" -Alias suelta -Version 4`
   Genera el `.apk` y el `.aab` firmados y los copia a Descargas.
3. Cada publicación necesita un `-Version` (ApplicationVersion) **mayor** que la anterior.
   El texto que se ve (`0.3.0`) es `ApplicationDisplayVersion` en `Suelta.App.csproj`.

Las contraseñas se piden sin mostrarse y se pasan al compilador por variables de entorno; no se
escriben en ningún archivo.

## 3. Integración continua (GitHub Actions)

`.github/workflows/ci.yml` corre las pruebas y publica el APK como artefacto en cada cambio.
Para que salga firmado con tu keystore, agrega estos secretos en GitHub (Settings → Secrets → Actions):

| Secreto | Valor |
|---|---|
| `KEYSTORE_BASE64` | El keystore en base64: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("suelta.keystore"))` |
| `KEYSTORE_ALIAS` | `suelta` |
| `KEYSTORE_PASSWORD` | Contraseña del keystore |
| `KEY_PASSWORD` | Contraseña de la clave |

Sin esos secretos, el APK de la CI sale con la firma de depuración.

## 4. Recorte de código

El modo Release recorta código que cree que no se usa. `Suelta.Core` está excluido del recorte
(`TrimmerRootAssembly`) porque SQLite lee las propiedades de `Recordatorio` por reflexión.
Después de cambiar dependencias, instala el APK de Release y revisa que abra, guarde y liste
recordatorios.

## 5. Errores

Los fallos se guardan en `errores.log` dentro de los datos de la app. Solo llevan el tipo de error
y los nombres de los métodos; nunca el mensaje ni lo que dijo la persona.
