# Thuật ngữ

## MCP

Model Context Protocol: giao thức để agent AI gọi các tool do một server cung cấp.

## Streamable HTTP

Transport của MCP chạy trên HTTP, agent gửi request tới một endpoint duy nhất của server.

## SCID

Số định danh phiên của scrcpy. Nó quyết định tên socket trên thiết bị, nên hai phiên cùng thiết bị phải có SCID khác nhau.

## DNS rebinding

Kiểu tấn công khiến một trang web độc hại gọi được dịch vụ đang chạy trên `127.0.0.1` của máy người dùng. Chặn bằng cách kiểm header `Host`, `Origin` và yêu cầu token.

## Bearer token

Chuỗi bí mật client gửi trong header `Authorization: Bearer <token>` để server xác thực.

## Framework-dependent và self-contained

Framework-dependent: bản publish cần máy người dùng cài sẵn .NET Runtime tương ứng. Self-contained: bản publish mang theo runtime, không cần cài nhưng dung lượng lớn hơn.
