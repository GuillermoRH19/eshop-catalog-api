FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# SkiaSharp (usado por QuestPDF para generar los PDF) necesita fontconfig para renderizar texto
# en imágenes Linux mínimas; sin esto falla en runtime al generar el primer PDF.
USER root
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Orders.API/Orders.API.csproj", "src/Orders.API/"]
COPY ["src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/BuildingBlocks/"]
RUN dotnet restore "src/Orders.API/Orders.API.csproj"

COPY . .
WORKDIR "/src/src/Orders.API"
RUN dotnet build "Orders.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Orders.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orders.API.dll"]
