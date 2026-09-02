FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WicStock.Web.csproj", "./"]
RUN dotnet restore "WicStock.Web.csproj"

COPY . .
RUN dotnet publish "WicStock.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
