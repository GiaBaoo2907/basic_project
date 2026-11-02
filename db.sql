IF DB_ID(N'DeviceManagementDb') IS NULL
BEGIN
    CREATE DATABASE DeviceManagementDb;
END;
GO

USE DeviceManagementDb;
GO
/* Upgrade legacy insurance table names without copying or dropping rows. */
IF OBJECT_ID(N'dbo.Requests', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Insurances', N'U') IS NULL EXEC sys.sp_rename N'dbo.Requests', N'Insurances';
IF OBJECT_ID(N'dbo.RequestAssets', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.InsuranceAssets', N'U') IS NULL EXEC sys.sp_rename N'dbo.RequestAssets', N'InsuranceAssets';
IF OBJECT_ID(N'dbo.RequestAttachments', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.InsuranceAttachments', N'U') IS NULL EXEC sys.sp_rename N'dbo.RequestAttachments', N'InsuranceAttachments';
IF OBJECT_ID(N'dbo.InsuranceAssets', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAssets', N'RequestId') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAssets', N'InsuranceId') IS NULL EXEC sys.sp_rename N'dbo.InsuranceAssets.RequestId', N'InsuranceId', N'COLUMN';
IF OBJECT_ID(N'dbo.InsuranceAttachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAttachments', N'RequestId') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAttachments', N'InsuranceId') IS NULL EXEC sys.sp_rename N'dbo.InsuranceAttachments.RequestId', N'InsuranceId', N'COLUMN';
IF OBJECT_ID(N'dbo.InsuranceAttachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAttachments', N'RequestAssetId') IS NOT NULL AND COL_LENGTH(N'dbo.InsuranceAttachments', N'InsuranceAssetId') IS NULL EXEC sys.sp_rename N'dbo.InsuranceAttachments.RequestAssetId', N'InsuranceAssetId', N'COLUMN';
DECLARE @MaterialCatalogCreated bit = 0;
DECLARE @InspectionDevicesCreated bit = 0;

/* 1. Create the core schema when tables do not exist. */
IF OBJECT_ID(N'dbo.Insurances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Insurances
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Insurances PRIMARY KEY,
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
        IsDeleted bit NOT NULL CONSTRAINT DF_Insurances_IsDeleted DEFAULT (0),
        DeletedAt datetime NULL,
        DeletedBy nvarchar(max) NULL,
        RowVersion rowversion NOT NULL
    );
END;

/* Upgrade existing request tables before indexing soft-delete columns. */
IF COL_LENGTH(N'dbo.Insurances', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Insurances
        ADD IsDeleted bit NOT NULL
            CONSTRAINT DF_Insurances_IsDeleted DEFAULT (0) WITH VALUES;
END
ELSE IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Insurances')
      AND name = N'IsDeleted'
      AND is_nullable = 1
)
BEGIN
    UPDATE dbo.Insurances SET IsDeleted = 0 WHERE IsDeleted IS NULL;
    ALTER TABLE dbo.Insurances ALTER COLUMN IsDeleted bit NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Insurances')
      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Insurances'), N'IsDeleted', 'ColumnId')
)
    ALTER TABLE dbo.Insurances ADD CONSTRAINT DF_Insurances_IsDeleted DEFAULT (0) FOR IsDeleted;

IF COL_LENGTH(N'dbo.Insurances', N'DeletedAt') IS NULL
    ALTER TABLE dbo.Insurances ADD DeletedAt datetime NULL;
IF COL_LENGTH(N'dbo.Insurances', N'DeletedBy') IS NULL
    ALTER TABLE dbo.Insurances ADD DeletedBy nvarchar(max) NULL;

/* Ràng buộc và index phục vụ tra cứu. */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Insurances') AND name = N'IX_Insurances_Code' AND is_unique = 0)
    DROP INDEX IX_Insurances_Code ON dbo.Insurances;
IF EXISTS (SELECT Code FROM dbo.Insurances GROUP BY Code HAVING COUNT(*) > 1)
BEGIN
    ;THROW 50001, 'Duplicate request codes exist; resolve them before adding the unique constraint.', 1;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Insurances') AND name = N'IX_Insurances_Code' AND is_unique = 1)
    CREATE UNIQUE INDEX IX_Insurances_Code ON dbo.Insurances(Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Insurances') AND name = N'IX_Insurances_IsDeleted_CreatedDate')
    CREATE INDEX IX_Insurances_IsDeleted_CreatedDate ON dbo.Insurances(IsDeleted, CreatedDate DESC);

/* Inspection requests are stored separately from insurance requests. */
IF OBJECT_ID(N'dbo.InspectionRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InspectionRequests
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionRequests PRIMARY KEY,
        Code varchar(30) NOT NULL,
        Title nvarchar(500) NOT NULL,
        Creator nvarchar(200) NOT NULL,
        DepartmentProject nvarchar(200) NOT NULL,
        Classification nvarchar(150) NULL,
        InsuranceType nvarchar(200) NULL,
        InspectionDate datetime NOT NULL,
        Notes nvarchar(max) NULL,
        EquipmentJson nvarchar(max) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_InspectionRequests_CreatedAt DEFAULT (GETDATE()),
        IsDeleted bit NOT NULL CONSTRAINT DF_InspectionRequests_IsDeleted DEFAULT (0),
        DeletedAt datetime NULL,
        DeletedBy nvarchar(200) NULL
    );
    CREATE UNIQUE INDEX UX_InspectionRequests_Code ON dbo.InspectionRequests(Code);
    CREATE INDEX IX_InspectionRequests_InspectionDate ON dbo.InspectionRequests(InspectionDate DESC);
END;
IF COL_LENGTH(N'dbo.InspectionRequests', N'InsuranceType') IS NULL
    ALTER TABLE dbo.InspectionRequests ADD InsuranceType nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.InspectionRequests', N'EquipmentJson') IS NOT NULL
    ALTER TABLE dbo.InspectionRequests ALTER COLUMN EquipmentJson nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.InspectionRequests', N'IsDeleted') IS NULL
    ALTER TABLE dbo.InspectionRequests ADD IsDeleted bit NOT NULL
        CONSTRAINT DF_InspectionRequests_IsDeleted DEFAULT (0) WITH VALUES;
IF COL_LENGTH(N'dbo.InspectionRequests', N'DeletedAt') IS NULL
    ALTER TABLE dbo.InspectionRequests ADD DeletedAt datetime NULL;
IF COL_LENGTH(N'dbo.InspectionRequests', N'DeletedBy') IS NULL
    ALTER TABLE dbo.InspectionRequests ADD DeletedBy nvarchar(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionRequests') AND name = N'UX_InspectionRequests_Code')
    CREATE UNIQUE INDEX UX_InspectionRequests_Code ON dbo.InspectionRequests(Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionRequests') AND name = N'IX_InspectionRequests_IsDeleted_InspectionDate')
    CREATE INDEX IX_InspectionRequests_IsDeleted_InspectionDate ON dbo.InspectionRequests(IsDeleted, InspectionDate DESC);

/* Request detail schema. */
IF OBJECT_ID(N'dbo.InsuranceAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceAssets
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceAssets PRIMARY KEY,
        InsuranceId int NOT NULL,
        MaterialCode varchar(20) NULL,
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
        CONSTRAINT FK_InsuranceAssets_Insurances FOREIGN KEY (InsuranceId) REFERENCES dbo.Insurances(Id) ON DELETE CASCADE
    );
END;
IF COL_LENGTH(N'dbo.InsuranceAssets', N'Supplier') IS NULL
    ALTER TABLE dbo.InsuranceAssets ADD Supplier nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.InsuranceAssets', N'MaterialCode') IS NULL
    ALTER TABLE dbo.InsuranceAssets ADD MaterialCode varchar(20) NULL;
IF COL_LENGTH(N'dbo.InsuranceAssets', N'UnitOfMeasure') IS NULL
    ALTER TABLE dbo.InsuranceAssets ADD UnitOfMeasure nvarchar(50) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.InsuranceAssets') AND name = N'DocumentName' AND is_nullable = 0)
    ALTER TABLE dbo.InsuranceAssets ALTER COLUMN DocumentName nvarchar(260) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InsuranceAssets') AND name = N'IX_InsuranceAssets_InsuranceId')
    CREATE INDEX IX_InsuranceAssets_InsuranceId ON dbo.InsuranceAssets(InsuranceId);

/* Shared device catalog, separate from request line items. */
IF OBJECT_ID(N'dbo.Devices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devices
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Devices PRIMARY KEY,
        MaterialCode varchar(20) NULL,
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

/* 2. Add only missing columns required to upgrade an existing database. */
IF COL_LENGTH(N'dbo.Devices', N'MaterialCode') IS NULL
    ALTER TABLE dbo.Devices ADD MaterialCode varchar(20) NULL;
IF COL_LENGTH(N'dbo.Devices', N'DeviceCode') IS NOT NULL
    EXEC(N'UPDATE dbo.Devices SET MaterialCode = DeviceCode WHERE MaterialCode IS NULL');
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Devices') AND name = N'UX_Devices_DeviceCode')
    DROP INDEX UX_Devices_DeviceCode ON dbo.Devices;
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
IF COL_LENGTH(N'dbo.Devices', N'UnitOfMeasure') IS NULL
    ALTER TABLE dbo.Devices ADD UnitOfMeasure nvarchar(50) NULL;
/* Contract numbers belong to an insurance request, not the shared device catalog. */
IF COL_LENGTH(N'dbo.Devices', N'ContractNumber') IS NOT NULL
    ALTER TABLE dbo.Devices DROP COLUMN ContractNumber;

/* Independent device register for inspection; it shares only the Materials catalog. */
IF OBJECT_ID(N'dbo.InspectionDevices', N'U') IS NULL
BEGIN
    SET @InspectionDevicesCreated = 1;
    CREATE TABLE dbo.InspectionDevices
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionDevices PRIMARY KEY,
        MaterialCode varchar(20) NOT NULL,
        DeviceName nvarchar(250) NOT NULL,
        Year int NULL,
        Classification nvarchar(100) NULL,
        Department nvarchar(200) NULL,
        PurchaseDate datetime NULL,
        ExpiryDate datetime NULL,
        Quantity int NULL,
        UnitOfMeasure nvarchar(50) NULL,
        Value decimal(18,2) NOT NULL CONSTRAINT DF_InspectionDevices_Value DEFAULT (0),
        Status nvarchar(50) NOT NULL CONSTRAINT DF_InspectionDevices_Status DEFAULT (N'Äang sá»­ dá»¥ng'),
        Supplier nvarchar(200) NULL
    );
END;

/* Migrate the previous Category field into InsuranceType when it exists. */
IF COL_LENGTH(N'dbo.Devices', N'Category') IS NOT NULL
    EXEC(N'UPDATE dbo.Devices SET InsuranceType = COALESCE(InsuranceType, Category) WHERE Category IS NOT NULL');

/* Enrich devices that were copied from existing request lines. */
;WITH ExistingRequestDevices AS
(
    SELECT a.Equipment, r.Classification, a.InsuranceType, a.PurchaseDate, a.ExpiryDate,
           a.Quantity,
           ROW_NUMBER() OVER (PARTITION BY a.Equipment ORDER BY a.Id) AS RowNum
    FROM dbo.InsuranceAssets AS a
    INNER JOIN dbo.Insurances AS r ON r.Id = a.InsuranceId
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
    FROM dbo.InsuranceAssets AS a
    INNER JOIN dbo.Insurances AS r ON r.Id = a.InsuranceId
    WHERE NULLIF(LTRIM(RTRIM(a.Equipment)), N'') IS NOT NULL
) AS source
WHERE source.RowNum = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Devices AS d WHERE d.DeviceName = source.Equipment);

IF OBJECT_ID(N'dbo.InsuranceAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceAttachments
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceAttachments PRIMARY KEY,
        InsuranceId int NOT NULL,
        InsuranceAssetId int NULL,
        FileName nvarchar(260) NOT NULL,
        StoredFileName nvarchar(260) NOT NULL,
        ContentType nvarchar(150) NULL,
        UploadedAt datetime NOT NULL,
        CONSTRAINT FK_InsuranceAttachments_Insurances FOREIGN KEY (InsuranceId) REFERENCES dbo.Insurances(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InsuranceAttachments') AND name = N'IX_InsuranceAttachments_InsuranceId')
    CREATE INDEX IX_InsuranceAttachments_InsuranceId ON dbo.InsuranceAttachments(InsuranceId);

/* 3. Create catalog tables, then migrate their legacy names and columns. */
IF OBJECT_ID(N'dbo.DeviceStatuses', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Status', N'U') IS NULL
    EXEC sys.sp_rename N'dbo.DeviceStatuses', N'Status';
IF OBJECT_ID(N'dbo.Statuses', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Status', N'U') IS NULL
    EXEC sys.sp_rename N'dbo.Statuses', N'Status';
IF OBJECT_ID(N'dbo.DeviceClassifications', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Classifications', N'U') IS NULL
    EXEC sys.sp_rename N'dbo.DeviceClassifications', N'Classifications';
IF OBJECT_ID(N'dbo.Projects', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DepartmentsProjects', N'U') IS NULL
    EXEC sys.sp_rename N'dbo.Projects', N'DepartmentsProjects';

IF OBJECT_ID(N'dbo.Status', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Status
    (
        StatusId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Status PRIMARY KEY,
        StatusCode varchar(20) NOT NULL,
        StatusName nvarchar(100) NOT NULL,
        Description nvarchar(255) NULL,
        TextColor nvarchar(7) NOT NULL CONSTRAINT DF_Status_TextColor DEFAULT (N'#FFFFFF'),
        BackgroundColor nvarchar(7) NOT NULL CONSTRAINT DF_Status_BackgroundColor DEFAULT (N'#0D6EFD')
    );
END;
IF COL_LENGTH(N'dbo.Status', N'StatusId') IS NULL
    EXEC sys.sp_rename N'dbo.Status.Id', N'StatusId', N'COLUMN';
IF COL_LENGTH(N'dbo.Status', N'StatusName') IS NULL
    EXEC sys.sp_rename N'dbo.Status.Name', N'StatusName', N'COLUMN';
IF COL_LENGTH(N'dbo.Status', N'StatusCode') IS NULL
    EXEC sys.sp_rename N'dbo.Status.Code', N'StatusCode', N'COLUMN';
IF COL_LENGTH(N'dbo.Status', N'Description') IS NULL
    ALTER TABLE dbo.Status ADD Description nvarchar(255) NULL;
IF COL_LENGTH(N'dbo.Status', N'TextColor') IS NULL
    ALTER TABLE dbo.Status ADD TextColor nvarchar(7) NOT NULL CONSTRAINT DF_Status_TextColor_Migration DEFAULT (N'#FFFFFF');
IF COL_LENGTH(N'dbo.Status', N'BackgroundColor') IS NULL
    ALTER TABLE dbo.Status ADD BackgroundColor nvarchar(7) NOT NULL CONSTRAINT DF_Status_BackgroundColor_Migration DEFAULT (N'#0D6EFD');
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'UX_Statuses_Code')
    DROP INDEX UX_Statuses_Code ON dbo.Status;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'UX_Statuses_Name')
    DROP INDEX UX_Statuses_Name ON dbo.Status;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'UX_Status_Code')
    DROP INDEX UX_Status_Code ON dbo.Status;
UPDATE dbo.Status
SET StatusCode = 'TT' + CASE WHEN StatusId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), StatusId), 4)
    ELSE CONVERT(varchar(10), StatusId) END
WHERE NULLIF(LTRIM(RTRIM(StatusCode)), '') IS NULL;
UPDATE dbo.Status
SET TextColor = COALESCE(NULLIF(TextColor, N''), N'#FFFFFF'),
    BackgroundColor = COALESCE(NULLIF(BackgroundColor, N''), N'#0D6EFD')
WHERE TextColor IS NULL OR BackgroundColor IS NULL;
IF EXISTS (SELECT 1 FROM dbo.Status WHERE LEN(StatusName) > 100)
BEGIN
    ;THROW 50002, 'Status contains a name longer than 100 characters; shorten it before applying the catalog schema.', 1;
END;
UPDATE dbo.Status
SET StatusCode = 'TT' + CASE WHEN StatusId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), StatusId), 4)
    ELSE CONVERT(varchar(10), StatusId) END
WHERE LEN(StatusCode) > 20;
IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'StatusCode'
      AND (TYPE_NAME(user_type_id) <> N'varchar' OR max_length <> 20 OR is_nullable = 1)
)
    ALTER TABLE dbo.Status ALTER COLUMN StatusCode varchar(20) NOT NULL;
ALTER TABLE dbo.Status ALTER COLUMN TextColor nvarchar(7) NOT NULL;
ALTER TABLE dbo.Status ALTER COLUMN BackgroundColor nvarchar(7) NOT NULL;

IF OBJECT_ID(N'dbo.Units', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Units
    (
        UnitId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY,
        UnitCode varchar(20) NOT NULL,
        UnitName nvarchar(100) NOT NULL
    );
END;
IF COL_LENGTH(N'dbo.Units', N'UnitId') IS NULL
    EXEC sys.sp_rename N'dbo.Units.Id', N'UnitId', N'COLUMN';
IF COL_LENGTH(N'dbo.Units', N'UnitName') IS NULL
    EXEC sys.sp_rename N'dbo.Units.Name', N'UnitName', N'COLUMN';
IF COL_LENGTH(N'dbo.Units', N'UnitCode') IS NULL
    ALTER TABLE dbo.Units ADD UnitCode varchar(20) NULL;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'UX_Units_Name')
    DROP INDEX UX_Units_Name ON dbo.Units;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'UX_Units_Code')
    DROP INDEX UX_Units_Code ON dbo.Units;
