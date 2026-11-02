# Device Management

Ứng dụng quản lý thiết bị, yêu cầu bảo hiểm, yêu cầu kiểm định và tai nạn/sự cố. Dự án dùng ASP.NET MVC, ASP.NET Web API, Entity Framework 6 và SQL Server.

## Cấu trúc dự án

```text
DeviceManagement/
├── App_Start/                  # Cấu hình MVC route, Web API, bundle và filter
├── Content/                    # CSS
├── Controllers/                # MVC controllers và Web API controllers
│   ├── DeviceController.cs     # Trang theo dõi thiết bị bảo hiểm
│   ├── DeviceApiController.cs  # API dữ liệu thiết bị bảo hiểm
│   ├── InspectionDeviceController.cs
│   ├── InspectionDeviceApiController.cs
│   ├── InspectionController.cs
│   ├── InspectionApiController.cs
│   ├── AccidentController.cs
│   ├── AccidentApiController.cs
│   ├── RequestController.cs   # Các trang phiếu bảo hiểm, giữ route /Request
│   ├── InsuranceApiController.cs
│   ├── InsuranceAttachmentApiController.cs
│   ├── CatalogApiController.cs
│   ├── ManageController.cs
│   ├── HomeController.cs
│   └── ReportController.cs
├── Data/
│   ├── ApplicationDbContext.cs # DbSet và ánh xạ Entity sang bảng SQL
│   └── sidebar-data.js         # Cấu hình menu
├── Models/                     # Entity, DTO/form và model danh mục
├── Services/
│   └── InsuranceService.cs     # Luồng lưu, đọc, xóa mềm và khôi phục phiếu bảo hiểm
├── Views/
│   ├── Device/                 # Theo dõi thiết bị bảo hiểm
│   ├── InspectionDevice/       # Theo dõi thiết bị kiểm định
│   ├── Inspection/             # Danh sách, form và xem phiếu kiểm định
│   ├── Accident/               # Danh sách, form thêm/sửa và xác nhận xóa tai nạn/sự cố
│   ├── Request/                # Form và trang xem phiếu bảo hiểm
│   ├── Home/                   # Danh sách phiếu bảo hiểm
│   ├── Manage/                 # Danh mục quản trị
│   ├── Report/                 # Thống kê, báo cáo
│   └── Shared/                 # Layout và trang dùng chung
├── Scripts/                    # JavaScript và thư viện phía trình duyệt
├── db.sql                      # Tạo/cập nhật schema và chuyển dữ liệu cũ
└── db.seed.sql                 # Dữ liệu mẫu bổ sung
```

## Tách biệt nghiệp vụ thiết bị

- `Materials` là danh mục vật tư gốc, cung cấp mã, tên và thông tin danh mục dùng chung.
- `Devices` lưu bản ghi theo dõi thiết bị của nghiệp vụ bảo hiểm, bao gồm các trường riêng như phí bảo hiểm.
- `InspectionDevices` lưu bản ghi theo dõi thiết bị của nghiệp vụ kiểm định, không có phí bảo hiểm. Hai bảng có CRUD và API riêng; thay đổi bản ghi ở module này không tự sửa bản ghi ở module kia.
- `InspectionRequests` và `InspectionAssets` lưu phiếu kiểm định và thông tin snapshot của thiết bị tại thời điểm tạo phiếu.
- `Insurances`, `InsuranceAssets` và `InsuranceAttachments` lưu phiếu bảo hiểm, thiết bị trong phiếu và tệp đính kèm.
- Xóa phiếu bảo hiểm và phiếu kiểm định là xóa mềm: cập nhật `IsDeleted`, `DeletedAt`, `DeletedBy`; dữ liệu chi tiết và tệp vẫn được giữ. API danh sách và xem/sửa phiếu chỉ trả về phiếu chưa xóa.

## API chính

| Chức năng | Endpoint |
|---|---|
| Danh sách thiết bị bảo hiểm | `GET /api/deviceapi` |
| CRUD thiết bị kiểm định | `/api/inspectiondeviceapi` |
| Phiếu kiểm định | `/api/inspectionapi` |
| Phiếu bảo hiểm | `/api/requestapi` |
| Tệp đính kèm phiếu bảo hiểm | `/api/requestapi/{id}/attachments` |
| Phiếu tai nạn/sự cố | `/api/accidentapi` |
| Danh mục quản trị | `/api/catalogapi/{category}` |

Endpoint phiếu bảo hiểm vẫn dùng tên `requestapi` để tương thích với các trang hiện tại; code và bảng lưu trữ phía server dùng tên `Insurance`.

## Cập nhật cơ sở dữ liệu

1. Mở `db.sql` trong SQL Server Management Studio hoặc SQL Server Object Explorer và chạy để tạo/cập nhật schema. Script có bước chuyển dữ liệu thiết bị cũ sang `InspectionDevices` khi bảng mới được tạo, đồng thời giữ ID để bảo toàn liên kết của các phiếu kiểm định hiện có.
2. Chạy `db.seed.sql` nếu cần thêm dữ liệu mẫu.
3. Kiểm tra chuỗi kết nối `DefaultConnection` trong `Web.config` trỏ tới đúng SQL Server và database.

Schema được quản lý bằng SQL; EF không tự tạo hay cập nhật bảng khi ứng dụng khởi động.

## Build

Mở `DeviceManagement.sln` bằng Visual Studio 2022 và build project `DeviceManagement`.
