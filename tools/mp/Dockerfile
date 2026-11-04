# Who Broke It? - iki istemcili ag testi imaji: uretim sunucu imajiyla (server/Dockerfile.game) ayni kutuphaneler + tc/netem (iproute2).
FROM ubuntu:22.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates xvfb libgl1-mesa-dri libglx-mesa0 libglu1-mesa libgbm1 \
    libxcursor1 libxrandr2 libxinerama1 libxi6 libxss1 libgssapi-krb5-2 libatomic1 libasound2 iproute2 \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /srv/game
ENV DISPLAY=:99 XDG_RUNTIME_DIR=/tmp