UPDATE dbo.Units
SET UnitCode = 'DVT' + CASE WHEN UnitId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), UnitId), 4)
    ELSE CONVERT(varchar(10), UnitId) END
WHERE NULLIF(LTRIM(RTRIM(UnitCode)), '') IS NULL OR LEN(UnitCode) > 20;
IF EXISTS (SELECT 1 FROM dbo.Units WHERE LEN(UnitName) > 100)
BEGIN
    ;THROW 50003, 'Units contains a name longer than 100 characters; shorten it before applying the catalog schema.', 1;
END;
IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'UnitCode'
      AND (TYPE_NAME(user_type_id) <> N'varchar' OR max_length <> 20 OR is_nullable = 1)
)
    ALTER TABLE dbo.Units ALTER COLUMN UnitCode varchar(20) NOT NULL;

IF OBJECT_ID(N'dbo.Classifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Classifications
    (
        ClassificationId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Classifications PRIMARY KEY,
        ClassificationCode varchar(20) NOT NULL,
        ClassificationName nvarchar(150) NOT NULL,
        Description nvarchar(255) NULL
    );
END;
IF COL_LENGTH(N'dbo.Classifications', N'ClassificationId') IS NULL
    EXEC sys.sp_rename N'dbo.Classifications.Id', N'ClassificationId', N'COLUMN';
IF COL_LENGTH(N'dbo.Classifications', N'ClassificationName') IS NULL
    EXEC sys.sp_rename N'dbo.Classifications.Name', N'ClassificationName', N'COLUMN';
IF COL_LENGTH(N'dbo.Classifications', N'ClassificationCode') IS NULL
    EXEC sys.sp_rename N'dbo.Classifications.Code', N'ClassificationCode', N'COLUMN';
IF COL_LENGTH(N'dbo.Classifications', N'Description') IS NULL
    ALTER TABLE dbo.Classifications ADD Description nvarchar(255) NULL;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Classifications') AND name = N'UX_Classifications_Name')
    DROP INDEX UX_Classifications_Name ON dbo.Classifications;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Classifications') AND name = N'UX_Classifications_Code')
    DROP INDEX UX_Classifications_Code ON dbo.Classifications;
UPDATE dbo.Classifications
SET ClassificationCode = 'PL' + CASE WHEN ClassificationId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), ClassificationId), 4)
    ELSE CONVERT(varchar(10), ClassificationId) END
