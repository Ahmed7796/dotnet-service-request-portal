# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `restore` is cached between builds
COPY src/ServiceDesk.Api/ServiceDesk.Api.csproj                       src/ServiceDesk.Api/
COPY src/ServiceDesk.Application/ServiceDesk.Application.csproj       src/ServiceDesk.Application/
COPY src/ServiceDesk.Domain/ServiceDesk.Domain.csproj                 src/ServiceDesk.Domain/
COPY src/ServiceDesk.Infrastructure/ServiceDesk.Infrastructure.csproj src/ServiceDesk.Infrastructure/
RUN dotnet restore src/ServiceDesk.Api/ServiceDesk.Api.csproj

# Copy the rest and publish
COPY src/ ./src/
RUN dotnet publish src/ServiceDesk.Api/ServiceDesk.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# .NET images listen on 8080 and ship a non-root "app" user
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ServiceDesk.Api.dll"]