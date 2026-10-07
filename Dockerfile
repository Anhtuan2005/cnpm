FROM node:22-alpine AS assets
WORKDIR /src/frontend
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ClientAssets ./ClientAssets
COPY src/frontend/scripts ./scripts
COPY src/backend/wwwroot /src/backend/wwwroot
RUN npm run build:assets

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/backend/EcommerceApp.csproj ./src/backend/
RUN dotnet restore src/backend/EcommerceApp.csproj
COPY . .
COPY --from=assets /src/backend/wwwroot ./src/backend/wwwroot
RUN dotnet publish src/backend/EcommerceApp.csproj --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p App_Data/DataProtectionKeys logs wwwroot/uploads && chown -R $APP_UID App_Data logs wwwroot/uploads
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EcommerceApp.dll"]
