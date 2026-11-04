# Servidor de respaldo (API + SQLite)

El respaldo es **opcional**. La app funciona completa sin servidor: los recordatorios viven en el
teléfono (SQLite) y el servidor solo guarda una copia para poder recuperarlos en otro teléfono.

La API usa **SQLite** (no PostgreSQL): un solo archivo, sin servicio de base de datos aparte.
Es suficiente para esta app: una cuenta son unas decenas de filas pequeñas.

## Cómo funciona

| Endpoint | Qué hace |
|---|---|
| `POST /v1/respaldo` | Crea una cuenta y devuelve el código de recuperación (`XXXX-XXXX-XXXX-XXXX`). |
| `POST /v1/respaldo/sincronizar` | Envía los cambios del teléfono y devuelve los de otros teléfonos. Gana la última modificación. |
| `DELETE /v1/respaldo` | Borra la cuenta y todos sus recordatorios. |
| `GET /salud` | Responde `ok` si el servidor está vivo. |

- **Identidad anónima:** el código de 80 bits viaja en la cabecera `X-Codigo`. El servidor guarda solo su
  huella (SHA-256), no el código.
- **Límites:** 120 peticiones por minuto por dirección y por código; 10 cuentas nuevas por hora por dirección.
  Se ajustan con `Limites__PorMinuto` y `Limites__CuentasPorHora`.
- **Copias de seguridad:** una cada día (`VACUUM INTO`), se conservan 7, en `Datos__CarpetaRespaldos`.
  Para desactivarlas: `Respaldos__Activo=false`.

## Correrlo en tu PC

```powershell
dotnet run --project src/Suelta.Api --urls http://0.0.0.0:5080
```

La base queda en `suelta-api.db` junto a donde lo ejecutes. Para probar con el teléfono:

1. Instala una **compilación de desarrollo** de la app (`dotnet build -f net10.0-android -t:Install`
   con el teléfono por USB). Solo esas compilaciones aceptan `http://`.
2. En la app: Ajustes → Servidor → `http://IP-DE-TU-PC:5080` (el teléfono y la PC en la misma red wifi;
   puede que tengas que permitir el puerto 5080 en el firewall de Windows).

La app de **Release** exige `https://`, porque el código de recuperación viaja en cada petición.

## Publicarlo (decisión pendiente: dónde alojarlo)

Sirve cualquier lugar que corra contenedores y dé HTTPS: Fly.io, Railway, Render, una VPS con Caddy o nginx.

```bash
docker build -t suelta-api .
docker run -p 8080:8080 -v suelta-datos:/datos suelta-api
```

- El volumen `/datos` guarda la base y las copias diarias: **sin volumen se pierde todo al actualizar**.
- Si hay un proxy delante (casi siempre), activa `Proxy__ConfiarEnCabeceras=true` para que el límite
  de peticiones use la dirección real de cada persona. No lo actives si la API está expuesta directamente.
- Pon el servicio en una sola instancia: SQLite escribe en un archivo local.

El `Dockerfile` está escrito pero todavía no lo probé construyendo la imagen.

## Migraciones de la base de datos

```powershell
dotnet ef migrations add NombreDelCambio --project src/Suelta.Api --output-dir Datos/Migraciones
```

La API aplica las migraciones pendientes al arrancar.
