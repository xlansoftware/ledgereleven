FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM node:23-slim AS build_client
WORKDIR /src
COPY ledger11.client/package.json ledger11.client/package-lock.json* ./
RUN npm install --force
COPY ./ledger11.client .
RUN NODE_ENV=production && npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY personalbudget.sln ./
COPY ledger11.web/ledger11.web.csproj ledger11.web/
COPY ledger11.service/ledger11.service.csproj ledger11.service/
COPY ledger11.model/ledger11.model.csproj ledger11.model/
COPY ledger11.data/ledger11.data.csproj ledger11.data/
COPY ledger11.tests/ledger11.tests.csproj ledger11.tests/
COPY ledger11.cli/ledger11.cli.csproj ledger11.cli/
RUN dotnet restore
COPY . .
WORKDIR /src/ledger11.web
RUN rm -rf ./wwwroot/app/*
COPY --from=build_client /src/dist/ ./wwwroot/app
RUN dotnet publish -c Release -o /app/publish
WORKDIR /src/ledger11.cli
RUN dotnet publish -c Release --self-contained true /p:PublishSingleFile=true -o /app/cli

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/cli .
ENTRYPOINT ["dotnet", "ledger11.web.dll"]