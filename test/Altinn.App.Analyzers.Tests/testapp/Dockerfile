FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:26a8e9c8c9854b0543a3ddaacb56ba96caaf47c4482844e5f7ad1f70338b97e3 AS build
WORKDIR /App

COPY /App/App.csproj .
RUN dotnet restore App.csproj

COPY /App .

RUN dotnet publish App.csproj --configuration Release --output /app_output

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:a6695199ce879ae4bcfc92f3dfd55b7db5e46e3bdc128a690c6594444f0e300a AS final
EXPOSE 5005
WORKDIR /App
COPY --from=build /app_output .
ENV ASPNETCORE_URLS=

# setup the user and group
# busybox doesn't include longopts, so the options are roughly
# -g --gid
# -u --uid
# -G --group
# -D --disable-password
# -s --shell
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet

USER dotnet
RUN mkdir /tmp/logtelemetry

ENTRYPOINT ["dotnet", "Altinn.Application.dll"]
