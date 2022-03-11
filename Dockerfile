ARG BUILDER_IMG=mcr.microsoft.com/dotnet/sdk:6.0
ARG RUNNER_IMG=mcr.microsoft.com/dotnet/aspnet:6.0
ARG RUNTIME=linux-x64

FROM $BUILDER_IMG AS builder

WORKDIR /app

ARG RUNTIME
RUN curl -sL https://deb.nodesource.com/setup_16.x | bash - \
 && apt-get install -y --no-install-recommends nodejs \
 && npm install -g yarn 

COPY ./src/Directory.Build.props ./src/*/*.csproj ./src/
RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore ./src/es-replicator -nowarn:msb3202,nu1503 -r $RUNTIME

COPY ./src/es-replicator/ClientApp/package.json ./src/es-replicator/ClientApp/
COPY ./src/es-replicator/ClientApp/yarn.lock ./src/es-replicator/ClientApp/
RUN cd ./src/es-replicator/ClientApp && yarn install

FROM builder as publish
ARG RUNTIME
COPY ./src ./src
RUN dotnet publish ./src/es-replicator -c Release -r $RUNTIME -clp:NoSummary --no-self-contained -o /app/publish

FROM $RUNNER_IMG AS runner

WORKDIR /app
COPY --from=publish /app/publish .

ENV ALLOWED_HOSTS "*"
ENV ASPNETCORE_URLS "http://*:5000"

EXPOSE 5000
ENTRYPOINT ["dotnet es-replicator.dll"]
