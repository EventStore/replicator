ARG RUNNER_IMG

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
ARG TARGETARCH

WORKDIR /app

ARG RUNTIME
RUN curl -sL https://deb.nodesource.com/setup_19.x | bash - \
 && apt-get install -y --no-install-recommends nodejs \
 && npm install -g yarn 

COPY ./src/Directory.Build.props ./src/*/*.csproj ./src/
RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore ./src/es-replicator -nowarn:msb3202,nu1503 -a $TARGETARCH

COPY ./src/es-replicator/ClientApp/package.json ./src/es-replicator/ClientApp/
COPY ./src/es-replicator/ClientApp/yarn.lock ./src/es-replicator/ClientApp/
RUN cd ./src/es-replicator/ClientApp && yarn install

FROM builder as publish
ARG TARGETARCH
COPY ./src ./src
RUN dotnet publish ./src/es-replicator -c Release -a $TARGETARCH -clp:NoSummary --no-self-contained -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runner

WORKDIR /app
COPY --from=publish /app/publish .

ENV ALLOWED_HOSTS "*"
ENV ASPNETCORE_URLS "http://*:5000"

EXPOSE 5000
ENTRYPOINT ["dotnet", "es-replicator.dll"]
