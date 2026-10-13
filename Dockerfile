FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["chess-console.Core/chess-console.Core.csproj", "chess-console.Core/"]
COPY ["chess-console.Server/chess-console.Server.csproj", "chess-console.Server/"]
RUN dotnet restore "chess-console.Server/chess-console.Server.csproj"

COPY . .
WORKDIR "/src/chess-console.Server"
RUN dotnet publish "chess-console.Server.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "chess-console.Server.dll"]
