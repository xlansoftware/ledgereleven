FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ledger11.sln ./
COPY ledger11.lib/ledger11.lib.csproj ledger11.lib/
COPY ledger11.web/ledger11.web.csproj ledger11.web/
COPY ledger11.service/ledger11.service.csproj ledger11.service/
COPY ledger11.service.SmtpEmailSenderService/ledger11.service.SmtpEmailSenderService.csproj ledger11.service.SmtpEmailSenderService/
COPY ledger11.model/ledger11.model.csproj ledger11.model/
COPY ledger11.data/ledger11.data.csproj ledger11.data/
COPY ledger11.tests/ledger11.tests.csproj ledger11.tests/
COPY ledger11.cli/ledger11.cli.csproj ledger11.cli/
RUN dotnet restore
COPY . .
CMD ["dotnet", "test", "--logger", "trx;LogFileName=/app/test-results/test-results.trx"]
