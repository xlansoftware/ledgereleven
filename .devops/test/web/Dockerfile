FROM mcr.microsoft.com/playwright:v1.53.0-noble

WORKDIR /src
COPY ledger11.webtests/ ./
COPY ledger11.webtests/package.json ledger11.webtests/package-lock.json* ./
RUN npm install
COPY ./ledger11.webtests .

COPY ledger11.webtests/wait-for.sh /wait-for.sh
RUN chmod +x /wait-for.sh

# CMD ["/wait-for.sh", "npx", "playwright", "test"]
CMD ["sh", "-c", "update-ca-certificates && /wait-for.sh npx playwright test"]