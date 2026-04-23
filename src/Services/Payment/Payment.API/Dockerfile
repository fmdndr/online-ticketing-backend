FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Payment/Payment.API/Payment.API.csproj", "src/Services/Payment/Payment.API/"]
COPY ["src/Shared/Shared.Common/Shared.Common.csproj", "src/Shared/Shared.Common/"]
RUN dotnet restore "src/Services/Payment/Payment.API/Payment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Payment/Payment.API"
RUN dotnet build "Payment.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__PaymentDb="Host=host.docker.internal;Port=5432;Database=ticketing_payments;Username=postgres;Password=postgres"
ENV RabbitMQ__Host=host.docker.internal
ENV RabbitMQ__Username=guest
ENV RabbitMQ__Password=guest
ENV Serilog__WriteTo__1__Args__serverUrl="http://host.docker.internal:5341"

ENTRYPOINT ["dotnet", "Payment.API.dll"]
