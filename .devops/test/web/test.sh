# docker run --rm mcr.microsoft.com/dotnet/aspnet:9.0 id app
# docker compose exec ledgerapp touch /data/test
# docker compose exec ledgerapp ls -ld /data
# docker compose down -v
docker-compose run --build --rm test