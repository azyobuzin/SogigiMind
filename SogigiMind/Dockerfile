FROM mcr.microsoft.com/dotnet/core/sdk:3.1-focal AS build
COPY . /source
WORKDIR /source/SogigiMind
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /app -r linux-x64 --self-contained false

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-focal
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["./SogigiMind"]
