# API de respaldo de Suelta (HT-18). Se construye desde la raíz del repositorio:
#   docker build -t suelta-api .
#   docker run -p 8080:8080 -v suelta-datos:/datos -e Proxy__ConfiarEnCabeceras=false suelta-api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /src
COPY src/Suelta.Core src/Suelta.Core
COPY src/Suelta.Api src/Suelta.Api
RUN dotnet publish src/Suelta.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilar /app .

# La base de datos y las copias diarias viven en un volumen, para que sobrevivan a las actualizaciones.
ENV Datos__Ruta=/datos/suelta-api.db \
    Datos__CarpetaRespaldos=/datos/respaldos \
    ASPNETCORE_URLS=http://+:8080
VOLUME /datos
EXPOSE 8080

ENTRYPOINT ["dotnet", "Suelta.Api.dll"]
