# NSIS ile Windows kurulum dosyasını Linux üzerinde derlemek için küçük imaj.
FROM ubuntu:22.04
RUN apt-get update && apt-get install -y --no-install-recommends nsis nsis-pluginapi ca-certificates \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /work
ENTRYPOINT ["makensis"]
