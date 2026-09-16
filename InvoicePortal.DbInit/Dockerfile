FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY InvoicePortal.DbInit/InvoicePortal.DbInit.csproj InvoicePortal.DbInit/
RUN dotnet restore InvoicePortal.DbInit/InvoicePortal.DbInit.csproj
COPY InvoicePortal.DbInit/ InvoicePortal.DbInit/
RUN dotnet publish InvoicePortal.DbInit/InvoicePortal.DbInit.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "InvoicePortal.DbInit.dll"]
