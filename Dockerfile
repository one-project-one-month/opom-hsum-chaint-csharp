FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HsumChaint.API/HsumChaint.API.csproj", "HsumChaint.API/"]
COPY ["HsumChaint.Application/HsumChaint.Application.csproj", "HsumChaint.Application/"]
COPY ["HsumChaint.Infrastructure/HsumChaint.Infrastructure.csproj", "HsumChaint.Infrastructure/"]
RUN dotnet restore "HsumChaint.API/HsumChaint.API.csproj"
COPY . .
WORKDIR "/src/HsumChaint.API"
RUN dotnet build "HsumChaint.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HsumChaint.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HsumChaint.API.dll"]
