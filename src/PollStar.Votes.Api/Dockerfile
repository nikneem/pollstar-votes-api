#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PollStar.Votes.Api/PollStar.Votes.Api.csproj", "PollStar.Votes.Api/"]
RUN dotnet restore "PollStar.Votes.Api/PollStar.Votes.Api.csproj"
COPY . .
WORKDIR "/src/PollStar.Votes.Api"
RUN dotnet build "PollStar.Votes.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PollStar.Votes.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PollStar.Votes.Api.dll"]