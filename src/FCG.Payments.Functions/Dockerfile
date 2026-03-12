FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FCG.Payments.Functions/FCG.Payments.Functions.csproj", "src/FCG.Payments.Functions/"]
COPY ["src/FCG.Payments.Application/FCG.Payments.Application.csproj", "src/FCG.Payments.Application/"]
COPY ["src/FCG.Payments.Domain/FCG.Payments.Domain.csproj", "src/FCG.Payments.Domain/"]
COPY ["src/FCG.Payments.Infrastructure/FCG.Payments.Infrastructure.csproj", "src/FCG.Payments.Infrastructure/"]
RUN dotnet restore "src/FCG.Payments.Functions/FCG.Payments.Functions.csproj"
COPY . .
WORKDIR "/src/src/FCG.Payments.Functions"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
