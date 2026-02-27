FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProjectAssembly_Final.csproj", "./"]
RUN dotnet restore "ProjectAssembly_Final.csproj"
COPY . . 
WORKDIR "/src"
RUN dotnet publish "ProjectAssembly_Final.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProjectAssembly_Final.dll"]