WHERE NULLIF(LTRIM(RTRIM(ClassificationCode)), '') IS NULL;
IF EXISTS (SELECT 1 FROM dbo.Classifications WHERE LEN(ClassificationName) > 150)
BEGIN
    ;THROW 50004, 'Classifications contains a name longer than 150 characters; shorten it before applying the catalog schema.', 1;
END;
UPDATE dbo.Classifications
SET ClassificationCode = 'PL' + CASE WHEN ClassificationId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), ClassificationId), 4)
    ELSE CONVERT(varchar(10), ClassificationId) END
WHERE LEN(ClassificationCode) > 20;
IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Classifications') AND name = N'ClassificationCode'
      AND (TYPE_NAME(user_type_id) <> N'varchar' OR max_length <> 20 OR is_nullable = 1)
)
    ALTER TABLE dbo.Classifications ALTER COLUMN ClassificationCode varchar(20) NOT NULL;
IF OBJECT_ID(N'dbo.DepartmentsProjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DepartmentsProjects
    (
        DepartmentProjectId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepartmentsProjects PRIMARY KEY,
        Code varchar(30) NOT NULL,
        Name nvarchar(150) NOT NULL,
        EntityType varchar(20) NOT NULL,
        Description nvarchar(255) NULL,
        CONSTRAINT CK_DepartmentsProjects_EntityType CHECK (EntityType IN ('DEPARTMENT', 'PROJECT'))
    );
END;
IF COL_LENGTH(N'dbo.DepartmentsProjects', N'DepartmentProjectId') IS NULL
    EXEC sys.sp_rename N'dbo.DepartmentsProjects.Id', N'DepartmentProjectId', N'COLUMN';
IF COL_LENGTH(N'dbo.DepartmentsProjects', N'Code') IS NULL
    ALTER TABLE dbo.DepartmentsProjects ADD Code varchar(30) NULL;
IF COL_LENGTH(N'dbo.DepartmentsProjects', N'EntityType') IS NULL
    ALTER TABLE dbo.DepartmentsProjects ADD EntityType varchar(20) NOT NULL CONSTRAINT DF_DepartmentsProjects_EntityType_Migration DEFAULT ('DEPARTMENT');
IF COL_LENGTH(N'dbo.DepartmentsProjects', N'Description') IS NULL
    ALTER TABLE dbo.DepartmentsProjects ADD Description nvarchar(255) NULL;
UPDATE dbo.DepartmentsProjects
SET EntityType = 'PROJECT'
WHERE Name LIKE N'Dự án%' AND EntityType <> 'PROJECT';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'UX_Projects_Name')
    DROP INDEX UX_Projects_Name ON dbo.DepartmentsProjects;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'UX_DepartmentsProjects_Code')
    DROP INDEX UX_DepartmentsProjects_Code ON dbo.DepartmentsProjects;
UPDATE dbo.DepartmentsProjects
SET Code = CASE WHEN EntityType = 'PROJECT' THEN 'AD' ELSE 'BP' END
    + RIGHT('0000' + CONVERT(varchar(10), DepartmentProjectId), 4)
WHERE NULLIF(LTRIM(RTRIM(Code)), '') IS NULL OR LEN(Code) > 30;
IF EXISTS (SELECT 1 FROM dbo.DepartmentsProjects WHERE LEN(Name) > 150)
BEGIN
    ;THROW 50005, 'DepartmentsProjects contains a name longer than 150 characters; shorten it before applying the catalog schema.', 1;
END;
IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'Code'
      AND (TYPE_NAME(user_type_id) <> N'varchar' OR max_length <> 30 OR is_nullable = 1)
)
    ALTER TABLE dbo.DepartmentsProjects ALTER COLUMN Code varchar(30) NOT NULL;

