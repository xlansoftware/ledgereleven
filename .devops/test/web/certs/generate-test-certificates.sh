# Create certificate for auth
openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 -nodes \
  -keyout auth.key -out auth.crt -subj "/CN=auth" \
  -addext "subjectAltName=DNS:auth,DNS:localhost,IP:127.0.0.1"

# Convert to PFX format (needed by ASP.NET)
openssl pkcs12 -export -out auth.pfx -inkey auth.key -in auth.crt -passout pass:

# Create certificate for ledgerapp
openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 -nodes \
  -keyout ledgerapp.key -out ledgerapp.crt -subj "/CN=ledgerapp" \
  -addext "subjectAltName=DNS:ledgerapp,DNS:localhost,IP:127.0.0.1"

# Convert to PFX format
openssl pkcs12 -export -out ledgerapp.pfx -inkey ledgerapp.key -in ledgerapp.crt -passout pass:
