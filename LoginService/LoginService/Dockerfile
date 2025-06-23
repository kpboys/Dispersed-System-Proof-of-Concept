# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ARG POD_NAME=Pod_Hest
ARG POD_IP=123.321.123.321
ARG VERSION=1.2.3
ARG JWT_SECRET_KEY=Hest
ARG ALIAS_SESSION_SERVICE=Hest
ARG GIT_COMMIT
ENV GIT_COMMIT=${GIT_COMMIT}


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LoginService/LoginService.csproj", "LoginService/"]
RUN dotnet restore "./LoginService/LoginService.csproj"
COPY . .
WORKDIR "/src/LoginService"
RUN dotnet build "./LoginService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LoginService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoginService.dll"]