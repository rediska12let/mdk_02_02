#!/usr/bin/env python3
"""Issue LOCAL DEVELOPMENT token; never use for real patients or production."""
import base64
import hashlib
import hmac
import json
import os
import sys
import time

def b64url(value: bytes) -> str:
    return base64.urlsafe_b64encode(value).decode().rstrip("=")

def main() -> None:
    if len(sys.argv) != 3 or sys.argv[2] not in {"Admin", "Clinician"}:
        raise SystemExit("Usage: python tools/issue_dev_token.py <actor-id> <Admin|Clinician>")
    secret = os.environ.get("Auth__SigningKey", "")
    if len(secret.encode("utf-8")) < 32:
        raise SystemExit("Set Auth__SigningKey to a random value of at least 32 bytes.")
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    # ClaimTypes.NameIdentifier and ClaimTypes.Role are mapped by JwtBearer.
    payload = {
        "sub": sys.argv[1],
        "role": sys.argv[2],
        "iss": "medical-records-local",
        "aud": "medical-records-api",
        "iat": now,
        "nbf": now,
        "exp": now + 3600,
    }
    part1 = b64url(json.dumps(header, separators=(",", ":")).encode())
    part2 = b64url(json.dumps(payload, separators=(",", ":")).encode())
    signing_input = f"{part1}.{part2}".encode()
    sig = hmac.new(secret.encode(), signing_input, hashlib.sha256).digest()
    print(f"{part1}.{part2}.{b64url(sig)}")

if __name__ == "__main__":
    main()
