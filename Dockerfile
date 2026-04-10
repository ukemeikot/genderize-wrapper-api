FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["GenderClassifyApi.sln", "./"]
COPY ["src/GenderClassifyApi/GenderClassifyApi.csproj", "src/GenderClassifyApi/"]
COPY ["tests/GenderClassifyApi.Tests/GenderClassifyApi.Tests.csproj", "tests/GenderClassifyApi.Tests/"]

RUN dotnet restore "GenderClassifyApi.sln"

COPY . .

RUN dotnet publish "src/GenderClassifyApi/GenderClassifyApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GenderClassifyApi.dll"]
