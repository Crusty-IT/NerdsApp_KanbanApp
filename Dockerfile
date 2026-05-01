FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["KanbanApp.Backend/KanbanApp.Backend.csproj", "KanbanApp.Backend/"]
RUN dotnet restore "KanbanApp.Backend/KanbanApp.Backend.csproj"

COPY KanbanApp.Backend/ KanbanApp.Backend/
WORKDIR /src/KanbanApp.Backend
RUN dotnet build "KanbanApp.Backend.csproj" -c Release -o /app/build

RUN dotnet publish "KanbanApp.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "KanbanApp.Backend.dll"]