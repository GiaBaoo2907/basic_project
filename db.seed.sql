USE DeviceManagementDb;
GO

/* Sample data uses the catalog as the root; device and material codes share VT + MaterialId. */
;MERGE dbo.Materials AS target
USING
(
    VALUES
        ('TMPM0001', N'Tủ lạnh', N'Thiết bị', N'Cái', 1),
        ('TMPM0002', N'Máy phát điện', N'Thiết bị', N'Bộ', 1),
        ('TMPM0003', N'Xe nâng hàng', N'Phương tiện', N'Chiếc', 1),
        ('TMPM0004', N'Máy móc thi công', N'Tài sản', N'Bộ', 1),
        ('TMPM0005', N'Máy in văn phòng', N'Thiết bị', N'Cái', 1)
) AS seed(MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
ON target.MaterialName = seed.MaterialName
WHEN MATCHED THEN
    UPDATE SET Classification = COALESCE(target.Classification, seed.Classification),
               UnitOfMeasure = COALESCE(target.UnitOfMeasure, seed.UnitOfMeasure),
               Quantity = CASE WHEN target.Quantity < 1 THEN seed.Quantity ELSE target.Quantity END
WHEN NOT MATCHED BY TARGET THEN
    INSERT (MaterialCode, MaterialName, Classification, UnitOfMeasure, Quantity)
    VALUES (seed.MaterialCode, seed.MaterialName, seed.Classification, seed.UnitOfMeasure, seed.Quantity);

UPDATE dbo.Materials
SET MaterialCode = 'VT' + CASE WHEN MaterialId < 10000
    THEN RIGHT('0000' + CONVERT(varchar(10), MaterialId), 4)
    ELSE CONVERT(varchar(10), MaterialId) END
WHERE MaterialCode LIKE 'TMPM%';

;MERGE dbo.Devices AS target
USING
(
    SELECT catalog.MaterialCode, seed.DeviceName, seed.Year,
           seed.Classification, seed.InsuranceType, seed.Department, seed.PurchaseDate, seed.ExpiryDate,
           seed.Quantity, seed.UnitOfMeasure, seed.Value, seed.InsuranceFee, seed.Status, seed.Supplier
    FROM
    (
    VALUES
        (N'Tủ lạnh', 2026, N'Thiết bị', N'Bảo hiểm trách nhiệm bên thứ ba', N'Dự án 01', CONVERT(datetime, '20261007', 112), CONVERT(datetime, '20271007', 112), 1, N'Cái', CAST(20000000 AS decimal(18,2)), CAST(100000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Thiết bị Á Châu'),
        (N'Máy phát điện', 2024, N'Thiết bị', N'Bảo hiểm thiết bị', N'Dự án 02', CONVERT(datetime, '20260115', 112), CONVERT(datetime, '20270115', 112), 1, N'Bộ', CAST(185000000 AS decimal(18,2)), CAST(925000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Cơ điện Việt'),
        (N'Xe nâng hàng', 2023, N'Phương tiện', N'Bảo hiểm phương tiện', N'Kho vận', CONVERT(datetime, '20260310', 112), CONVERT(datetime, '20270310', 112), 1, N'Chiếc', CAST(420000000 AS decimal(18,2)), CAST(2100000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Xe nâng Miền Nam'),
        (N'Máy móc thi công', 2025, N'Tài sản', N'Bảo hiểm mọi rủi ro', N'Dự án 01', CONVERT(datetime, '20260201', 112), CONVERT(datetime, '20270201', 112), 1, N'Bộ', CAST(760000000 AS decimal(18,2)), CAST(3800000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Xây dựng và Thiết bị'),
        (N'Máy in văn phòng', 2022, N'Thiết bị', N'Bảo hiểm tài sản', N'Phòng Hành chính', CONVERT(datetime, '20260105', 112), CONVERT(datetime, '20270105', 112), 1, N'Cái', CAST(18000000 AS decimal(18,2)), CAST(90000 AS decimal(18,2)), N'Đang sử dụng', N'Công ty Văn phòng phẩm Việt')
    ) AS seed(DeviceName, Year, Classification, InsuranceType, Department, PurchaseDate, ExpiryDate, Quantity, UnitOfMeasure, Value, InsuranceFee, Status, Supplier)
    INNER JOIN dbo.Materials AS catalog ON catalog.MaterialName = seed.DeviceName
) AS seed
ON target.DeviceName = seed.DeviceName
WHEN MATCHED THEN
    UPDATE SET Year = COALESCE(target.Year, seed.Year),
               Classification = COALESCE(target.Classification, seed.Classification),
               InsuranceType = COALESCE(NULLIF(target.InsuranceType, N''), seed.InsuranceType),
               Department = COALESCE(NULLIF(target.Department, N''), seed.Department),
               PurchaseDate = COALESCE(target.PurchaseDate, seed.PurchaseDate),
               ExpiryDate = COALESCE(target.ExpiryDate, seed.ExpiryDate),
               Quantity = COALESCE(target.Quantity, seed.Quantity),
               UnitOfMeasure = COALESCE(NULLIF(target.UnitOfMeasure, N''), seed.UnitOfMeasure),
               Value = CASE WHEN target.Value = 0 THEN seed.Value ELSE target.Value END,
               InsuranceFee = CASE WHEN target.InsuranceFee = 0 THEN seed.InsuranceFee ELSE target.InsuranceFee END,
               Status = COALESCE(target.Status, seed.Status),
               Supplier = COALESCE(NULLIF(target.Supplier, N''), seed.Supplier)
WHEN NOT MATCHED BY TARGET THEN
    INSERT (MaterialCode, DeviceName, Year, Classification, InsuranceType, Department, PurchaseDate, ExpiryDate, Quantity, UnitOfMeasure, Value, InsuranceFee, Status, Supplier)
    VALUES (seed.MaterialCode, seed.DeviceName, seed.Year, seed.Classification, seed.InsuranceType, seed.Department,
            seed.PurchaseDate, seed.ExpiryDate, seed.Quantity, seed.UnitOfMeasure, seed.Value, seed.InsuranceFee, seed.Status, seed.Supplier);

/* Seed the root material catalog. Names are unique in this list. */
/* Every tracked device uses the selected root catalog code as its device code too. */
UPDATE device
SET device.MaterialCode = material.MaterialCode,
    device.DeviceName = material.MaterialName,
    device.Classification = material.Classification,
    device.UnitOfMeasure = material.UnitOfMeasure
FROM dbo.Devices AS device
INNER JOIN dbo.Materials AS material ON material.MaterialName = device.DeviceName;

/* Seed separate inspection tracking records for the sample inventory. */
INSERT INTO dbo.InspectionDevices
    (MaterialCode, DeviceName, Year, Classification, Department, PurchaseDate, ExpiryDate, Quantity, UnitOfMeasure, Value, Status, Supplier)
SELECT device.MaterialCode, device.DeviceName, device.Year, device.Classification, device.Department, device.PurchaseDate,
       device.ExpiryDate, device.Quantity, device.UnitOfMeasure, device.Value, device.Status, device.Supplier
FROM dbo.Devices AS device
WHERE device.DeviceName IN (N'Tủ lạnh', N'Máy phát điện', N'Xe nâng hàng', N'Máy móc thi công', N'Máy in văn phòng')
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.InspectionDevices AS existing
      WHERE existing.MaterialCode = device.MaterialCode
        AND ISNULL(existing.Year, -1) = ISNULL(device.Year, -1)
        AND ISNULL(existing.Department, N'') = ISNULL(device.Department, N'')
  );

/* Seed sample requests without overwriting requests edited by users. */
;MERGE dbo.Insurances AS target
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

/* Attach one sample material to each sample insurance request. */
INSERT INTO dbo.InsuranceAssets
    (InsuranceId, MaterialCode, Equipment, PurchaseDate, ExpiryDate, Value, Premium, Quantity, UnitOfMeasure, InsuranceType, ContractNumber, Supplier)
SELECT request.Id, device.MaterialCode, device.DeviceName, device.PurchaseDate, device.ExpiryDate, device.Value, device.InsuranceFee,
       device.Quantity, device.UnitOfMeasure, device.InsuranceType, sample.ContractNumber, device.Supplier
FROM
(
    VALUES
        (N'TBBH2026-005', N'Máy móc thi công', N'HD-BH-2026-005'),
        (N'TBBH2026-004', N'Máy in văn phòng', N'HD-BH-2026-004'),
        (N'TBBH2026-003', N'Máy phát điện', N'HD-BH-2026-003'),
        (N'TBBH2026-002', N'Xe nâng hàng', N'HD-BH-2026-002'),
        (N'TBBH2026-001', N'Tủ lạnh', N'HD-BH-2026-001')
) AS sample(RequestCode, DeviceName, ContractNumber)
INNER JOIN dbo.Insurances AS request ON request.Code = sample.RequestCode
INNER JOIN dbo.Devices AS device ON device.DeviceName = sample.DeviceName
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.InsuranceAssets AS existing
    WHERE existing.InsuranceId = request.Id
      AND existing.Equipment = device.DeviceName
      AND existing.ContractNumber = sample.ContractNumber
);

UPDATE asset
SET MaterialCode = material.MaterialCode,
    Equipment = material.MaterialName,
    UnitOfMeasure = COALESCE(asset.UnitOfMeasure, material.UnitOfMeasure)
FROM dbo.InsuranceAssets AS asset
INNER JOIN dbo.Materials AS material ON material.MaterialName = asset.Equipment;

/* Add lookup options introduced by the sample records. */
;WITH SeedStatuses AS
(
    SELECT DISTINCT LTRIM(RTRIM(Status)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(Status)), N'') IS NOT NULL
), NumberedStatuses AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM SeedStatuses
)
INSERT INTO dbo.Status (StatusCode, StatusName, TextColor, BackgroundColor)
SELECT 'TT' + CASE WHEN nextId.StatusId < 10000
           THEN RIGHT('0000' + CONVERT(varchar(10), nextId.StatusId), 4)
           ELSE CONVERT(varchar(10), nextId.StatusId) END,
       LEFT(source.Name, 100), N'#FFFFFF',
       CASE
           WHEN source.Name = N'Đang sử dụng' THEN N'#198754'
           WHEN source.Name = N'Ngừng sử dụng' THEN N'#6C757D'
           WHEN source.Name = N'Hết hạn' THEN N'#DC3545'
           ELSE N'#0D6EFD'
       END
FROM NumberedStatuses AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(StatusId) FROM dbo.Status), 0) + source.RowNumber AS StatusId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Status AS item WHERE item.StatusName = source.Name);

;WITH SeedUnits AS
(
    SELECT N'Cái' AS Name
    UNION SELECT N'Bộ'
    UNION
    SELECT DISTINCT LTRIM(RTRIM(UnitOfMeasure)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(UnitOfMeasure)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(UnitOfMeasure)) AS Name
    FROM dbo.InsuranceAssets
    WHERE NULLIF(LTRIM(RTRIM(UnitOfMeasure)), N'') IS NOT NULL
), NumberedUnits AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM SeedUnits
)
INSERT INTO dbo.Units (UnitCode, UnitName)
SELECT 'DVT' + CASE WHEN nextId.UnitId < 10000
           THEN RIGHT('0000' + CONVERT(varchar(10), nextId.UnitId), 4)
           ELSE CONVERT(varchar(10), nextId.UnitId) END,
       LEFT(source.Name, 100)
FROM NumberedUnits AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(UnitId) FROM dbo.Units), 0) + source.RowNumber AS UnitId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Units AS item WHERE item.UnitName = source.Name);

