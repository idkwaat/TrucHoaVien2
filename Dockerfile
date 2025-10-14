# ================================
# Stage 1: Build the .NET project
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ProjectApi/ProjectApi.csproj ProjectApi/
WORKDIR /src/ProjectApi
RUN dotnet restore

# Copy all source files and build
COPY . .
RUN dotnet publish -c Release -o /app/out

# ================================
# Stage 2: Run the application
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ProjectApi.dll"]
