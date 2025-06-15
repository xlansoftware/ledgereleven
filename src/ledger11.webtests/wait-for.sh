#!/bin/sh

set -e

echo "Waiting for APP_URL: $APP_URL"
until curl -s --head --fail "$APP_URL"; do
  echo "Waiting for app ($APP_URL)..."
  sleep 2
done

echo "Waiting for AUTH_URL: $AUTH_URL"
until curl -s --head --fail "$AUTH_URL"; do
  echo "Waiting for auth ($AUTH_URL)..."
  sleep 2
done

echo "Both services are up!"
exec "$@"
