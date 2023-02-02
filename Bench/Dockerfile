#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
#RUN dotnet restore "Bench/Bench.csproj"

WORKDIR "/src/Bench"
RUN dotnet build "Bench.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bench.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src /app/src
RUN chmod 777 . -R
#USER armonikuser

ENV ASPNETCORE_URLS http://+:1080, http://+:1081
EXPOSE 1080
EXPOSE 1081

RUN ls -ail
WORKDIR /app/src/Bench
ENTRYPOINT ["dotnet", "run", "--project", "Bench.csproj", "-c", "release"]
