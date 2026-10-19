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
