FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["BakeryOrderSystem.csproj", "./"]
RUN dotnet restore "BakeryOrderSystem.csproj"

COPY . .
RUN dotnet publish "BakeryOrderSystem.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

ENTRYPOINT ["dotnet", "BakeryOrderSystem.dll"]