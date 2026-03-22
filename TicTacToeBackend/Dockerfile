# Stage 1: Build Environment (Heavy SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy just the csproj first to cache the NuGet restore step
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the code and build the release
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime Environment (Lightweight, Secure)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App

# Copy only the compiled output from the build stage
COPY --from=build-env /App/out .

# .NET 8 defaults to port 8080 for non-root runtime images
EXPOSE 8080

# Define the entry point for the container
ENTRYPOINT ["dotnet", "TicTacToeBackend.dll"]