IF OBJECT_ID(N'dbo.InsuranceTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceTypes
    (
        InsuranceTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceTypes PRIMARY KEY,
        InsuranceTypeCode varchar(20) NOT NULL,
        InsuranceTypeName nvarchar(200) NOT NULL,
        Description nvarchar(255) NULL
    );
END;

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers
    (
        SupplierId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
        SupplierCode varchar(20) NOT NULL,
        SupplierName nvarchar(200) NOT NULL,
        Description nvarchar(255) NULL
    );
END;

IF OBJECT_ID(N'dbo.Materials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Materials
    (
        MaterialId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Materials PRIMARY KEY,
        MaterialCode varchar(20) NOT NULL,
        MaterialName nvarchar(250) NOT NULL,
        Classification nvarchar(100) NULL,
        UnitOfMeasure nvarchar(50) NULL,
        Quantity int NOT NULL CONSTRAINT DF_Materials_Quantity DEFAULT (1),
        CONSTRAINT CK_Materials_Quantity CHECK (Quantity > 0)
    );
    CREATE UNIQUE INDEX UX_Materials_MaterialCode ON dbo.Materials(MaterialCode);
    SET @MaterialCatalogCreated = 1;
END;
IF OBJECT_ID(N'dbo.InspectionTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InspectionTypes
    (
        InspectionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionTypes PRIMARY KEY,
        InspectionTypeCode varchar(20) NOT NULL,
        InspectionTypeName nvarchar(200) NOT NULL,
        Description nvarchar(255) NULL
    );
END;

IF OBJECT_ID(N'dbo.AccidentCause', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccidentCause
    (
        AccidentCauseId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccidentCauseCode varchar(20) NOT NULL,
        AccidentCauseName nvarchar(200) NOT NULL,
        Description nvarchar(255) NULL
    );
END;

IF OBJECT_ID(N'dbo.AccidentTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccidentTypes
    (
        AccidentTypesId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccidentTypesCode varchar(20) NOT NULL,
        AccidentTypesName nvarchar(200) NOT NULL,
        Description nvarchar(255) NULL
    );
END;


IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Materials') AND name = N'UX_Materials_MaterialName')
    DROP INDEX UX_Materials_MaterialName ON dbo.Materials;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Materials') AND name = N'UX_Materials_MaterialCode')
    CREATE UNIQUE INDEX UX_Materials_MaterialCode ON dbo.Materials(MaterialCode);
IF COL_LENGTH(N'dbo.Materials', N'UnitOfMeasure') IS NULL
    ALTER TABLE dbo.Materials ADD UnitOfMeasure nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.Materials', N'Department') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE material
        SET UnitOfMeasure = device.UnitOfMeasure
        FROM dbo.Materials AS material
        OUTER APPLY
        (
            SELECT TOP (1) tracked.UnitOfMeasure
            FROM dbo.Devices AS tracked
            WHERE tracked.DeviceName = material.MaterialName
            ORDER BY tracked.Id
        ) AS device
        WHERE material.UnitOfMeasure IS NULL;
    ');

    ALTER TABLE dbo.Materials DROP COLUMN Department;
END;

/* Remove the retired IsActive column and its default constraints from old schemas. */
DECLARE @CatalogTableName sysname;
DECLARE @CatalogDefaultName sysname;
DECLARE @CatalogSql nvarchar(max);
DECLARE CatalogActiveColumnCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT catalog.TableName
    FROM (VALUES
        (N'Status'),
        (N'Units'),
        (N'Classifications'),
        (N'DepartmentsProjects')
    ) AS catalog(TableName)
    WHERE COL_LENGTH(N'dbo.' + catalog.TableName, N'IsActive') IS NOT NULL;

OPEN CatalogActiveColumnCursor;
FETCH NEXT FROM CatalogActiveColumnCursor INTO @CatalogTableName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @CatalogDefaultName = NULL;
    SELECT @CatalogDefaultName = constraintInfo.name
    FROM sys.default_constraints AS constraintInfo
    INNER JOIN sys.columns AS columnInfo
        ON columnInfo.object_id = constraintInfo.parent_object_id
       AND columnInfo.column_id = constraintInfo.parent_column_id
    WHERE constraintInfo.parent_object_id = OBJECT_ID(N'dbo.' + @CatalogTableName)
      AND columnInfo.name = N'IsActive';

    IF @CatalogDefaultName IS NOT NULL
    BEGIN
        SET @CatalogSql = N'ALTER TABLE dbo.' + QUOTENAME(@CatalogTableName)
            + N' DROP CONSTRAINT ' + QUOTENAME(@CatalogDefaultName) + N';';
        EXEC sys.sp_executesql @CatalogSql;
        SET @CatalogDefaultName = NULL;
    END;

    SET @CatalogSql = N'ALTER TABLE dbo.' + QUOTENAME(@CatalogTableName) + N' DROP COLUMN IsActive;';
    EXEC sys.sp_executesql @CatalogSql;
    FETCH NEXT FROM CatalogActiveColumnCursor INTO @CatalogTableName;
END;
CLOSE CatalogActiveColumnCursor;
DEALLOCATE CatalogActiveColumnCursor;

/* 4. Migrate legacy catalog data after destination tables are ready. */
IF OBJECT_ID(N'dbo.CatalogItems', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1 FROM dbo.CatalogItems
        WHERE (CategoryKey = N'statuses' AND LEN(Name) > 100)
           OR (CategoryKey = N'units' AND LEN(Name) > 100)
           OR (CategoryKey = N'classifications' AND LEN(Name) > 150)
           OR (CategoryKey = N'projects' AND LEN(Name) > 150)
           OR (CategoryKey = N'materials' AND LEN(Name) > 250)
    )
    BEGIN
        ;THROW 50006, 'CatalogItems contains a name exceeding the new catalog column length; shorten it before migration.', 1;
    END;

    IF COL_LENGTH(N'dbo.CatalogItems', N'TextColor') IS NULL
        ALTER TABLE dbo.CatalogItems ADD TextColor nvarchar(7) NULL;
    IF COL_LENGTH(N'dbo.CatalogItems', N'BackgroundColor') IS NULL
        ALTER TABLE dbo.CatalogItems ADD BackgroundColor nvarchar(7) NULL;

    EXEC(N'
        INSERT INTO dbo.Status (StatusCode, StatusName, TextColor, BackgroundColor)
        SELECT CASE WHEN LEN(source.Code) BETWEEN 1 AND 20 THEN source.Code ELSE ''TT'' + CONVERT(varchar(10), ISNULL((SELECT MAX(StatusId) FROM dbo.Status), 0) + ROW_NUMBER() OVER (ORDER BY source.Id)) END,
               source.Name,
               COALESCE(NULLIF(source.TextColor, N''''), N''#FFFFFF''),
               COALESCE(NULLIF(source.BackgroundColor, N''''), NULLIF(source.Color, N''''), N''#0D6EFD'')
        FROM dbo.CatalogItems AS source
        WHERE source.CategoryKey = N''statuses''
          AND NOT EXISTS (SELECT 1 FROM dbo.Status AS target WHERE target.StatusName = source.Name);

        INSERT INTO dbo.Units (UnitCode, UnitName)
        SELECT ''DVT'' + CONVERT(varchar(10), ISNULL((SELECT MAX(UnitId) FROM dbo.Units), 0) + ROW_NUMBER() OVER (ORDER BY source.Id)), source.Name
        FROM dbo.CatalogItems AS source
        WHERE source.CategoryKey = N''units''
          AND NOT EXISTS (SELECT 1 FROM dbo.Units AS target WHERE target.UnitName = source.Name);

        INSERT INTO dbo.Classifications (ClassificationCode, ClassificationName)
        SELECT ''PL'' + CONVERT(varchar(10), ISNULL((SELECT MAX(ClassificationId) FROM dbo.Classifications), 0) + ROW_NUMBER() OVER (ORDER BY source.Id)), source.Name
        FROM dbo.CatalogItems AS source
        WHERE source.CategoryKey = N''classifications''
          AND NOT EXISTS (SELECT 1 FROM dbo.Classifications AS target WHERE target.ClassificationName = source.Name);

        INSERT INTO dbo.DepartmentsProjects (Code, Name, EntityType)
        SELECT CASE WHEN source.Name LIKE N''Dự án%'' THEN ''AD'' ELSE ''BP'' END
                   + CONVERT(varchar(10), ISNULL((SELECT MAX(DepartmentProjectId) FROM dbo.DepartmentsProjects), 0) + ROW_NUMBER() OVER (ORDER BY source.Id)), source.Name,
               CASE WHEN source.Name LIKE N''Dự án%'' THEN ''PROJECT'' ELSE ''DEPARTMENT'' END
        FROM dbo.CatalogItems AS source
        WHERE source.CategoryKey = N''projects''
          AND NOT EXISTS (SELECT 1 FROM dbo.DepartmentsProjects AS target WHERE target.Name = source.Name);

        INSERT INTO dbo.Materials (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
        SELECT ''TMPL'' + CONVERT(varchar(10), MIN(source.Id)), source.Name, NULL, NULL, 1
        FROM dbo.CatalogItems AS source
        WHERE source.CategoryKey = N''materials''
          AND NOT EXISTS (SELECT 1 FROM dbo.Materials AS target WHERE target.MaterialName = source.Name)
        GROUP BY source.Name;

        UPDATE dbo.Materials
        SET MaterialCode = ''VT'' + CASE WHEN MaterialId < 10000
            THEN RIGHT(''0000'' + CONVERT(varchar(10), MaterialId), 4)
            ELSE CONVERT(varchar(10), MaterialId) END
        WHERE MaterialCode LIKE ''TMPL%'';
    ');
END;

/* Initialize the independent material catalog once from the existing device list. */
IF @MaterialCatalogCreated = 1
BEGIN
    EXEC(N'
        ;WITH ExistingMaterials AS
        (
            SELECT LTRIM(RTRIM(DeviceName)) AS Name,
                   Classification,
                   UnitOfMeasure,
                   Quantity,
                   ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(DeviceName)) ORDER BY Id) AS NameRow,
                   Id AS SourceId
            FROM dbo.Devices
            WHERE NULLIF(LTRIM(RTRIM(DeviceName)), N'''') IS NOT NULL
        )
        INSERT INTO dbo.Materials (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
        SELECT ''TMPD'' + CONVERT(varchar(10), source.SourceId),
               source.Name,
               source.Classification,
               source.UnitOfMeasure,
               CASE WHEN source.Quantity IS NULL OR source.Quantity < 1 THEN 1 ELSE source.Quantity END
        FROM ExistingMaterials AS source
        WHERE source.NameRow = 1
          AND NOT EXISTS (SELECT 1 FROM dbo.Materials AS target WHERE target.MaterialName = source.Name);

        UPDATE dbo.Materials
        SET MaterialCode = ''VT'' + CASE WHEN MaterialId < 10000
            THEN RIGHT(''0000'' + CONVERT(varchar(10), MaterialId), 4)
            ELSE CONVERT(varchar(10), MaterialId) END
        WHERE MaterialCode LIKE ''TMPD%'';
    ');
END;

/* One catalog item owns one code (VT + MaterialId); module records use that same code. */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Devices_Materials_MaterialCode')
    ALTER TABLE dbo.Devices DROP CONSTRAINT FK_Devices_Materials_MaterialCode;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InsuranceAssets_Materials_MaterialCode')
    ALTER TABLE dbo.InsuranceAssets DROP CONSTRAINT FK_InsuranceAssets_Materials_MaterialCode;
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InspectionAssets', N'MaterialCode') IS NULL
    ALTER TABLE dbo.InspectionAssets ADD MaterialCode varchar(20) NULL;
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InspectionAssets', N'DeviceCode') IS NOT NULL
    EXEC(N'UPDATE dbo.InspectionAssets SET MaterialCode = DeviceCode WHERE MaterialCode IS NULL');
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL AND EXISTS
   (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionAssets_Materials_MaterialCode')
    ALTER TABLE dbo.InspectionAssets DROP CONSTRAINT FK_InspectionAssets_Materials_MaterialCode;
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL AND EXISTS
   (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionAssets') AND name = N'UX_InspectionAssets_InspectionRequestCode_DeviceCode')
    DROP INDEX UX_InspectionAssets_InspectionRequestCode_DeviceCode ON dbo.InspectionAssets;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Materials') AND name = N'UX_Materials_MaterialCode')
    DROP INDEX UX_Materials_MaterialCode ON dbo.Materials;

;WITH MissingNames AS
(
    SELECT LTRIM(RTRIM(DeviceName)) AS MaterialName FROM dbo.Devices
    UNION
    SELECT LTRIM(RTRIM(Equipment)) FROM dbo.InsuranceAssets
)
INSERT INTO dbo.Materials (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
SELECT 'TMP' + LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 17), source.MaterialName, NULL, NULL, 1
FROM MissingNames AS source
WHERE NULLIF(source.MaterialName, N'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Materials AS item WHERE item.MaterialName = source.MaterialName);

UPDATE device
SET MaterialCode = catalog.MaterialCode
FROM dbo.Devices AS device
CROSS APPLY
(
    SELECT TOP (1) item.MaterialCode
    FROM dbo.Materials AS item
    WHERE item.MaterialName = device.DeviceName
    ORDER BY item.MaterialId
) AS catalog;

UPDATE asset
SET MaterialCode = catalog.MaterialCode
FROM dbo.InsuranceAssets AS asset
CROSS APPLY
(
    SELECT TOP (1) item.MaterialCode
    FROM dbo.Materials AS item
    WHERE item.MaterialName = asset.Equipment
    ORDER BY item.MaterialId
) AS catalog;

IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL
    UPDATE asset SET MaterialCode = device.MaterialCode
    FROM dbo.InspectionAssets AS asset INNER JOIN dbo.Devices AS device ON device.Id = asset.DeviceId;

DELETE duplicate
FROM dbo.Materials AS duplicate
INNER JOIN
(
    SELECT MaterialId, MIN(MaterialId) OVER (PARTITION BY MaterialName) AS CanonicalId
    FROM dbo.Materials
) AS ranked ON ranked.MaterialId = duplicate.MaterialId
WHERE ranked.MaterialId <> ranked.CanonicalId;

UPDATE dbo.Materials
SET MaterialCode = 'VT' + CASE WHEN MaterialId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), MaterialId), 4)
    ELSE CONVERT(varchar(10), MaterialId) END;
UPDATE device SET MaterialCode = material.MaterialCode
FROM dbo.Devices AS device INNER JOIN dbo.Materials AS material ON material.MaterialName = device.DeviceName;
UPDATE asset SET MaterialCode = material.MaterialCode
FROM dbo.InsuranceAssets AS asset INNER JOIN dbo.Materials AS material ON material.MaterialName = asset.Equipment;
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL
    UPDATE asset SET MaterialCode = device.MaterialCode
    FROM dbo.InspectionAssets AS asset INNER JOIN dbo.Devices AS device ON device.Id = asset.DeviceId;

/* Keep MaterialCode as the only persisted equipment code in inspection details. */
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.InspectionAssets', N'DeviceCode') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.InspectionAssets') AND name = N'CK_InspectionAssets_DeviceCode_MaterialCode')
        ALTER TABLE dbo.InspectionAssets DROP CONSTRAINT CK_InspectionAssets_DeviceCode_MaterialCode;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionAssets') AND name = N'IX_InspectionAssets_DeviceCode')
        DROP INDEX IX_InspectionAssets_DeviceCode ON dbo.InspectionAssets;
    ALTER TABLE dbo.InspectionAssets DROP COLUMN DeviceCode;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Materials') AND name = N'UX_Materials_MaterialCode')
    CREATE UNIQUE INDEX UX_Materials_MaterialCode ON dbo.Materials(MaterialCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Materials') AND name = N'UX_Materials_MaterialName')
    CREATE UNIQUE INDEX UX_Materials_MaterialName ON dbo.Materials(MaterialName);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Devices_Materials_MaterialCode')
    ALTER TABLE dbo.Devices ADD CONSTRAINT FK_Devices_Materials_MaterialCode FOREIGN KEY (MaterialCode) REFERENCES dbo.Materials(MaterialCode);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InsuranceAssets_Materials_MaterialCode')
    ALTER TABLE dbo.InsuranceAssets ADD CONSTRAINT FK_InsuranceAssets_Materials_MaterialCode FOREIGN KEY (MaterialCode) REFERENCES dbo.Materials(MaterialCode);

/* Migrate insurance lookup values already stored as free text. */
;WITH ExistingInsuranceTypes AS
(
    SELECT LTRIM(RTRIM(InsuranceType)) AS Name FROM dbo.Devices
    UNION
    SELECT LTRIM(RTRIM(InsuranceType)) FROM dbo.Insurances
    UNION
    SELECT LTRIM(RTRIM(InsuranceType)) FROM dbo.InsuranceAssets
), NumberedInsuranceTypes AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM ExistingInsuranceTypes
    WHERE NULLIF(Name, N'') IS NOT NULL
)
INSERT INTO dbo.InsuranceTypes (InsuranceTypeCode, InsuranceTypeName)
SELECT 'BHT' + CONVERT(varchar(10), ISNULL((SELECT MAX(InsuranceTypeId) FROM dbo.InsuranceTypes), 0) + source.RowNumber),
       source.Name
FROM NumberedInsuranceTypes AS source
WHERE NOT EXISTS (SELECT 1 FROM dbo.InsuranceTypes AS item WHERE item.InsuranceTypeName = source.Name);

;WITH ExistingSuppliers AS
(
    SELECT LTRIM(RTRIM(Supplier)) AS Name FROM dbo.Devices
    UNION
    SELECT LTRIM(RTRIM(Supplier)) FROM dbo.InsuranceAssets
), NumberedSuppliers AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM ExistingSuppliers
    WHERE NULLIF(Name, N'') IS NOT NULL
)
INSERT INTO dbo.Suppliers (SupplierCode, SupplierName)
SELECT 'NCC' + CONVERT(varchar(10), ISNULL((SELECT MAX(SupplierId) FROM dbo.Suppliers), 0) + source.RowNumber),
       source.Name
FROM NumberedSuppliers AS source
WHERE NOT EXISTS (SELECT 1 FROM dbo.Suppliers AS item WHERE item.SupplierName = source.Name);

/* 5. Synchronize lookup values from existing business records. */
;WITH ExistingStatuses AS
(
    SELECT DISTINCT LTRIM(RTRIM(Status)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
)
INSERT INTO dbo.Status (StatusCode, StatusName, TextColor, BackgroundColor)
SELECT 'TT' + CONVERT(varchar(10), ISNULL((SELECT MAX(StatusId) FROM dbo.Status), 0) + ROW_NUMBER() OVER (ORDER BY source.Name)),
       LEFT(source.Name, 100),
       N'#FFFFFF',
       CASE
           WHEN source.Name = N'Đang sử dụng' THEN N'#198754'
           WHEN source.Name = N'Ngừng sử dụng' THEN N'#6C757D'
           WHEN source.Name = N'Hết hạn' THEN N'#DC3545'
           ELSE N'#0D6EFD'
       END
FROM ExistingStatuses AS source
WHERE NOT EXISTS (SELECT 1 FROM dbo.Status AS item WHERE item.StatusName = source.Name);

INSERT INTO dbo.Units (UnitCode, UnitName)
SELECT 'DVT' + CONVERT(varchar(10), ISNULL((SELECT MAX(UnitId) FROM dbo.Units), 0) + ROW_NUMBER() OVER (ORDER BY source.UnitOfMeasure)),
       LEFT(source.UnitOfMeasure, 100)
FROM
(
    SELECT DISTINCT LTRIM(RTRIM(UnitOfMeasure)) AS UnitOfMeasure FROM dbo.Devices
    UNION
    SELECT DISTINCT LTRIM(RTRIM(UnitOfMeasure)) AS UnitOfMeasure FROM dbo.InsuranceAssets
) AS source
WHERE NULLIF(source.UnitOfMeasure, N'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Units AS item WHERE item.UnitName = source.UnitOfMeasure);

INSERT INTO dbo.Classifications (ClassificationCode, ClassificationName)
SELECT 'PL' + CONVERT(varchar(10), ISNULL((SELECT MAX(ClassificationId) FROM dbo.Classifications), 0) + ROW_NUMBER() OVER (ORDER BY source.Classification)),
       source.Classification
FROM
(
    SELECT DISTINCT LTRIM(RTRIM(Classification)) AS Classification FROM dbo.Devices
    UNION
    SELECT DISTINCT LTRIM(RTRIM(Classification)) AS Classification FROM dbo.Insurances
    UNION
    SELECT DISTINCT LTRIM(RTRIM(Classification)) AS Classification FROM dbo.InspectionRequests
) AS source
WHERE NULLIF(source.Classification, N'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Classifications AS item WHERE item.ClassificationName = source.Classification);

IF EXISTS
(
    SELECT 1 FROM dbo.Devices WHERE LEN(LTRIM(RTRIM(Department))) > 150
    UNION ALL
    SELECT 1 FROM dbo.Insurances WHERE LEN(LTRIM(RTRIM(DepartmentProject))) > 150
    UNION ALL
    SELECT 1 FROM dbo.InspectionRequests WHERE LEN(LTRIM(RTRIM(DepartmentProject))) > 150
)
BEGIN
    ;THROW 50007, 'A department/project name exceeds 150 characters; shorten it before catalog synchronization.', 1;
END;

INSERT INTO dbo.DepartmentsProjects (Code, Name, EntityType)
SELECT CASE WHEN source.DepartmentProject LIKE N'Dự án%' THEN 'AD' ELSE 'BP' END
           + CONVERT(varchar(10), ISNULL((SELECT MAX(DepartmentProjectId) FROM dbo.DepartmentsProjects), 0) + ROW_NUMBER() OVER (ORDER BY source.DepartmentProject)),
       source.DepartmentProject,
       CASE WHEN source.DepartmentProject LIKE N'Dự án%' THEN 'PROJECT' ELSE 'DEPARTMENT' END
FROM
(
    SELECT DISTINCT LTRIM(RTRIM(Department)) AS DepartmentProject FROM dbo.Devices
    UNION
    SELECT DISTINCT LTRIM(RTRIM(DepartmentProject)) AS DepartmentProject FROM dbo.Insurances
    UNION
    SELECT DISTINCT LTRIM(RTRIM(DepartmentProject)) AS DepartmentProject FROM dbo.InspectionRequests
) AS source
WHERE NULLIF(source.DepartmentProject, N'') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.DepartmentsProjects AS item WHERE item.Name = source.DepartmentProject);

/* Replace legacy generated catalog codes and backfill device codes before indexing. */
UPDATE dbo.Status
SET StatusCode = 'TT' + CASE WHEN StatusId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), StatusId), 4)
    ELSE CONVERT(varchar(10), StatusId) END
WHERE LEN(StatusCode) = 20 AND LEFT(StatusCode, 1) = 'S'
  AND SUBSTRING(StatusCode, 2, 19) NOT LIKE '%[^0-9A-F]%';
UPDATE dbo.Units
SET UnitCode = 'DVT' + CASE WHEN UnitId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), UnitId), 4)
    ELSE CONVERT(varchar(10), UnitId) END
WHERE LEN(UnitCode) = 20 AND LEFT(UnitCode, 1) = 'U'
  AND SUBSTRING(UnitCode, 2, 19) NOT LIKE '%[^0-9A-F]%';
UPDATE dbo.Classifications
SET ClassificationCode = 'PL' + CASE WHEN ClassificationId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), ClassificationId), 4)
    ELSE CONVERT(varchar(10), ClassificationId) END
