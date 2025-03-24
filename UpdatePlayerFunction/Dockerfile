FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["UpdatePlayerFunction/UpdatePlayerFunction.csproj", "UpdatePlayerFunction/"]
RUN dotnet restore "UpdatePlayerFunction/UpdatePlayerFunction.csproj"
COPY . .
WORKDIR "/src/UpdatePlayerFunction"
RUN dotnet build "UpdatePlayerFunction.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UpdatePlayerFunction.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0 AS final
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .