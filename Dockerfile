# Usa la imagen oficial de .NET para construir la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PayMeChat_V1_Backend.csproj", "./"]
RUN dotnet restore "PayMeChat_V1_Backend.csproj"

COPY . .
WORKDIR "/src/"
RUN dotnet build "PayMeChat_V1_Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PayMeChat_V1_Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PayMeChat_V1_Backend.dll"]