WHERE LEN(ClassificationCode) = 20 AND LEFT(ClassificationCode, 1) = 'C'
  AND SUBSTRING(ClassificationCode, 2, 19) NOT LIKE '%[^0-9A-F]%';
UPDATE dbo.DepartmentsProjects
SET Code = CASE WHEN EntityType = 'PROJECT' THEN 'AD' ELSE 'BP' END
    + CASE WHEN DepartmentProjectId < 10000
        THEN RIGHT('0000' + CONVERT(varchar(10), DepartmentProjectId), 4)
        ELSE CONVERT(varchar(10), DepartmentProjectId) END
WHERE (LEN(Code) = 30 AND LEFT(Code, 1) IN ('D', 'P')
       AND SUBSTRING(Code, 2, 29) NOT LIKE '%[^0-9A-F]%')
   OR (LEFT(Code, 1) IN ('D', 'P') AND Code NOT LIKE 'BP%' AND Code NOT LIKE 'AD%');
IF EXISTS (SELECT 1 FROM dbo.Devices WHERE MaterialCode IS NULL)
    THROW 50007, 'A device does not link to a material catalog item. Add the item to Materials and rerun the script.', 1;
ALTER TABLE dbo.Devices ALTER COLUMN MaterialCode varchar(20) NOT NULL;
IF COL_LENGTH(N'dbo.Devices', N'DeviceCode') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Devices') AND name = N'CK_Devices_DeviceCode_MaterialCode')
        ALTER TABLE dbo.Devices DROP CONSTRAINT CK_Devices_DeviceCode_MaterialCode;
    ALTER TABLE dbo.Devices DROP COLUMN DeviceCode;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionDevices') AND name = N'IX_InspectionDevices_MaterialCode')
    CREATE INDEX IX_InspectionDevices_MaterialCode ON dbo.InspectionDevices(MaterialCode);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionDevices_Materials_MaterialCode')
    ALTER TABLE dbo.InspectionDevices ADD CONSTRAINT FK_InspectionDevices_Materials_MaterialCode
        FOREIGN KEY (MaterialCode) REFERENCES dbo.Materials(MaterialCode);

