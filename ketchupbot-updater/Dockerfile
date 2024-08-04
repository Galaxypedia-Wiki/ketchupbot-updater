FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ketchupbot-updater/ketchupbot-updater.csproj", "ketchupbot-updater/"]
RUN dotnet restore "ketchupbot-updater/ketchupbot-updater.csproj"
COPY . .
WORKDIR "/src/ketchupbot-updater"
RUN dotnet build "ketchupbot-updater.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ketchupbot-updater.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ketchupbot-updater.dll"]
CMD ["--turrets"]
