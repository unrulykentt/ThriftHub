FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ThriftHub/ThriftHub.csproj", "ThriftHub/"]
RUN dotnet restore "ThriftHub/ThriftHub.csproj"
COPY . .
WORKDIR "/src/ThriftHub"
RUN dotnet build "ThriftHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ThriftHub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ThriftHub/thriftHub.db thrifthub.db
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_hostBuilder__reloadConfigOnChange=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "ThriftHub.dll"]


