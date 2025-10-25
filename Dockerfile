# ===== Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia el .csproj, restaura (aprovecha cache de Docker)
COPY *.csproj ./
RUN dotnet restore

# Copia el resto y publica en Release
COPY . ./
RUN dotnet publish -c Release -o /app/out

# ===== Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/out .

# Cambia si tu .csproj tiene otro nombre
ENV DOTNET_RUNNING_APP=LifeHub.dll

# Render inyecta $PORT; servimos en 0.0.0.0:$PORT
CMD ASPNETCORE_URLS=http://+:$PORT dotnet $DOTNET_RUNNING_APP