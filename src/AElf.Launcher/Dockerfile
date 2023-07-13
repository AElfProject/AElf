#FROM mcr.microsoft.com/dotnet/core/sdk:3.1
FROM mcr.microsoft.com/dotnet/sdk:6.0

# 安装 Node.js
RUN apt-get update && \
    apt-get install -y curl && \
    curl -sL https://deb.nodesource.com/setup_14.x | bash - && \
    apt-get install -y nodejs

# 刷新软件包缓存
RUN apt-get update

# 安装 aelf-command 包
RUN npm install aelf-command -g

WORKDIR /app

ENV LD_LIBRARY_PATH /app

COPY . .