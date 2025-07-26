FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN addgroup -g 1001 appgroup && \
    adduser -u 1001 -G appgroup -s /bin/sh -D appuser

RUN apk add --no-cache icu-libs tzdata curl ca-certificates && \
    update-ca-certificates

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["Nemui.sln", "./"]
COPY ["Nemui.Api/Nemui.Api.csproj", "Nemui.Api/"]
COPY ["Nemui.Application/Nemui.Application.csproj", "Nemui.Application/"]
COPY ["Nemui.Infrastructure/Nemui.Infrastructure.csproj", "Nemui.Infrastructure/"]
COPY ["Nemui.Shared/Nemui.Shared.csproj", "Nemui.Shared/"]

RUN dotnet restore "Nemui.Api/Nemui.Api.csproj"

COPY . .

FROM build AS publish
WORKDIR /src/Nemui.Api

RUN dotnet publish "Nemui.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_ENVIRONMENT=Staging
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

ENTRYPOINT ["dotnet", "Nemui.Api.dll"] 