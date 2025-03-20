# Usa la imagen oficial de .NET para construir la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el archivo del proyecto (.csproj)
COPY ["PayMeChat_V1_Backend/PayMeChat_V_1.csproj", "PayMeChat_V1_Backend/"]
WORKDIR "/src/PayMeChat_V1_Backend"
RUN dotnet restore "PayMeChat_V_1.csproj"

# Copiar todo el código fuente
COPY . .
RUN dotnet build "PayMeChat_V_1.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "PayMeChat_V_1.csproj" -c Release -o /app/publish

# Imagen final con la app publicada
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Establecer la variable de entorno PORT (Render usa esto automáticamente)
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PayMeChat_V_1.dll"]
