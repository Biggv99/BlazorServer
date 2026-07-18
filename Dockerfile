FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

USER root 

# Install the necessary packages for GSSAPI/Kerberos support
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OptimusPrime.csproj", "./"]
RUN dotnet restore "OptimusPrime.csproj"

COPY . .
WORKDIR "/src/"
RUN dotnet build "OptimusPrime.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OptimusPrime.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/keys \
    && chown -R $APP_UID /app/keys

USER $APP_UID

ENTRYPOINT ["dotnet", "OptimusPrime.dll"]