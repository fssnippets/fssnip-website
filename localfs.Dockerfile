FROM mcr.microsoft.com/dotnet/core/sdk:3.1.201-buster as base

RUN apt-get update && \
    apt-get install -y nodejs npm && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

RUN ./build.sh -t download-data-dump && ./build.sh -t deploy

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim

WORKDIR /wwwroot
COPY --from=base /wwwroot/deploy_0 .
COPY --from=base /src/data data/

ENV FSSNIP_HOME_DIR=/wwwroot
ENV FSSNIP_DATA_DIR=/wwwroot/data
ENV LOG_LEVEL=Info
ENV DISABLE_RECAPTCHA=true
ENV IP_ADDRESS=0.0.0.0
ENV PORT=5000
EXPOSE 5000

ENTRYPOINT ["./bin/fssnip"]
