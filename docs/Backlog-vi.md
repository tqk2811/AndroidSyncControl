# Backlog

### [Vừa] Kiểm MCP server trên thiết bị thật

- Vấn đề: khi viết chưa có điện thoại cắm vào máy, test chỉ dùng thiết bị giả. Chưa chạy thật các ca: `device_connect` thành công, `screenshot` khi bật GPU, `tap`/`swipe`, nhập tiếng Việt, connect thất bại, ngắt máy giữa lúc swipe.
- Vị trí: `src/AndroidSyncControl/Mcp/Tools/ScreenTools.cs:32`, `src/AndroidSyncControl/Mcp/Helpers/ScreenshotEncoder.cs:19`, `src/AndroidSyncControl/UI/McpDeviceHost.cs:47`.
- Vì sao nên làm: `ScreenshotEncoder.LooksUnwritten` giả định frame thật có alpha khác 0, và đường chụp qua D3D11 chưa từng chạy từ MCP.
- Ngày ghi: 2026-11-09.

### [Vừa] Output của `adb_execute` không giới hạn trong RAM

- Vấn đề: `ProcessCommand` gom toàn bộ stdout vào bộ nhớ rồi `AdbTools` mới cắt còn 100.000 ký tự. Lệnh như `logcat` chạy tới timeout 600 giây có thể ăn nhiều RAM.
- Vị trí: `src/AndroidSyncControl/Mcp/AdbCommandRunner.cs:26`, `src/AndroidSyncControl/Mcp/Tools/AdbTools.cs:21`.
- Vì sao nên làm: một lệnh của agent có thể làm app phình bộ nhớ. Cách sửa là tự chạy process và ngừng đọc khi đủ giới hạn.
- Ngày ghi: 2026-11-09.

### [Thấp] Thiết bị do MCP mở cũng nhận thao tác Sync của người dùng

- Vấn đề: `device_connect` thêm máy vào danh sách chung, nên khi bật Sync ở khung chính, thao tác của người dùng cũng đi tới máy agent đang điều khiển. Agent cũng đóng được máy người dùng đang xem.
- Vị trí: `src/AndroidSyncControl/UI/McpDeviceHost.cs:47`, `src/AndroidSyncControl/UI/MainWindow.xaml.cs:174`.
- Vì sao nên làm: có thể cần đánh dấu máy do MCP mở để loại khỏi Sync. Hành vi hiện tại là hệ quả của quyết định dùng chung phiên với UI.
- Ngày ghi: 2026-11-09.
