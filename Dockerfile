FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION=0.0.0
WORKDIR /src

COPY ["Kwytka/Kwytka.csproj", "Kwytka/"]
COPY ["Kwytka.RichTextEditor/Kwytka.RichTextEditor.csproj", "Kwytka.RichTextEditor/"]
RUN dotnet restore "Kwytka/Kwytka.csproj"

COPY . .
WORKDIR "/src/Kwytka"
RUN dotnet publish "Kwytka.csproj" -c Release -o /app/publish /p:UseAppHost=false \
    /p:Version=$APP_VERSION /p:InformationalVersion=$APP_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_VERSION=0.0.0
LABEL org.opencontainers.image.version=$APP_VERSION
ENV APP_VERSION=$APP_VERSION
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

USER root
RUN chown -R $APP_UID /app
USER $APP_UID

ENTRYPOINT ["dotnet", "Kwytka.dll"]
