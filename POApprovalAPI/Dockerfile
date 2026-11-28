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

# Chatbot schema search shells out to Python (numpy + fastembed).
# The model is downloaded here so a free-tier restart does not fetch it again.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip python-is-python3 \
    && (python3 -m pip install --no-cache-dir --break-system-packages numpy fastembed \
        || python3 -m pip install --no-cache-dir numpy fastembed) \
    && rm -rf /var/lib/apt/lists/*
ENV FASTEMBED_CACHE_PATH=/opt/fastembed
RUN mkdir -p /opt/fastembed \
    && python3 -c "from fastembed import TextEmbedding; TextEmbedding(model_name='sentence-transformers/all-MiniLM-L6-v2')" \
    && chmod -R a+rX /opt/fastembed

WORKDIR /App
COPY --from=build-env /App/out .

# Render ignores EXPOSE and scans 0.0.0.0:$PORT. The entrypoint binds that port.
EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

COPY docker-entrypoint.sh /App/docker-entrypoint.sh
# The script is authored on Windows; strip CR so the Linux shell can run it.
RUN sed -i 's/\r$//' /App/docker-entrypoint.sh && chmod +x /App/docker-entrypoint.sh

ENTRYPOINT ["/App/docker-entrypoint.sh"]
