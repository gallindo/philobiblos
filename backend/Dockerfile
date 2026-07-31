FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props backend/Directory.Build.props
COPY backend/Philobiblos.sln backend/Philobiblos.sln
COPY backend/src/Philobiblos.Domain/Philobiblos.Domain.csproj backend/src/Philobiblos.Domain/
COPY backend/src/Philobiblos.Application/Philobiblos.Application.csproj backend/src/Philobiblos.Application/
COPY backend/src/Philobiblos.Infrastructure/Philobiblos.Infrastructure.csproj backend/src/Philobiblos.Infrastructure/
COPY backend/src/Philobiblos.Api/Philobiblos.Api.csproj backend/src/Philobiblos.Api/
RUN dotnet restore backend/src/Philobiblos.Api/Philobiblos.Api.csproj

COPY backend/src/Philobiblos.Domain/ backend/src/Philobiblos.Domain/
COPY backend/src/Philobiblos.Application/ backend/src/Philobiblos.Application/
COPY backend/src/Philobiblos.Infrastructure/ backend/src/Philobiblos.Infrastructure/
COPY backend/src/Philobiblos.Api/ backend/src/Philobiblos.Api/
WORKDIR /src/backend/src/Philobiblos.Api
RUN dotnet publish Philobiblos.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
RUN useradd -m -d /app -s /bin/sh appuser
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Philobiblos.Api.dll"]
