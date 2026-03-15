FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/04-Presentation/CleanArchitecture.WebAPI/CleanArchitecture.WebAPI.csproj", "src/04-Presentation/CleanArchitecture.WebAPI/"]
COPY ["src/03-Infrastructure/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj", "src/03-Infrastructure/CleanArchitecture.Infrastructure/"]
COPY ["src/02-Application/CleanArchitecture.Application/CleanArchitecture.Application.csproj", "src/02-Application/CleanArchitecture.Application/"]
COPY ["src/01-Domain/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj", "src/01-Domain/CleanArchitecture.Domain/"]

RUN dotnet restore "src/04-Presentation/CleanArchitecture.WebAPI/CleanArchitecture.WebAPI.csproj"

COPY . .

WORKDIR "/src/src/04-Presentation/CleanArchitecture.WebAPI"
RUN dotnet build "CleanArchitecture.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "CleanArchitecture.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleanArchitecture.WebAPI.dll"]
