ARG VERSION="0.0.0"

### Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app

COPY . ./

RUN ./build.sh --no-logo --target Publish --configuration Release --version ${VERSION}

### Deploy
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final

LABEL org.opencontainers.image.source=https://github.com/WirePact/poc-demo-translator

WORKDIR /app

COPY --from=build /app/artifacts .

ENTRYPOINT [ "dotnet" ]
CMD [ "WirePact.PoC.Translator.dll" ]
