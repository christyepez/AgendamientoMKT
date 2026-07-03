FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/AgendamientoMKT.Api/AgendamientoMKT.Api.csproj src/AgendamientoMKT.Api/
RUN dotnet restore src/AgendamientoMKT.Api/AgendamientoMKT.Api.csproj
COPY . .
RUN dotnet publish src/AgendamientoMKT.Api/AgendamientoMKT.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AgendamientoMKT.Api.dll"]
