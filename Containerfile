# syntax=docker/dockerfile:1.7
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /workspace

# Copy full standalone repository context
COPY . .

WORKDIR /workspace

# Deterministic CI-style restore/build
RUN dotnet restore Alloyed.DevOps.Multitool.slnx
RUN dotnet build Alloyed.DevOps.Multitool.slnx -c Release --no-restore

# Default command keeps container useful for local validation reruns
CMD ["dotnet", "build", "Alloyed.DevOps.Multitool.slnx", "-c", "Release", "--no-restore"]
