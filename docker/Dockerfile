FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/MedulaOnKontrol.Web/MedulaOnKontrol.Web.csproj --locked-mode
RUN dotnet publish src/MedulaOnKontrol.Web/MedulaOnKontrol.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 fonts-lato curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080 MEDULA_SECRET_DIR=/run/medula-secrets TZ=Europe/Istanbul
RUN mkdir -p /app/logs && chown -R 1654:1654 /app
USER 1654
EXPOSE 8080
ENTRYPOINT ["dotnet","MedulaOnKontrol.Web.dll"]
