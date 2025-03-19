#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BCL.API/BCL.API.csproj", "BCL.API/"]
RUN dotnet restore "BCL.API/BCL.API.csproj"
COPY . .
WORKDIR "/src/BCL.API"
RUN dotnet build "BCL.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BCL.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BCL.API.dll"]