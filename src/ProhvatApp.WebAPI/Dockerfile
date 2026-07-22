# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/ProhvatApp.WebAPI/ProhvatApp.WebAPI.csproj", "ProhvatApp.WebAPI/"]
COPY ["src/ProhvatApp.Application/ProhvatApp.Application.csproj", "ProhvatApp.Application/"]
COPY ["src/ProhvatApp.Domain/ProhvatApp.Domain.csproj", "ProhvatApp.Domain/"]
COPY ["src/ProhvatApp.Infrastructure/ProhvatApp.Infrastructure.csproj", "ProhvatApp.Infrastructure/"]

RUN dotnet restore "ProhvatApp.WebAPI/ProhvatApp.WebAPI.csproj"

# Copy everything else and build
COPY src/ .
WORKDIR "/src/ProhvatApp.WebAPI"
RUN dotnet build "ProhvatApp.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProhvatApp.WebAPI.csproj" -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy cities seed data
COPY --from=build /src/ProhvatApp.Infrastructure/Persistence/russian-cities.json ./russian-cities.json

# Expose port 8080 (ASP.NET 8+ default)
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProhvatApp.WebAPI.dll"]
