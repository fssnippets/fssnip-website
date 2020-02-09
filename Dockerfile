FROM mcr.microsoft.com/dotnet/core/sdk:3.1.101-buster

RUN apt-get update && \
    apt-get install -y nodejs npm && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

RUN ./build.sh -t download-data-dump && ./build.sh -t publish

EXPOSE 5000
ENV IP_ADDRESS=0.0.0.0
ENV PORT=5000
ENV LOG_LEVEL=Info

CMD ["./artifacts/fssnip"]