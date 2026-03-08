# ── Build Stage ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY RestaurantService.API/*.csproj ./RestaurantService.API/
RUN dotnet restore RestaurantService.API/RestaurantService.API.csproj

COPY RestaurantService.API/ ./RestaurantService.API/
RUN dotnet publish RestaurantService.API/RestaurantService.API.csproj -c Release -o /app/publish

# ── Runtime Stage ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 5000

ENTRYPOINT ["dotnet", "RestaurantService.API.dll"]
