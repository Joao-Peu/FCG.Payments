FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FCG.Payments.Api/FCG.Payments.Api.csproj", "src/FCG.Payments.Api/"]
COPY ["src/FCG.Payments.Application/FCG.Payments.Application.csproj", "src/FCG.Payments.Application/"]
COPY ["src/FCG.Payments.Domain/FCG.Payments.Domain.csproj", "src/FCG.Payments.Domain/"]
COPY ["src/FCG.Payments.Infrastructure/FCG.Payments.Infrastructure.csproj", "src/FCG.Payments.Infrastructure/"]
RUN dotnet restore "src/FCG.Payments.Api/FCG.Payments.Api.csproj"
COPY . .
WORKDIR "/src/src/FCG.Payments.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FCG.Payments.Api.dll"]
