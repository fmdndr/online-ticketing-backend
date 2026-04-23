#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# Generate RSA-2048 key pair for JWT signing (RS256).
#
# Usage:
#   chmod +x scripts/generate-rsa-keys.sh
#   ./scripts/generate-rsa-keys.sh
#
# Output:
#   keys/rsa-private.pem   (Identity.API only — NEVER commit this)
#   keys/rsa-public.pem    (all services — safe to distribute)
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

KEYS_DIR="$(cd "$(dirname "$0")/.." && pwd)/keys"
mkdir -p "$KEYS_DIR"

PRIVATE_KEY="$KEYS_DIR/rsa-private.pem"
PUBLIC_KEY="$KEYS_DIR/rsa-public.pem"

if [ -f "$PRIVATE_KEY" ]; then
    echo "⚠  RSA private key already exists at $PRIVATE_KEY — skipping."
    echo "   Delete it manually if you want to regenerate."
    exit 0
fi

echo "🔐 Generating RSA-2048 key pair..."

# Generate private key
openssl genpkey -algorithm RSA -out "$PRIVATE_KEY" -pkeyopt rsa_keygen_bits:2048

# Extract public key
openssl rsa -pubout -in "$PRIVATE_KEY" -out "$PUBLIC_KEY"

chmod 600 "$PRIVATE_KEY"
chmod 644 "$PUBLIC_KEY"

echo "✅ Keys generated:"
echo "   Private: $PRIVATE_KEY (chmod 600)"
echo "   Public:  $PUBLIC_KEY  (chmod 644)"
echo ""
echo "⚠  IMPORTANT: Never commit the private key to source control!"
echo "   The keys/ directory is already in .gitignore."
