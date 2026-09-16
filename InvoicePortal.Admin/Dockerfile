FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY InvoicePortal.Admin/InvoicePortal.Admin.csproj InvoicePortal.Admin/
RUN dotnet restore InvoicePortal.Admin/InvoicePortal.Admin.csproj
COPY InvoicePortal.Admin/ InvoicePortal.Admin/
RUN dotnet publish InvoicePortal.Admin/InvoicePortal.Admin.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "InvoicePortal.Admin.dll"]
