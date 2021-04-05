# PoC Demo Translator

This repository represents a demo translator used in the proof of concept (PoC)
of WirePact. The demo translator intercepts - as an external authentication handler - any envoy communication. The envoy
proxy in injected as a sidecar into a Kubernetes deployment toghether with this translator.

When the enhanced service receives a call via Envoy, this translator will modify the authorization header. To use the
translator, the origin service must transmit a valid [Zitadel](https://zitadel.ch) OIDC token. The translator will then
check if the token is valid and replace the token with static basic auth credentials configured in a Kubernetes secret.

## Config

The following environment variables can be set:

- `PORT` : Define a "public" port (listening on `0.0.0.0`) on which the translator listens for gRPC communication.
- `LOCAL_PORT`: Define a "local" port (only listening on `localhost`, useful in sidecar mode in a pod) on which the
  translator listens for gRPC communication.
- `CREDENTIALS_SECRET_NAME`: The name of the secret (in the same namespace as the app) that contains the basic
  authentication credentials.
- `CREDENTIALS_USER_PROPERTY`: Optional name of the property that returns the username from the secret. Defaults
  to `username`.
- `CREDENTIALS_PASS_PROPERTY`: Optional name of the property that returns the password from the secret. Defaults
  to `password`.

Required variables are:

- Either `PORT` or `LOCAL_PORT` (otherwise, the translator cannot be communicated with)
- `CREDENTIALS_SECRET_NAME`

## Development

To run this demo translator in "dev" (local) mode:

1. Locate the folder `tests\dev-setup`
2. Start Envoy and the legacy application with `docker-compose up -d`
3. Start the translator with `PORT=5000` to make it accessable from the docker host via port 5000
4. Fetch an OIDC access token from Zitadel (_note_: you need a Zitadel account):
    - Grant Type: `Authorization Code`
    - Authrorization Url: `https://accounts.zitadel.ch/oauth/v2/authorize`
    - Access Token Url: `https://api.zitadel.ch/oauth/v2/token`
    - Client Id: `102538020334461370@poc_showcase`
    - PKCE: `true`
    - Scopes: `openid email profile`
5. Call the API via `localhost:8080/orders`
