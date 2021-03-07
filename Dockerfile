# Starting image
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

# Set working directory to /app
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./

# Install packages
RUN dotnet add package Newtonsoft.Json --version 12.0.3
RUN dotnet add package Confluent.Kafka --version 1.6.2
RUN dotnet add package LiteDB --version 5.0.10
RUN dotnet add package Docker.DotNet --version 3.125.4
# RUN dotnet add package Devart.Data.Oracle.EFCore --version 9.14.1204
# RUN dotnet add package System.Diagnostics.PerformanceCounter --version 5.0.1
RUN dotnet add package xunit --version 2.4.1

# Copy everything else and build
COPY . ./

# Publish the project to a DLL
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:5.0
WORKDIR /app
COPY --from=build-env /app/out .

# ENV KAFKA_BROKERS=192.168.1.237:9092
# ENV BROADCAST_TOPIC=Tangible.broadcast.1
# ENV ADVERTISED_HOST_NAME=192.168.1.237
# ENV PORT_NUMBER=8000
# ENV INTERFACE=0.0.0.0
# ENV CERT_EXPIRE_DAYS=365
# ENV CLUSTER_ID=Tangible#1
# ENV REQUEST_TOPIC=Tangible.request.1
# ENV HEARTBEAT_S=3

# Run the program
# ENTRYPOINT ["dotnet", "run", "EC.MS.csproj"]
ENTRYPOINT ["dotnet", "Node.dll"]
