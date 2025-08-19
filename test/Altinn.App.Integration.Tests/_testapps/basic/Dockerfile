FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:4042a687b9c50bfbd4defd6c28c455e5883aaaa5d086cb2d6e8dd39027bde58b AS build

COPY NuGet.config .
COPY _packages _packages/
COPY _shared _shared/
WORKDIR /App
COPY /App/App.csproj .
RUN dotnet restore App.csproj

COPY /App .

RUN dotnet publish App.csproj --configuration Release --output /app_output

# Ensure that the config folder is copied to the output folder (policy.xml was missing)
RUN cp -R config /app_output/

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:6faef52a037435524fcda33d94a23cada50eed3775849efa863bf0d45327403c AS final
EXPOSE 5005
EXPOSE 5006
WORKDIR /App
COPY --from=build /app_output .
ENV ASPNETCORE_URLS=http://+:5005


# setup the user and group
# busybox doesn't include longopts, so the options are roughly
# -g --gid
# -u --uid
# -G --group
# -D --disable-password
# -s --shell
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet

# Change ownership of /App to dotnet user so scenario overrides can modify files
RUN chown -R dotnet:dotnet /App

USER dotnet
RUN mkdir /tmp/logtelemetry

HEALTHCHECK --interval=1s --timeout=1s --retries=20 \
    CMD wget -nv -t1 --spider 'http://localhost:5005/health' || exit 1

ENTRYPOINT ["dotnet", "Altinn.Application.dll"]
