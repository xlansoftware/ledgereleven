#!/bin/sh

set -e

wait_for_url() {
  local url="$1"
  local service_name="$2"
  local max_attempts=10
  local attempt=0

  echo "Waiting for $service_name at: $url"
  
  until curl -s --head --fail "$url"; do
    attempt=$((attempt + 1))
    if [ $attempt -ge $max_attempts ]; then
      echo "Error: $service_name not available after $max_attempts attempts"
      exit 1
    fi
    echo "Waiting for $service_name ($url)... attempt $attempt/$max_attempts"
    sleep 2
  done

  echo "$service_name is up!"
}

wait_for_url "$APP_URL" "APP_URL"

echo "Services are up!"
exec "$@"