;WITH SeedClassifications AS
(
    SELECT DISTINCT LTRIM(RTRIM(Classification)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(Classification)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(Classification)) AS Name
    FROM dbo.Insurances
    WHERE NULLIF(LTRIM(RTRIM(Classification)), N'') IS NOT NULL
), NumberedClassifications AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM SeedClassifications
)
INSERT INTO dbo.Classifications (ClassificationCode, ClassificationName)
SELECT 'PL' + CASE WHEN nextId.ClassificationId < 10000
           THEN RIGHT('0000' + CONVERT(varchar(10), nextId.ClassificationId), 4)
           ELSE CONVERT(varchar(10), nextId.ClassificationId) END,
       LEFT(source.Name, 150)
FROM NumberedClassifications AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(ClassificationId) FROM dbo.Classifications), 0) + source.RowNumber AS ClassificationId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Classifications AS item WHERE item.ClassificationName = source.Name);

IF EXISTS
(
    SELECT 1 FROM dbo.Devices WHERE LEN(LTRIM(RTRIM(Department))) > 150
    UNION ALL
    SELECT 1 FROM dbo.Insurances WHERE LEN(LTRIM(RTRIM(DepartmentProject))) > 150
)
BEGIN
    ;THROW 50008, 'A seeded department/project name exceeds 150 characters; shorten it before catalog synchronization.', 1;
