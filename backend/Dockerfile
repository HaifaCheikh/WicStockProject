FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WicStock.Api.csproj", "./"]
RUN dotnet restore "WicStock.Api.csproj"

COPY . .
RUN dotnet publish "WicStock.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "WicStock.dll"]
