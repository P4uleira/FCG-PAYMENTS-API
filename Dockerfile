FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["src/FCG.Payments.Api/FCG.Payments.Api.csproj", "src/FCG.Payments.Api/"]
COPY ["src/FCG.Payments.Application/FCG.Payments.Application.csproj", "src/FCG.Payments.Application/"]
COPY ["src/FCG.Payments.Domain/FCG.Payments.Domain.csproj", "src/FCG.Payments.Domain/"]
COPY ["src/FCG.Payments.Infrastructure/FCG.Payments.Infrastructure.csproj", "src/FCG.Payments.Infrastructure/"]
COPY ["src/FCG.Payments.Contracts/FCG.Payments.Contracts.csproj", "src/FCG.Payments.Contracts/"]

RUN dotnet restore \
    "src/FCG.Payments.Api/FCG.Payments.Api.csproj"

FROM restore AS build

COPY . .

RUN dotnet publish \
    "src/FCG.Payments.Api/FCG.Payments.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8082
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8082

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FCG.Payments.Api.dll"]