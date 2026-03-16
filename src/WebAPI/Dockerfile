FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["global.json", "."]
COPY ["src/WebAPI/CleanArchitecture.WebAPI.csproj", "src/WebAPI/"]
COPY ["src/Application/CleanArchitecture.Application.csproj", "src/Application/"]
COPY ["src/Domain/CleanArchitecture.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/CleanArchitecture.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Modules/AI/CleanArchitecture.Modules.AI.csproj", "src/Modules/AI/"]

RUN dotnet restore "src/WebAPI/CleanArchitecture.WebAPI.csproj"

COPY . .

WORKDIR "/src/src/WebAPI"
RUN dotnet build "CleanArchitecture.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "CleanArchitecture.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleanArchitecture.WebAPI.dll"]
