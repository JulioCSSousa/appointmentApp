# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AppointmentAppMvc.csproj", "."]
RUN dotnet restore "AppointmentAppMvc.csproj"
COPY . .
RUN dotnet build "AppointmentAppMvc.csproj" -c Release -o /app/build

# Etapa de publicação
FROM build AS publish
RUN dotnet publish "AppointmentAppMvc.csproj" -c Debug -o /app/publish
EXPOSE 8081
# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppointmentAppMvc.dll"]
