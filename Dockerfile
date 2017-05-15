FROM fsharp/fsharp
ENV source /src
WORKDIR ${source}
ADD . $source
RUN mono ./.paket/paket.bootstrapper.exe
RUN mono ./.paket/paket.exe restore
CMD ["fsharpi", "--debug", "docker.fsx"]