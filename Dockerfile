FROM fsharp/fsharp
COPY . .
RUN mono ./.paket/paket.bootstrapper.exe
RUN mono ./.paket/paket.exe restore
CMD ["fsharpi", "app.docker.fsx"]