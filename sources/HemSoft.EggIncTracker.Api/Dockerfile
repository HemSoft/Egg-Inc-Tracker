FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EggIncTrackerApi/EggIncTrackerApi.csproj", "EggIncTrackerApi/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["Domain/Domain.csproj", "Domain/"]
RUN dotnet restore "EggIncTrackerApi/EggIncTrackerApi.csproj"
COPY . .
WORKDIR "/src/EggIncTrackerApi"
RUN dotnet build "EggIncTrackerApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EggIncTrackerApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EggIncTrackerApi.dll"]