FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY WalletSample/coinsreceiver/CoinsReceiver.csproj WalletSample/coinsreceiver/
COPY Wallet/Wallet.csproj Wallet/
RUN dotnet restore
COPY . .
WORKDIR /src/WalletSample/coinsreceiver
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CoinsReceiver.dll"]
