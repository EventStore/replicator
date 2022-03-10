ARG BUILDER_IMG=mcr.microsoft.com/dotnet/sdk:6.0
ARG RUNNER_IMG=mcr.microsoft.com/dotnet/aspnet:6.0
ARG RUNTIME=linux-x64

FROM $BUILDER_IMG AS builder

WORKDIR /app

# add Node.js, npm and yarn
RUN curl -sL https://deb.nodesource.com/setup_16.x | bash - \
 && apt-get install -y --no-install-recommends nodejs \
 && npm install -g yarn 

# copy csproj and restore as distinct layers
COPY ./src/Directory.Build.props ./src/*/*.csproj ./src/
RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore ./src/es-replicator -nowarn:msb3202,nu1503 --runtime=${RUNTIME}

# run yarn install as a separate layer
COPY ./src/es-replicator/ClientApp/package.json ./src/es-replicator/ClientApp/
COPY ./src/es-replicator/ClientApp/yarn.lock ./src/es-replicator/ClientApp/
RUN cd ./src/es-replicator/ClientApp && yarn install

# copy everything else, build and publish the final binaries
COPY ./src ./src
RUN dotnet publish ./src/es-replicator -c Release -r linux-x64 --no-restore --no-self-contained -clp:NoSummary -o /app/publish

# Create final runtime image
FROM $RUNNER_IMG AS runner

#USER 1001

WORKDIR /app
COPY --from=builder /app/publish .

ENV ALLOWED_HOSTS "*"
ENV ASPNETCORE_URLS "http://*:5000"

EXPOSE 5000
ENTRYPOINT ["dotnet es-replicator.dll"]
