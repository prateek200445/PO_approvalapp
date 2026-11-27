# Use the official .NET 10.0 SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /App

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the remaining source files and build the release
COPY . ./
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Google Chrome and a virtual display for the GST portal login (it runs a visible browser window)
ADD https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb /tmp/chrome.deb
RUN apt-get update \
    && apt-get install -y --no-install-recommends /tmp/chrome.deb xvfb xauth fonts-liberation \
    && rm -rf /var/lib/apt/lists/* /tmp/chrome.deb

WORKDIR /App
COPY --from=build-env /App/out .

# Render ignores EXPOSE and scans 0.0.0.0:$PORT. The entrypoint binds that port.
EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

COPY docker-entrypoint.sh /App/docker-entrypoint.sh
# The script is authored on Windows; strip CR so the Linux shell can run it.
RUN sed -i 's/\r$//' /App/docker-entrypoint.sh && chmod +x /App/docker-entrypoint.sh

ENTRYPOINT ["/App/docker-entrypoint.sh"]
