FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim as base

RUN apt-get update && \
    apt-get install -y nodejs npm && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

RUN ./build.sh -t deploy

FROM mcr.microsoft.com/dotnet/core/runtime:8.0-bookworm-slim

WORKDIR /wwwroot
COPY --from=base /wwwroot/deploy_0 .
RUN mkdir -p /wwwroot/data

ENV CUSTOMCONNSTR_FSSNIP_STORAGE=
ENV RECAPTCHA_SECRET=
ENV DISABLE_RECAPTCHA=false
ENV FSSNIP_HOME_DIR=/wwwroot
ENV FSSNIP_DATA_DIR=/wwwroot/data
ENV LOG_LEVEL=Info
ENV IP_ADDRESS=0.0.0.0
ENV PORT=5000
EXPOSE 5000

ENTRYPOINT ["./bin/fssnip"]
