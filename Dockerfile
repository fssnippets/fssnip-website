FROM mcr.microsoft.com/dotnet/core/sdk:3.1.101-buster as base

RUN apt-get update && \
    apt-get install -y nodejs npm && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

RUN ./build.sh -t deploy

FROM mcr.microsoft.com/dotnet/core/runtime:3.1.101-buster

COPY --from=base /wwwroot /wwwroot
WORKDIR /wwwroot/deploy_0

ENV FSSNIP_HOME_DIR=/wwwroot/deploy_0
ENV LOG_LEVEL=Info
ENV DISABLE_RECAPTCHA=true
#ENV CUSTOMCONNSTR_FSSNIP_STORAGE=???
#ENV RECAPTCHA_SECRET=???
ENV IP_ADDRESS=0.0.0.0
ENV PORT=5000
EXPOSE 5000

CMD ["./bin/fssnip"]