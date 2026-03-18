# 1. MƯỢN MÁY CỦA MICROSOFT ĐỂ BUILD CODE
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file csproj vào và Restore thư viện
COPY ["WebSuDungDIen.csproj", "./"]
RUN dotnet restore "./WebSuDungDIen.csproj"

# Copy toàn bộ code vào và đóng gói (Publish)
COPY . .
RUN dotnet publish "WebSuDungDIen.csproj" -c Release -o /app/publish

# 2. TẠO SERVER NHỎ ĐỂ CHẠY WEB
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Mở cổng 8080 (Chuẩn mới của .NET 8)
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Chép bản đóng gói từ bước 1 sang đây và chạy
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebSuDungDIen.dll"]