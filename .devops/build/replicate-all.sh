#!/bin/sh

OUTPUT_FILE="/etc/litestream.yml"
DB_DIR="/data"

REMOTE_USER="xlansoft"
REMOTE_HOST="neptun.superhosting.bg"
REMOTE_PORT="1022"
REMOTE_ROOT="/home/xlansoft/xlansoftware.com/ledgerbackup/preview"

# Start with base configuration
echo "addr: \":9090\"" > "$OUTPUT_FILE"
echo "dbs:" >> "$OUTPUT_FILE"

# For each .db file, append config block
for DB_FILE in "$DB_DIR"/*.db; do
  DB_NAME=$(basename "$DB_FILE")
  cat <<EOF >> "$OUTPUT_FILE"
  - path: $DB_FILE
    replicas:
      - type: sftp
        host: ${REMOTE_HOST}:${REMOTE_PORT}
        user: ${REMOTE_USER}
        password: \${SFTP_PASSWORD}
        path: ${REMOTE_ROOT}/${DB_NAME}
EOF
done

# Upload with lftp
echo "Uploading litestream.yml using lftp..."
lftp -d -u "${REMOTE_USER},${SFTP_PASSWORD}" sftp://${REMOTE_HOST}:${REMOTE_PORT} <<EOF
cd ${REMOTE_ROOT}
put ${OUTPUT_FILE} -o litestream.yml
bye
EOF