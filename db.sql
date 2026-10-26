IF DB_ID(N'DeviceManagementDb') IS NULL
BEGIN
    CREATE DATABASE DeviceManagementDb;
END;
GO

USE DeviceManagementDb;
GO
/* Tạo bảng chính cho database mới. */
IF OBJECT_ID(N'dbo.Requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Requests
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Requests PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Title nvarchar(500) NOT NULL,
        Creator nvarchar(200) NOT NULL,
        DepartmentProject nvarchar(200) NOT NULL,
        Classification nvarchar(100) NULL,
        InsuranceType nvarchar(200) NULL,
        InsuranceExpiry datetime NULL,
        CreatedDate datetime NOT NULL,
        CreatedBy nvarchar(max) NULL,
        CreatedAt datetime NULL,
        UpdatedBy nvarchar(max) NULL,
        UpdatedAt datetime NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_Requests_IsDeleted DEFAULT (0),
        DeletedAt datetime NULL,
        DeletedBy nvarchar(max) NULL,
        RowVersion rowversion NOT NULL
    );
END;

/* Ràng buộc và index phục vụ tra cứu. */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Requests') AND name = N'IX_Requests_Code' AND is_unique = 0)
    DROP INDEX IX_Requests_Code ON dbo.Requests;
IF EXISTS (SELECT Code FROM dbo.Requests GROUP BY Code HAVING COUNT(*) > 1)
BEGIN
    ;THROW 50001, 'Duplicate request codes exist; resolve them before adding the unique constraint.', 1;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Requests') AND name = N'IX_Requests_Code' AND is_unique = 1)
    CREATE UNIQUE INDEX IX_Requests_Code ON dbo.Requests(Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Requests') AND name = N'IX_Requests_IsDeleted_CreatedDate')
    CREATE INDEX IX_Requests_IsDeleted_CreatedDate ON dbo.Requests(IsDeleted, CreatedDate DESC);

/* Bảng vật tư và tệp đính kèm. */
IF OBJECT_ID(N'dbo.RequestAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestAssets
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RequestAssets PRIMARY KEY,
        RequestId int NOT NULL,
        Equipment nvarchar(250) NOT NULL,
        DocumentName nvarchar(260) NULL,
        Supplier nvarchar(200) NULL,
        PurchaseDate datetime NOT NULL,
        ExpiryDate datetime NULL,
        Value decimal(18,2) NOT NULL,
        Premium decimal(18,2) NOT NULL,
        Quantity int NOT NULL,
        InsuranceType nvarchar(200) NULL,
        ContractNumber nvarchar(100) NOT NULL,
        CONSTRAINT FK_RequestAssets_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests(Id) ON DELETE CASCADE
    );
END;
IF COL_LENGTH(N'dbo.RequestAssets', N'Supplier') IS NULL
    ALTER TABLE dbo.RequestAssets ADD Supplier nvarchar(200) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RequestAssets') AND name = N'DocumentName' AND is_nullable = 0)
    ALTER TABLE dbo.RequestAssets ALTER COLUMN DocumentName nvarchar(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RequestAssets') AND name = N'IX_RequestAssets_RequestId')
    CREATE INDEX IX_RequestAssets_RequestId ON dbo.RequestAssets(RequestId);

/* Danh mục thiết bị dùng chung, tách biệt với các dòng vật tư trên từng phiếu. */
IF OBJECT_ID(N'dbo.Devices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devices
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Devices PRIMARY KEY,
        DeviceName nvarchar(250) NOT NULL,
        Year int NULL,
        Classification nvarchar(100) NULL,
        InsuranceType nvarchar(200) NULL,
        Department nvarchar(200) NULL,
        PurchaseDate datetime NULL,
        ExpiryDate datetime NULL,
        Quantity int NULL,
        Value decimal(18,2) NOT NULL CONSTRAINT DF_Devices_Value DEFAULT (0),
        InsuranceFee decimal(18,2) NOT NULL CONSTRAINT DF_Devices_InsuranceFee DEFAULT (0),
        Status nvarchar(50) NOT NULL CONSTRAINT DF_Devices_Status DEFAULT (N'Đang sử dụng'),
        Supplier nvarchar(200) NULL
    );
END;

/* Add separate filter fields when upgrading an existing Devices table. */
IF COL_LENGTH(N'dbo.Devices', N'Classification') IS NULL
    ALTER TABLE dbo.Devices ADD Classification nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.Devices', N'InsuranceType') IS NULL
    ALTER TABLE dbo.Devices ADD InsuranceType nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.Devices', N'PurchaseDate') IS NULL
    ALTER TABLE dbo.Devices ADD PurchaseDate datetime NULL;
IF COL_LENGTH(N'dbo.Devices', N'ExpiryDate') IS NULL
    ALTER TABLE dbo.Devices ADD ExpiryDate datetime NULL;
IF COL_LENGTH(N'dbo.Devices', N'Quantity') IS NULL
    ALTER TABLE dbo.Devices ADD Quantity int NULL;
/* Contract numbers belong to an insurance request, not the shared device catalog. */
IF COL_LENGTH(N'dbo.Devices', N'ContractNumber') IS NOT NULL
    ALTER TABLE dbo.Devices DROP COLUMN ContractNumber;

/* Migrate the previous Category field into InsuranceType when it exists. */
IF COL_LENGTH(N'dbo.Devices', N'Category') IS NOT NULL
    EXEC(N'UPDATE dbo.Devices SET InsuranceType = COALESCE(InsuranceType, Category) WHERE Category IS NOT NULL');

/* Enrich devices that were copied from existing request lines. */
;WITH ExistingRequestDevices AS
(
    SELECT a.Equipment, r.Classification, a.InsuranceType, a.PurchaseDate, a.ExpiryDate,
           a.Quantity,
           ROW_NUMBER() OVER (PARTITION BY a.Equipment ORDER BY a.Id) AS RowNum
    FROM dbo.RequestAssets AS a
    INNER JOIN dbo.Requests AS r ON r.Id = a.RequestId
    WHERE NULLIF(LTRIM(RTRIM(a.Equipment)), N'') IS NOT NULL
)
UPDATE d
SET d.Classification = COALESCE(d.Classification, source.Classification),
    d.InsuranceType = COALESCE(d.InsuranceType, source.InsuranceType),
    d.PurchaseDate = COALESCE(d.PurchaseDate, source.PurchaseDate),
    d.ExpiryDate = COALESCE(d.ExpiryDate, source.ExpiryDate),
    d.Quantity = COALESCE(d.Quantity, source.Quantity)
FROM dbo.Devices AS d
INNER JOIN ExistingRequestDevices AS source ON source.Equipment = d.DeviceName AND source.RowNum = 1;

/* Khởi tạo danh mục từ thiết bị đã có trong phiếu, không sửa/xóa dữ liệu phiếu gốc. */
INSERT INTO dbo.Devices (DeviceName, Year, Classification, InsuranceType, Department, PurchaseDate, ExpiryDate, Quantity, Value, InsuranceFee, Status, Supplier)
SELECT source.Equipment,
       YEAR(source.PurchaseDate),
       source.Classification,
       source.InsuranceType,
       source.DepartmentProject,
       source.PurchaseDate,
       source.ExpiryDate,
       source.Quantity,
       source.Value,
       source.Premium,
       CASE WHEN source.ExpiryDate IS NOT NULL AND CONVERT(date, source.ExpiryDate) < CONVERT(date, GETDATE())
            THEN N'Hết hạn' ELSE N'Đang sử dụng' END,
       source.Supplier
FROM
(
    SELECT a.Equipment, a.PurchaseDate, r.Classification, a.InsuranceType, r.DepartmentProject,
           a.ExpiryDate, a.Quantity,
           a.Value, a.Premium, a.Supplier,
           ROW_NUMBER() OVER (PARTITION BY a.Equipment ORDER BY a.Id) AS RowNum
    FROM dbo.RequestAssets AS a
    INNER JOIN dbo.Requests AS r ON r.Id = a.RequestId
    WHERE NULLIF(LTRIM(RTRIM(a.Equipment)), N'') IS NOT NULL
) AS source
WHERE source.RowNum = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Devices AS d WHERE d.DeviceName = source.Equipment);

/* Seed danh mục thiết bị mẫu; chạy lại script điền các trường seed còn trống. */
;MERGE dbo.Devices AS target
USING
(
    VALUES
        (N'Tủ lạnh', 2026, N'Thiết bị', N'Bảo hiểm trách nhiệm bên thứ ba', N'Dự án 01', CONVERT(datetime, '20261007', 112), CONVERT(datetime, '20271007', 112), 1, CAST(20000000 AS decimal(18,2)), CAST(100000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Thiết bị Á Châu'),
        (N'Máy phát điện', 2024, N'Thiết bị', N'Bảo hiểm thiết bị', N'Dự án 02', CONVERT(datetime, '20260115', 112), CONVERT(datetime, '20270115', 112), 1, CAST(185000000 AS decimal(18,2)), CAST(925000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Cơ điện Việt'),
        (N'Xe nâng hàng', 2023, N'Phương tiện', N'Bảo hiểm phương tiện', N'Kho vận', CONVERT(datetime, '20260310', 112), CONVERT(datetime, '20270310', 112), 1, CAST(420000000 AS decimal(18,2)), CAST(2100000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Xe nâng Miền Nam'),
        (N'Máy móc thi công', 2025, N'Tài sản', N'Bảo hiểm mọi rủi ro', N'Dự án 01', CONVERT(datetime, '20260201', 112), CONVERT(datetime, '20270201', 112), 1, CAST(760000000 AS decimal(18,2)), CAST(3800000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Xây dựng và Thiết bị'),
        (N'Máy in văn phòng', 2022, N'Thiết bị', N'Bảo hiểm tài sản', N'Phòng Hành chính', CONVERT(datetime, '20260105', 112), CONVERT(datetime, '20270105', 112), 1, CAST(18000000 AS decimal(18,2)), CAST(90000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Văn phòng phẩm Việt')
) AS seed(DeviceName, Year, Classification, InsuranceType, Department, PurchaseDate, ExpiryDate, Quantity, Value, InsuranceFee, Status, Supplier)
ON target.DeviceName = seed.DeviceName
WHEN MATCHED THEN
    UPDATE SET Year = COALESCE(target.Year, seed.Year),
               Classification = COALESCE(target.Classification, seed.Classification),
               InsuranceType = COALESCE(target.InsuranceType, seed.InsuranceType),
               Department = COALESCE(target.Department, seed.Department),
               PurchaseDate = COALESCE(target.PurchaseDate, seed.PurchaseDate),
               ExpiryDate = COALESCE(target.ExpiryDate, seed.ExpiryDate),
               Quantity = COALESCE(target.Quantity, seed.Quantity),
               Value = CASE WHEN target.Value = 0 THEN seed.Value ELSE target.Value END,
               InsuranceFee = CASE WHEN target.InsuranceFee = 0 THEN seed.InsuranceFee ELSE target.InsuranceFee END,
               Status = COALESCE(target.Status, seed.Status),
               Supplier = COALESCE(target.Supplier, seed.Supplier)
WHEN NOT MATCHED BY TARGET THEN
    INSERT (DeviceName, Year, Classification, InsuranceType, Department, PurchaseDate, ExpiryDate, Quantity, Value, InsuranceFee, Status, Supplier)
    VALUES (seed.DeviceName, seed.Year, seed.Classification, seed.InsuranceType, seed.Department,
            seed.PurchaseDate, seed.ExpiryDate, seed.Quantity,
            seed.Value, seed.InsuranceFee, seed.Status, seed.Supplier);

IF OBJECT_ID(N'dbo.RequestAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestAttachments
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RequestAttachments PRIMARY KEY,
        RequestId int NOT NULL,
        RequestAssetId int NULL,
        FileName nvarchar(260) NOT NULL,
        StoredFileName nvarchar(260) NOT NULL,
        ContentType nvarchar(150) NULL,
        UploadedAt datetime NOT NULL,
        CONSTRAINT FK_RequestAttachments_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RequestAttachments') AND name = N'IX_RequestAttachments_RequestId')
    CREATE INDEX IX_RequestAttachments_RequestId ON dbo.RequestAttachments(RequestId);

/* Seed mẫu */
;MERGE dbo.Requests AS target
USING
(
    VALUES
        (N'TBBH2026-005', N'Đăng ký bảo hiểm vật tư công trình A', N'Nguyễn Minh Anh', N'Dự án 01', N'Tài sản', N'Bảo hiểm trách nhiệm bên thứ ba', CONVERT(datetime, '20271008', 112), CONVERT(datetime, '20261008', 112)),
        (N'TBBH2026-004', N'Đăng ký bảo hiểm thiết bị văn phòng', N'Trần Quốc Bảo', N'Phòng Hành chính', N'Thiết bị', N'Bảo hiểm tài sản', CONVERT(datetime, '20271007', 112), CONVERT(datetime, '20261007', 112)),
        (N'TBBH2026-003', N'Kiểm tra hồ sơ bảo hiểm máy phát điện', N'Lê Thu Hà', N'Dự án 02', N'Thiết bị', N'Bảo hiểm thiết bị', NULL, CONVERT(datetime, '20260928', 112)),
        (N'TBBH2026-002', N'Kiểm tra thời hạn bảo hiểm xe nâng', N'Phạm Đức Long', N'Kho vận', NULL, N'Bảo hiểm phương tiện', CONVERT(datetime, '20261231', 112), CONVERT(datetime, '20260928', 112)),
        (N'TBBH2026-001', N'Đăng ký bảo hiểm máy móc thi công', N'Nguyễn Minh Anh', N'Dự án 01', N'Thiết bị', NULL, NULL, CONVERT(datetime, '20260923', 112))
) AS seed(Code, Title, Creator, DepartmentProject, Classification, InsuranceType, InsuranceExpiry, CreatedDate)
ON target.Code = seed.Code
WHEN MATCHED AND target.CreatedBy = N'system' AND target.UpdatedAt IS NULL THEN
    UPDATE SET Title = seed.Title, Creator = seed.Creator, DepartmentProject = seed.DepartmentProject,
               Classification = seed.Classification, InsuranceType = seed.InsuranceType,
               InsuranceExpiry = seed.InsuranceExpiry, CreatedDate = seed.CreatedDate
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Title, Creator, DepartmentProject, Classification, InsuranceType, InsuranceExpiry, CreatedDate, CreatedBy, CreatedAt)
    VALUES (seed.Code, seed.Title, seed.Creator, seed.DepartmentProject, seed.Classification, seed.InsuranceType,
            seed.InsuranceExpiry, seed.CreatedDate, N'system', GETDATE());
