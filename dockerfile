FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GigHub.Api/*.csproj GigHub.Api/
RUN dotnet restore GigHub.Api
COPY . .
RUN dotnet publish GigHub.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GigHub.Api.dll"]
