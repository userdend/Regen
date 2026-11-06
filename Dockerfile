# Use the .NET 10 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /Regen

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Copy the entire project and build
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /Regen
COPY --from=build-env /Regen/out ./

# Expose port 5050 for the application
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Regen.dll"]

# Display a message in the terminal during build
RUN echo  -----------------------------------------------
RUN echo  Application is running at http://localhost:5050
RUN echo  -----------------------------------------------