END;

;WITH SeedDepartmentsProjects AS
(
    SELECT DISTINCT LTRIM(RTRIM(Department)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(Department)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(DepartmentProject)) AS Name
    FROM dbo.Insurances
    WHERE NULLIF(LTRIM(RTRIM(DepartmentProject)), N'') IS NOT NULL
), TypedOptions AS
(
    SELECT Name, CASE WHEN Name LIKE N'Dự án%' THEN 'PROJECT' ELSE 'DEPARTMENT' END AS EntityType
    FROM SeedDepartmentsProjects
), NumberedOptions AS
(
    SELECT Name, EntityType, ROW_NUMBER() OVER (ORDER BY EntityType, Name) AS RowNumber
    FROM TypedOptions
)
INSERT INTO dbo.DepartmentsProjects (Code, Name, EntityType)
SELECT CASE WHEN source.EntityType = 'PROJECT' THEN 'AD' ELSE 'BP' END
           + CASE WHEN nextId.DepartmentProjectId < 10000
               THEN RIGHT('0000' + CONVERT(varchar(10), nextId.DepartmentProjectId), 4)
               ELSE CONVERT(varchar(10), nextId.DepartmentProjectId) END,
       source.Name, source.EntityType
FROM NumberedOptions AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(DepartmentProjectId) FROM dbo.DepartmentsProjects), 0) + source.RowNumber AS DepartmentProjectId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.DepartmentsProjects AS item WHERE item.Name = source.Name);

