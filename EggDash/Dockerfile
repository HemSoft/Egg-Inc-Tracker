FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EggDash/EggDash.csproj", "EggDash/"]
COPY ["EggDash.Client/EggDash.Client.csproj", "EggDash.Client/"]
RUN dotnet restore "EggDash/EggDash.csproj"
COPY . .
WORKDIR "/src/EggDash"
RUN dotnet build "EggDash.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EggDash.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EggDash.dll"]