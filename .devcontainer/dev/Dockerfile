FROM mcr.microsoft.com/dotnet/core/sdk:3.1-focal

ARG USE_MIRROR=1
RUN if [ "${USE_MIRROR}" = '1' ]; then \
    sed -i "s|http://archive\.ubuntu\.com/ubuntu/|mirror://mirrors.ubuntu.com/mirrors.txt|g" /etc/apt/sources.list; fi

RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg postgresql-client

RUN echo 'export PATH="$PATH:/root/.dotnet/tools"' >> /root/.bashrc
RUN dotnet tool install --global dotnet-ef --version 3.1.8

CMD [ "/bin/sh", "-c", "while sleep 1000; do :; done" ]
