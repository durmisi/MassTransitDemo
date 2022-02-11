#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MassTransitDemo.Api/MassTransitDemo.Api.csproj", "MassTransitDemo.Api/"]
RUN dotnet restore "MassTransitDemo.Api/MassTransitDemo.Api.csproj"
COPY . .
WORKDIR "/src/MassTransitDemo.Api"
RUN dotnet build "MassTransitDemo.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MassTransitDemo.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MassTransitDemo.Api.dll"]
