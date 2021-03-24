# Starting image
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

# Set working directory to /app
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./

# Copy everything else and build
COPY . ./

# Publish the project to a DLL
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:5.0
WORKDIR /app
COPY --from=build-env /app/out .

# Run the program
ENTRYPOINT ["dotnet", "Driver.dll"]
