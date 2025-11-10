# Multi-stage build for ZILF compiler service

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY CurrentVersion.props ./
COPY src/Zilf.CompilerService/Zilf.CompilerService.csproj src/Zilf.CompilerService/
COPY src/Zilf/Zilf.csproj src/Zilf/
COPY src/Zilf.Common/Zilf.Common.csproj src/Zilf.Common/
COPY src/Zilf.Emit/Zilf.Emit.csproj src/Zilf.Emit/
COPY src/Zapf/Zapf.csproj src/Zapf/
COPY src/Zapf.Parsing/Zapf.Parsing.csproj src/Zapf.Parsing/
RUN dotnet restore src/Zilf.CompilerService/Zilf.CompilerService.csproj

# Copy rest of source
COPY . .
RUN dotnet publish src/Zilf.CompilerService/Zilf.CompilerService.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Optional: install cron and rsyslog for scheduled cleanup (skipped when SKIP_CRON=1)
ARG SKIP_CRON=0
RUN if [ "$SKIP_CRON" != "1" ]; then \
    apt-get update && apt-get install -y \
    cron \
    rsyslog \
    && rm -rf /var/lib/apt/lists/*; \
    fi

# Copy zillib (ZIL library) into runtime image for include paths
COPY zillib /app/zillib

# Copy published app
COPY --from=build /app/publish .

# Copy cleanup script and cron configuration (safe even when SKIP_CRON=1)
COPY src/Zilf.CompilerService/cleanup.sh /app/cleanup.sh
COPY src/Zilf.CompilerService/cleanup.cron /etc/cron.d/zilf-cleanup
RUN chmod +x /app/cleanup.sh || true && \
    chmod 0644 /etc/cron.d/zilf-cleanup || true && \
    (crontab /etc/cron.d/zilf-cleanup || true) && \
    (touch /var/log/cron.log || true)

# Configure URLs and default port
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Start syslog/cron when present, then the application
CMD sh -c 'if command -v rsyslogd >/dev/null 2>&1; then sed -i "/imklog/s/^/#/" /etc/rsyslog.conf && rsyslogd; fi; \
           if command -v cron >/dev/null 2>&1; then cron; fi; \
           dotnet Zilf.CompilerService.dll'
