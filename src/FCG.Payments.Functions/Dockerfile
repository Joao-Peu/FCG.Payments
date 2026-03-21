FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FCG.Payments.Functions/FCG.Payments.Functions.csproj", "FCG.Payments.Functions/"]
COPY ["src/FCG.Payments.Application/FCG.Payments.Application.csproj", "FCG.Payments.Application/"]
COPY ["src/FCG.Payments.Domain/FCG.Payments.Domain.csproj", "FCG.Payments.Domain/"]
COPY ["src/FCG.Payments.Infrastructure/FCG.Payments.Infrastructure.csproj", "FCG.Payments.Infrastructure/"]
RUN dotnet restore "FCG.Payments.Functions/FCG.Payments.Functions.csproj"
COPY src/ .
RUN dotnet publish "FCG.Payments.Functions/FCG.Payments.Functions.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
