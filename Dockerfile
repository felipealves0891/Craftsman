FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/App/Craftsman.csproj", "src/App/"]
COPY ["src/Domains/Craftsman.Domains.csproj", "src/Domains/"]
COPY ["src/Infra/Craftsman.Infra.csproj", "src/Infra/"]
RUN dotnet restore "src/App/Craftsman.csproj"
COPY . .
WORKDIR "/src/src/App"
RUN dotnet publish "Craftsman.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Craftsman.dll"]
