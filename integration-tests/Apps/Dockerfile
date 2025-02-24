FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG APP_FOLDER
# Copy the pre built app-lib-packages to the image
RUN echo "Copying app-lib-packages to thimagde"
COPY app-lib-packages /app-lib-packages/
# COPY app-lib-nuget /app-lib-nuget/

WORKDIR /App

COPY nuget/nuget.config .
COPY ${APP_FOLDER}/App/App.csproj .
RUN dotnet restore -v diag App.csproj
# RUN dotnet restore  --packages app-lib-nuget App.csproj

COPY ${APP_FOLDER}/App .

RUN dotnet publish App.csproj --configuration Release --output /app_output

# Ensure that the config folder is copied to the output folder (policy.xml was missing)
RUN cp -R config /app_output/


FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
# Add globalization timezone support
RUN apk add --no-cache icu-libs tzdata

WORKDIR /App
COPY --from=build /app_output .

# setup the user and group
# busybox doesn't include longopts, so the options are roughly
# -g --gid
# -u --uid
# -G --group
# -D --disable-password
# -s --shell
RUN chmod -R 755 /App && \
    addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet


USER dotnet
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=
EXPOSE 5005

RUN mkdir /tmp/logtelemetry
HEALTHCHECK --interval=5s --timeout=5s --retries=3 \
    CMD wget -nv -t1 --spider 'http://localhost:5005/health' || exit 1

ENTRYPOINT ["dotnet", "Altinn.Application.dll"]
