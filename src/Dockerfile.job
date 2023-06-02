#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PollStar.Votes.ProcessJob/PollStar.Votes.ProcessJob.csproj", "PollStar.Votes.ProcessJob/"]
COPY ["PollStar.Votes.Abstractions/PollStar.Votes.Abstractions.csproj", "PollStar.Votes.Abstractions/"]
COPY ["PollStar.Votes.Functions/PollStar.Votes.Functions.csproj", "PollStar.Votes.Functions/"]
COPY ["PollStar.Votes/PollStar.Votes.csproj", "PollStar.Votes/"]
RUN dotnet restore "PollStar.Votes.ProcessJob/PollStar.Votes.ProcessJob.csproj"
COPY . .
WORKDIR "/src/PollStar.Votes.ProcessJob"
RUN dotnet build "PollStar.Votes.ProcessJob.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PollStar.Votes.ProcessJob.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PollStar.Votes.ProcessJob.dll"]