;WITH SeedInsuranceTypes AS
(
    SELECT DISTINCT LTRIM(RTRIM(InsuranceType)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(InsuranceType)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(InsuranceType)) AS Name
    FROM dbo.Insurances
    WHERE NULLIF(LTRIM(RTRIM(InsuranceType)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(InsuranceType)) AS Name
    FROM dbo.InsuranceAssets
    WHERE NULLIF(LTRIM(RTRIM(InsuranceType)), N'') IS NOT NULL
), NumberedInsuranceTypes AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM SeedInsuranceTypes
)
INSERT INTO dbo.InsuranceTypes (InsuranceTypeCode, InsuranceTypeName)
SELECT 'BHT' + CASE WHEN nextId.InsuranceTypeId < 10000
           THEN RIGHT('0000' + CONVERT(varchar(10), nextId.InsuranceTypeId), 4)
           ELSE CONVERT(varchar(10), nextId.InsuranceTypeId) END,
       LEFT(source.Name, 200)
FROM NumberedInsuranceTypes AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(InsuranceTypeId) FROM dbo.InsuranceTypes), 0) + source.RowNumber AS InsuranceTypeId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.InsuranceTypes AS item WHERE item.InsuranceTypeName = source.Name);

;WITH SeedSuppliers AS
(
    SELECT DISTINCT LTRIM(RTRIM(Supplier)) AS Name
    FROM dbo.Devices
    WHERE NULLIF(LTRIM(RTRIM(Supplier)), N'') IS NOT NULL
    UNION
    SELECT DISTINCT LTRIM(RTRIM(Supplier)) AS Name
    FROM dbo.InsuranceAssets
    WHERE NULLIF(LTRIM(RTRIM(Supplier)), N'') IS NOT NULL
), NumberedSuppliers AS
(
    SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNumber
    FROM SeedSuppliers
)
INSERT INTO dbo.Suppliers (SupplierCode, SupplierName)
SELECT 'NCC' + CASE WHEN nextId.SupplierId < 10000
           THEN RIGHT('0000' + CONVERT(varchar(10), nextId.SupplierId), 4)
           ELSE CONVERT(varchar(10), nextId.SupplierId) END,
       LEFT(source.Name, 200)
FROM NumberedSuppliers AS source
CROSS APPLY (SELECT ISNULL((SELECT MAX(SupplierId) FROM dbo.Suppliers), 0) + source.RowNumber AS SupplierId) AS nextId
WHERE NOT EXISTS (SELECT 1 FROM dbo.Suppliers AS item WHERE item.SupplierName = source.Name);

/* Sample accident catalog entries; rerunning this script does not duplicate names or codes. */
INSERT INTO dbo.AccidentCause (AccidentCauseCode, AccidentCauseName, Description)
SELECT seed.Code, seed.Name, seed.Description
FROM (VALUES
    ('NC001', N'Lỗi vận hành', N'Thao tác hoặc vận hành thiết bị không đúng quy trình.'),
    ('NC002', N'Thời tiết', N'Ảnh hưởng do mưa, ngập, gió hoặc điều kiện thời tiết.'),
    ('NC003', N'Đang xác minh', N'Nguyên nhân đang được kiểm tra và xác minh.')
) AS seed(Code, Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AccidentCause AS item WHERE item.AccidentCauseName = seed.Name OR item.AccidentCauseCode = seed.Code);

INSERT INTO dbo.AccidentTypes (AccidentTypesCode, AccidentTypesName, Description)
SELECT seed.Code, seed.Name, seed.Description
FROM (VALUES
    ('LT001', N'Tai nạn tài sản', N'Sự cố gây thiệt hại tài sản hoặc thiết bị.'),
    ('LT002', N'Va chạm', N'Va chạm trong quá trình di chuyển hoặc vận chuyển.'),
    ('LT003', N'Tổn thất thiết bị', N'Thiết bị hư hỏng hoặc giảm khả năng hoạt động.')
) AS seed(Code, Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AccidentTypes AS item WHERE item.AccidentTypesName = seed.Name OR item.AccidentTypesCode = seed.Code);
GO
