# Stage 1: Build the code (The Factory)
# We use the full SDK image to compile the code
# Since you are on .NET 10, we use the 10.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file first (caching layer)
COPY ["WeatherApp.csproj", "./"]
# Restore dependencies
RUN dotnet restore "WeatherApp.csproj"

# Copy the rest of the code
COPY . .
# Build the release version
RUN dotnet publish "WeatherApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run the app (The Tent)
# We use a lighter "Runtime" image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# Expose the standard HTTP port
EXPOSE 8080

# Copy the built files from Stage 1
COPY --from=build /app/publish .

# Start the app
ENTRYPOINT ["dotnet", "WeatherApp.dll"]