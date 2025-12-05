# Sử dụng base image .NET 8.0 SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy toàn bộ source code vào container
COPY . ./

# Build project ChatServer
RUN dotnet publish ChatServer/ChatServer.csproj -c Release -o out

# ========================================================================
# Sử dụng base image ASP.NET Core Runtime (thay vì chỉ .NET Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy kết quả build
COPY --from=build-env /app/out .

# Mở port 5000 (HTTP)
EXPOSE 5000

# Entry point
ENTRYPOINT ["dotnet", "ChatServer.dll"]
