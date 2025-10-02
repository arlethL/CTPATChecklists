FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./CTPATChecklists/CTPATChecklists.csproj ./CTPATChecklists/

RUN dotnet restore ./CTPATChecklists/CTPATChecklists.csproj

COPY . .
WORKDIR /src/CTPATChecklists
RUN dotnet publish CTPATChecklists.csproj -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CTPATChecklists.dll"]
