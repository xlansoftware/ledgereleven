#!/bin/sh

OUTPUT_FILE="/etc/litestream.yml"
DB_DIR="/data"

# Start with base configuration
echo "addr: \":9090\"" > "$OUTPUT_FILE"

# For each .db file, append config block
echo "dbs:" >> "$OUTPUT_FILE"
for DB_FILE in "$DB_DIR"/*.db; do
  DB_NAME=$(basename "$DB_FILE")
  cat <<EOF >> "$OUTPUT_FILE"
  - path: $DB_FILE
    replicas:
      - type: sftp
        host: neptun.superhosting.bg:1022
        user: xlansoft
        password: \${SFTP_PASSWORD}
        path: /home/xlansoft/xlansoftware.com/ledgerbackup/preview/${DB_NAME}
EOF
done
