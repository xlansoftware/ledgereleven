# Build the Docker image
docker build -t ledger11-web -f ./Dockerfile.ledger-eleven-app ../../src
docker build -t ledger11-authserver -f ./Dockerfile.ledger-eleven-auth-server ../../src

# Debug the size of the context
docker build -t debug-context -f Dockerfile.debug  ../../src
docker run --rm debug-context

# Tag
git tag test-v1.0.0
git push origin test-v1.0.0
