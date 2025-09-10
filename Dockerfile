# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
 
# Copier le .csproj
COPY soft20181_starter.csproj ./
 
# Restaurer les dépendances
RUN dotnet restore
 
# Copier le reste des fichiers (nettoyé grâce à .dockerignore)
COPY . ./
 
# Publier l'application
RUN dotnet publish -c Release -o /out --no-restore
 
# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
 
# Créer un utilisateur non-root pour exécuter l'application
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser
 
# Copier les fichiers compilés depuis l'étape build
COPY --from=build /out ./
 
# Exposer le port HTTP
EXPOSE 80
 
# Variables d'environnement pour ASP.NET
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
 
# Point d'entrée de l'application
ENTRYPOINT ["dotnet", "soft20181_starter.dll"]
