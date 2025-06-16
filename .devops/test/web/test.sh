cd certs
./generate-test-certificates.sh
cd ..
docker-compose run --build --rm test