/* Copy existing insurance devices once to preserve current inspection line links; subsequent data is independent. */
IF @InspectionDevicesCreated = 1
BEGIN
    SET IDENTITY_INSERT dbo.InspectionDevices ON;
    INSERT INTO dbo.InspectionDevices (Id, MaterialCode, DeviceName, Year, Classification, Department, PurchaseDate, ExpiryDate,
                                       Quantity, UnitOfMeasure, Value, Status, Supplier)
    SELECT device.Id, device.MaterialCode, device.DeviceName, device.Year, device.Classification, device.Department, device.PurchaseDate,
           device.ExpiryDate, device.Quantity, device.UnitOfMeasure, device.Value, device.Status, device.Supplier
    FROM dbo.Devices AS device
    WHERE NOT EXISTS (SELECT 1 FROM dbo.InspectionDevices AS existing WHERE existing.Id = device.Id);
    SET IDENTITY_INSERT dbo.InspectionDevices OFF;
END;

IF EXISTS (SELECT 1 FROM dbo.InsuranceAssets WHERE MaterialCode IS NULL)
    THROW 50008, 'An insurance detail does not link to a material catalog item.', 1;
ALTER TABLE dbo.InsuranceAssets ALTER COLUMN MaterialCode varchar(20) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'UX_Status_Code')
    CREATE UNIQUE INDEX UX_Status_Code ON dbo.Status(StatusCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'UX_Units_Code')
    CREATE UNIQUE INDEX UX_Units_Code ON dbo.Units(UnitCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Classifications') AND name = N'UX_Classifications_Code')
    CREATE UNIQUE INDEX UX_Classifications_Code ON dbo.Classifications(ClassificationCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'UX_DepartmentsProjects_Code')
    CREATE UNIQUE INDEX UX_DepartmentsProjects_Code ON dbo.DepartmentsProjects(Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InsuranceTypes') AND name = N'UX_InsuranceTypes_Code')
    CREATE UNIQUE INDEX UX_InsuranceTypes_Code ON dbo.InsuranceTypes(InsuranceTypeCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suppliers') AND name = N'UX_Suppliers_Code')
    CREATE UNIQUE INDEX UX_Suppliers_Code ON dbo.Suppliers(SupplierCode);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'CK_DepartmentsProjects_EntityType')
    ALTER TABLE dbo.DepartmentsProjects ADD CONSTRAINT CK_DepartmentsProjects_EntityType CHECK (EntityType IN ('DEPARTMENT', 'PROJECT'));

/* Inspection details use MaterialCode as the single equipment code. */
IF OBJECT_ID(N'dbo.InspectionAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InspectionAssets
    (
        InspectionAssetId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionAssets PRIMARY KEY,
        InspectionRequestCode varchar(30) NOT NULL,
        MaterialCode varchar(20) NULL,
        DeviceId int NOT NULL,
        DeviceName nvarchar(250) NOT NULL,
        Year int NULL,
        Classification nvarchar(100) NULL,
        Department nvarchar(200) NULL,
        PurchaseDate datetime NULL,
        ExpiryDate datetime NULL,
        Quantity int NULL,
        UnitOfMeasure nvarchar(50) NULL,
        Value decimal(18,2) NOT NULL CONSTRAINT DF_InspectionAssets_Value DEFAULT (0),
        InspectionFee decimal(18,2) NOT NULL CONSTRAINT DF_InspectionAssets_InspectionFee DEFAULT (0),
        InspectionType nvarchar(200) NULL,
        Status nvarchar(50) NULL,
        Supplier nvarchar(200) NULL,
        /* DeviceId remains a historical reference value; the other columns are a snapshot. */
        CONSTRAINT FK_InspectionAssets_InspectionRequests_InspectionRequestCode
            FOREIGN KEY (InspectionRequestCode) REFERENCES dbo.InspectionRequests(Code) ON DELETE CASCADE
    );
END;
IF COL_LENGTH(N'dbo.InspectionAssets', N'InspectionType') IS NULL
    ALTER TABLE dbo.InspectionAssets ADD InspectionType nvarchar(200) NULL;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionAssets_Devices_DeviceId')
    ALTER TABLE dbo.InspectionAssets DROP CONSTRAINT FK_InspectionAssets_Devices_DeviceId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionAssets_InspectionDevices_DeviceId')
    ALTER TABLE dbo.InspectionAssets DROP CONSTRAINT FK_InspectionAssets_InspectionDevices_DeviceId;
IF COL_LENGTH(N'dbo.InspectionAssets', N'MaterialCode') IS NULL
    ALTER TABLE dbo.InspectionAssets ADD MaterialCode varchar(20) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionAssets_Materials_MaterialCode')
    ALTER TABLE dbo.InspectionAssets ADD CONSTRAINT FK_InspectionAssets_Materials_MaterialCode
        FOREIGN KEY (MaterialCode) REFERENCES dbo.Materials(MaterialCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionAssets') AND name = N'UX_InspectionAssets_InspectionRequestCode_DeviceId')
    CREATE UNIQUE INDEX UX_InspectionAssets_InspectionRequestCode_DeviceId
        ON dbo.InspectionAssets(InspectionRequestCode, DeviceId);
IF EXISTS (SELECT 1 FROM dbo.InspectionAssets WHERE MaterialCode IS NULL)
    THROW 50009, 'An inspection detail does not link to a material catalog item.', 1;
ALTER TABLE dbo.InspectionAssets ALTER COLUMN MaterialCode varchar(20) NOT NULL;
IF OBJECT_ID(N'dbo.InspectionAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InspectionAttachments
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionAttachments PRIMARY KEY,
        InspectionRequestId int NOT NULL,
        InspectionAssetId int NULL,
        FileName nvarchar(260) NOT NULL,
        StoredFileName nvarchar(260) NOT NULL,
        ContentType nvarchar(150) NULL,
        UploadedAt datetime NOT NULL,
        CONSTRAINT FK_InspectionAttachments_InspectionRequests
            FOREIGN KEY (InspectionRequestId) REFERENCES dbo.InspectionRequests(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InspectionAttachments') AND name = N'IX_InspectionAttachments_InspectionRequestId')
    CREATE INDEX IX_InspectionAttachments_InspectionRequestId ON dbo.InspectionAttachments(InspectionRequestId);

/* Sample materials also use the single VT + MaterialId code format. */
IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE MaterialName = N'Máy nén khí mẫu')
BEGIN
    INSERT INTO dbo.Materials (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
    VALUES ('TMPSEED01', N'Máy nén khí mẫu', N'Thiết bị', N'Cái', 1);
    UPDATE dbo.Materials SET MaterialCode = 'VT' + RIGHT('0000' + CONVERT(varchar(10), MaterialId), 4)
    WHERE MaterialCode = 'TMPSEED01';
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE MaterialName = N'Nồi hơi mẫu')
BEGIN
    INSERT INTO dbo.Materials (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
    VALUES ('TMPSEED02', N'Nồi hơi mẫu', N'Thiết bị', N'Cái', 1);
    UPDATE dbo.Materials SET MaterialCode = 'VT' + RIGHT('0000' + CONVERT(varchar(10), MaterialId), 4)
    WHERE MaterialCode = 'TMPSEED02';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceName = N'Máy nén khí mẫu')
    INSERT INTO dbo.Devices (MaterialCode, DeviceName, Year, Classification, Department, PurchaseDate, ExpiryDate,
                             Quantity, UnitOfMeasure, Value, InsuranceFee, Status, Supplier)
    SELECT material.MaterialCode, material.MaterialName, 2024, material.Classification,
           N'Phòng kỹ thuật', '2024-03-15', '2027-03-15', 1, material.UnitOfMeasure,
           25000000, 350000, N'Đang sử dụng', N'Công ty Thiết bị An Phát'
    FROM dbo.Materials AS material WHERE material.MaterialName = N'Máy nén khí mẫu';
IF NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceName = N'Nồi hơi mẫu')
    INSERT INTO dbo.Devices (MaterialCode, DeviceName, Year, Classification, Department, PurchaseDate, ExpiryDate,
                             Quantity, UnitOfMeasure, Value, InsuranceFee, Status, Supplier)
    SELECT material.MaterialCode, material.MaterialName, 2023, material.Classification,
           N'Phòng kỹ thuật', '2023-08-01', '2026-08-01', 1, material.UnitOfMeasure,
           48000000, 620000, N'Đang sử dụng', N'Công ty Cơ khí Việt'
    FROM dbo.Materials AS material WHERE material.MaterialName = N'Nồi hơi mẫu';

/* Add the example devices to the inspection register as independent rows. */
SET IDENTITY_INSERT dbo.InspectionDevices ON;
INSERT INTO dbo.InspectionDevices (Id, MaterialCode, DeviceName, Year, Classification, Department, PurchaseDate, ExpiryDate,
                                   Quantity, UnitOfMeasure, Value, Status, Supplier)
SELECT device.Id, device.MaterialCode, device.DeviceName, device.Year, device.Classification, device.Department,
       device.PurchaseDate, device.ExpiryDate, device.Quantity, device.UnitOfMeasure, device.Value, device.Status, device.Supplier
FROM dbo.Devices AS device
WHERE @InspectionDevicesCreated = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.InspectionDevices AS existing WHERE existing.Id = device.Id);
SET IDENTITY_INSERT dbo.InspectionDevices OFF;
IF NOT EXISTS (SELECT 1 FROM dbo.InspectionRequests WHERE Code = 'KD2026-00001')
    INSERT INTO dbo.InspectionRequests (Code, Title, Creator, DepartmentProject, Classification, InspectionDate, Notes, CreatedAt)
    VALUES ('KD2026-00001', N'Phiếu kiểm định thiết bị mẫu', N'Nguyễn Văn A', N'Phòng kỹ thuật', N'Thiết bị',
            '2026-10-29', N'Dữ liệu mẫu để kiểm tra liên kết phiếu và thiết bị.', GETDATE());

INSERT INTO dbo.InspectionAssets
    (InspectionRequestCode, MaterialCode, DeviceId, DeviceName, Year, Classification, Department,
     PurchaseDate, ExpiryDate, Quantity, UnitOfMeasure, Value, InspectionFee, Status, Supplier)
SELECT 'KD2026-00001', device.MaterialCode, device.Id, device.DeviceName, device.Year, device.Classification,
       device.Department, device.PurchaseDate, device.ExpiryDate, device.Quantity, device.UnitOfMeasure,
       device.Value, CAST(0 AS decimal(18,2)), device.Status, device.Supplier
FROM dbo.InspectionDevices AS device
WHERE @InspectionDevicesCreated = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.InspectionAssets AS saved
                  WHERE saved.InspectionRequestCode = 'KD2026-00001' AND saved.DeviceId = device.Id);

/* Each administration catalog stores a single row per display name. */
;WITH Ranked AS
(
    SELECT StatusId, ROW_NUMBER() OVER (PARTITION BY StatusName ORDER BY StatusId) AS RowNumber
    FROM dbo.Status
)
DELETE item FROM dbo.Status AS item INNER JOIN Ranked AS ranked ON ranked.StatusId = item.StatusId WHERE ranked.RowNumber > 1;
;WITH Ranked AS
(
    SELECT UnitId, ROW_NUMBER() OVER (PARTITION BY UnitName ORDER BY UnitId) AS RowNumber
    FROM dbo.Units
)
DELETE item FROM dbo.Units AS item INNER JOIN Ranked AS ranked ON ranked.UnitId = item.UnitId WHERE ranked.RowNumber > 1;
;WITH Ranked AS
(
    SELECT ClassificationId, ROW_NUMBER() OVER (PARTITION BY ClassificationName ORDER BY ClassificationId) AS RowNumber
    FROM dbo.Classifications
)
DELETE item FROM dbo.Classifications AS item INNER JOIN Ranked AS ranked ON ranked.ClassificationId = item.ClassificationId WHERE ranked.RowNumber > 1;
;WITH Ranked AS
(
    SELECT DepartmentProjectId, ROW_NUMBER() OVER (PARTITION BY Name ORDER BY DepartmentProjectId) AS RowNumber
    FROM dbo.DepartmentsProjects
)
DELETE item FROM dbo.DepartmentsProjects AS item INNER JOIN Ranked AS ranked ON ranked.DepartmentProjectId = item.DepartmentProjectId WHERE ranked.RowNumber > 1;
;WITH Ranked AS
(
    SELECT InsuranceTypeId, ROW_NUMBER() OVER (PARTITION BY InsuranceTypeName ORDER BY InsuranceTypeId) AS RowNumber
    FROM dbo.InsuranceTypes
)
DELETE item FROM dbo.InsuranceTypes AS item INNER JOIN Ranked AS ranked ON ranked.InsuranceTypeId = item.InsuranceTypeId WHERE ranked.RowNumber > 1;
;WITH Ranked AS
(
    SELECT SupplierId, ROW_NUMBER() OVER (PARTITION BY SupplierName ORDER BY SupplierId) AS RowNumber
    FROM dbo.Suppliers
)
DELETE item FROM dbo.Suppliers AS item INNER JOIN Ranked AS ranked ON ranked.SupplierId = item.SupplierId WHERE ranked.RowNumber > 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Status') AND name = N'UX_Status_Name')
    CREATE UNIQUE INDEX UX_Status_Name ON dbo.Status(StatusName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'UX_Units_Name')
    CREATE UNIQUE INDEX UX_Units_Name ON dbo.Units(UnitName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Classifications') AND name = N'UX_Classifications_Name')
    CREATE UNIQUE INDEX UX_Classifications_Name ON dbo.Classifications(ClassificationName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DepartmentsProjects') AND name = N'UX_DepartmentsProjects_Name')
    CREATE UNIQUE INDEX UX_DepartmentsProjects_Name ON dbo.DepartmentsProjects(Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InsuranceTypes') AND name = N'UX_InsuranceTypes_Name')
    CREATE UNIQUE INDEX UX_InsuranceTypes_Name ON dbo.InsuranceTypes(InsuranceTypeName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suppliers') AND name = N'UX_Suppliers_Name')
    CREATE UNIQUE INDEX UX_Suppliers_Name ON dbo.Suppliers(SupplierName);

IF OBJECT_ID(N'dbo.CatalogItems', N'U') IS NOT NULL
    DROP TABLE dbo.CatalogItems;

/* Accident rows retain a material snapshot so historical loss details stay readable. */
IF OBJECT_ID(N'dbo.Accidents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accidents
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accidents PRIMARY KEY,
        AccidentCode varchar(30) NOT NULL,
        MaterialCode varchar(20) NOT NULL,
        MaterialName nvarchar(250) NOT NULL,
        Classification nvarchar(150) NULL,
        Department nvarchar(150) NULL,
        Value decimal(18,2) NOT NULL CONSTRAINT DF_Accidents_Value DEFAULT (0),
        LossDescription nvarchar(1000) NOT NULL,
        AccidentDate datetime NOT NULL CONSTRAINT DF_Accidents_AccidentDate DEFAULT (GETDATE()),
        AccidentType nvarchar(200) NULL,
        AccidentCause nvarchar(200) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_Accidents_IsDeleted DEFAULT (0),
        DeletedAt datetime NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_Accidents_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_Accidents_Materials_MaterialCode
            FOREIGN KEY (MaterialCode) REFERENCES dbo.Materials(MaterialCode)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Accidents') AND name = N'UX_Accidents_AccidentCode')
    CREATE UNIQUE INDEX UX_Accidents_AccidentCode ON dbo.Accidents(AccidentCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Accidents') AND name = N'IX_Accidents_IsDeleted_AccidentDate')
    CREATE INDEX IX_Accidents_IsDeleted_AccidentDate ON dbo.Accidents(IsDeleted, AccidentDate DESC);

/* Idempotent grid examples: use existing material catalog entries and keep their code FK valid. */
;WITH SeedMaterials AS
(
    SELECT TOP (3)
           material.MaterialCode,
           material.MaterialName,
           material.Classification,
           ROW_NUMBER() OVER (ORDER BY material.MaterialId) AS RowNumber
    FROM dbo.Materials AS material
    ORDER BY material.MaterialId
)
INSERT INTO dbo.Accidents
    (AccidentCode, MaterialCode, MaterialName, Classification, Department, Value,
     LossDescription, AccidentDate, AccidentType, AccidentCause, IsDeleted, CreatedAt)
SELECT 'ACC-2026-' + RIGHT('0000' + CONVERT(varchar(4), seed.RowNumber), 4),
       seed.MaterialCode,
       seed.MaterialName,
       COALESCE(seed.Classification, N'Vật tư'),
       COALESCE(device.Department, N'Bộ phận 01'),
       COALESCE(device.Value, 0),
       CASE seed.RowNumber
           WHEN 1 THEN N'Thiết bị bị ảnh hưởng do nước mưa tràn vào khu vực vận hành.'
           WHEN 2 THEN N'Va chạm trong quá trình vận chuyển làm hư hỏng một phần thiết bị.'
           ELSE N'Phát hiện hư hỏng tài sản trong quá trình kiểm tra hiện trường.'
       END,
       DATEADD(day, -seed.RowNumber, CONVERT(date, GETDATE())),
       CASE seed.RowNumber WHEN 1 THEN N'Tai nạn tài sản' WHEN 2 THEN N'Va chạm' ELSE N'Tổn thất thiết bị' END,
       CASE seed.RowNumber WHEN 1 THEN N'Thời tiết' WHEN 2 THEN N'Lỗi vận hành' ELSE N'Đang xác minh' END,
       0,
       GETDATE()
FROM SeedMaterials AS seed
OUTER APPLY
(
    SELECT TOP (1) tracked.Department, tracked.Value
    FROM dbo.Devices AS tracked
    WHERE tracked.MaterialCode = seed.MaterialCode
    ORDER BY tracked.Id
) AS device
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Accidents AS existing
    WHERE existing.AccidentCode = 'ACC-2026-' + RIGHT('0000' + CONVERT(varchar(4), seed.RowNumber), 4)
);
