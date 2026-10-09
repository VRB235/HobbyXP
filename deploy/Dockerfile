# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json ./
COPY HobbyXP.sln ./
COPY src/HobbyXP.Core/HobbyXP.Core.csproj src/HobbyXP.Core/
COPY src/HobbyXP.Web/HobbyXP.Web.csproj src/HobbyXP.Web/
RUN dotnet restore src/HobbyXP.Web/HobbyXP.Web.csproj
COPY src/HobbyXP.Core/ src/HobbyXP.Core/
COPY src/HobbyXP.Web/ src/HobbyXP.Web/
RUN dotnet publish src/HobbyXP.Web/HobbyXP.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN mkdir -p /data/media
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HobbyXP.Web.dll"]
