FROM mcr.microsoft.com/playwright:v1.57.0-noble

WORKDIR /src
COPY ledger11.webtests/ ./
COPY ledger11.webtests/package.json ledger11.webtests/package-lock.json* ./
RUN npm install
COPY ./ledger11.webtests .

COPY ledger11.webtests/wait-for.sh /src/wait-for.sh
RUN tr -d '\r' < ./wait-for.sh > ./wait-for-fixed.sh
RUN chmod +x ./wait-for-fixed.sh

CMD ["/src/wait-for-fixed.sh", "npx", "playwright", "test"]