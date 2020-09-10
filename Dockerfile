ARG BUILDER_IMG=mcr.microsoft.com/dotnet/core/sdk:3.1.102
ARG RUNNER_IMG=mcr.microsoft.com/dotnet/core/runtime:3.1.0

FROM $BUILDER_IMG AS builder

WORKDIR /app

COPY ./src ./src
RUN dotnet restore -nowarn:msb3202,nu1503 src/EventStore.Shell -r linux-x64

RUN dotnet publish src/EventStore.Shell -c Release -r linux-x64 --no-restore --no-self-contained -clp:NoSummary -o /app/publish \
/p:PublishReadyToRun=true,PublishSingleFile=false

FROM $RUNNER_IMG AS runner

WORKDIR /app
COPY --from=builder /app/publish .

ENTRYPOINT ["./EventStore.Shell"]
CMD []
