using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/catalogapi")]
    public class CatalogApiController : ApiController
    {
        private static readonly string[] CategoryKeys =
        {
            "statuses", "units", "classifications", "projects", "insurance-types", "suppliers", "materials", "inspection-types", "accidents-cause", "accidents-types"
        };

        [HttpGet, Route("{categoryKey}")]
        public async Task<IHttpActionResult> Get(string categoryKey)
        {
            if (!IsSupported(categoryKey)) return BadRequest("Danh mục không hợp lệ.");

            using (var db = new ApplicationDbContext())
            {
                List<CatalogItem> items;
                switch (categoryKey.ToLowerInvariant())
                {
                    case "statuses":
                        items = await db.Statuses.OrderBy(item => item.StatusName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.StatusId,
                                Name = item.StatusName,
                                Code = item.StatusCode,
                                Description = item.Description,
                                TextColor = item.TextColor,
                                BackgroundColor = item.BackgroundColor
                            }).ToListAsync();
                        break;
                    case "units":
                        items = await db.Units.OrderBy(item => item.UnitName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.UnitId,
                                Name = item.UnitName,
                                Code = item.UnitCode
                            })
                            .ToListAsync();
                        break;
                    case "classifications":
                        items = await db.Classifications.OrderBy(item => item.ClassificationName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.ClassificationId,
                                Name = item.ClassificationName,
                                Code = item.ClassificationCode,
                                Description = item.Description
                            })
                            .ToListAsync();
                        break;
                    case "projects":
                        items = await db.DepartmentsProjects.OrderBy(item => item.Name)
                            .Select(item => new CatalogItem
                            {
                                Id = item.DepartmentProjectId,
                                Name = item.Name,
                                Code = item.Code,
                                EntityType = item.EntityType,
                                Description = item.Description
                            })
                            .ToListAsync();
                        break;
                    case "insurance-types":
                        items = await db.InsuranceTypes.OrderBy(item => item.InsuranceTypeName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.InsuranceTypeId,
                                Name = item.InsuranceTypeName,
                                Code = item.InsuranceTypeCode,
                                Description = item.Description
                            }).ToListAsync();
                        break;
                    case "suppliers":
                        items = await db.Suppliers.OrderBy(item => item.SupplierName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.SupplierId,
                                Name = item.SupplierName,
                                Code = item.SupplierCode,
                                Description = item.Description
                            })
                            .ToListAsync();
                        break;
                    case "materials":
                        items = await db.Materials.OrderBy(item => item.MaterialName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.MaterialId,
                                Name = item.MaterialName,
                                Code = item.MaterialCode,
                                Classification = item.Classification,
                                UnitOfMeasure = item.UnitOfMeasure,
                                Quantity = item.Quantity
                            })
                            .ToListAsync();
                        break;
                    case "inspection-types":
                        items = await db.InspectionTypes.OrderBy(item => item.InspectionTypeName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.InspectionTypeId,
                                Name = item.InspectionTypeName,
                                Code = item.InspectionTypeCode,
                                Description = item.Description
                            })
                            .ToListAsync();
                        break;
                    case "accidents-cause":
                        items = await db.AccidentCauses.OrderBy(item => item.AccidentCauseName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.AccidentCauseId,
                                Name = item.AccidentCauseName,
                                Code = item.AccidentCauseCode,
                                Description = item.Description
                            }).ToListAsync();
                        break;
                    case "accidents-types":
                        items = await db.AccidentTypes.OrderBy(item => item.AccidentTypesName)
                            .Select(item => new CatalogItem
                            {
                                Id = item.AccidentTypesId,
                                Name = item.AccidentTypesName,
                                Code = item.AccidentTypesCode,
                                Description = item.Description
                            }).ToListAsync();
                        break;
                    default:
                        return BadRequest("Danh mục không hợp lệ.");
                }
                return Ok(items);
            }
        }

        [HttpPost, Route("{categoryKey}")]
        public async Task<IHttpActionResult> Create(string categoryKey, [FromBody] CatalogItem model)
        {
            if (!IsSupported(categoryKey)) return BadRequest("Danh mục không hợp lệ.");
            var validationError = Validate(categoryKey, model);
            if (validationError != null) return BadRequest(validationError);

            using (var db = new ApplicationDbContext())
            {
                var name = model.Name.Trim();
                var code = Clean(model.Code);
                if (await ExistsAsync(db, categoryKey, name, code, null))
                {
                    return Content(System.Net.HttpStatusCode.Conflict, "Tên hoặc mã đã tồn tại trong danh mục.");
                }

                switch (categoryKey.ToLowerInvariant())
                {
                    case "statuses":
                        var status = new StatusCatalogItem
                        {
                            StatusName = name,
                            StatusCode = code,
                            Description = Clean(model.Description),
                            TextColor = Clean(model.TextColor),
                            BackgroundColor = Clean(model.BackgroundColor)
                        };
                        db.Statuses.Add(status);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = status.StatusId,
                            Name = status.StatusName,
                            Code = status.StatusCode,
                            Description = status.Description,
                            TextColor = status.TextColor,
                            BackgroundColor = status.BackgroundColor
                        });
                    case "units":
                        var unit = new UnitCatalogItem
                        {
                            UnitName = name,
                            UnitCode = code
                        };
                        db.Units.Add(unit);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = unit.UnitId,
                            Name = unit.UnitName,
                            Code = unit.UnitCode
                        });
                    case "classifications":
                        var classification = new ClassificationCatalogItem
                        {
                            ClassificationName = name,
                            ClassificationCode = code,
                            Description = Clean(model.Description)
                        };
                        db.Classifications.Add(classification);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = classification.ClassificationId,
                            Name = classification.ClassificationName,
                            Code = classification.ClassificationCode,
                            Description = classification.Description
                        });
                    case "projects":
                        var project = new ProjectCatalogItem
                        {
                            Name = name,
                            Code = code,
                            EntityType = model.EntityType.Trim().ToUpperInvariant(),
                            Description = Clean(model.Description)
                        };
                        db.DepartmentsProjects.Add(project);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = project.DepartmentProjectId,
                            Name = project.Name,
                            Code = project.Code,
                            EntityType = project.EntityType,
                            Description = project.Description
                        });
                    case "insurance-types":
                        var insuranceType = new InsuranceTypeCatalogItem
                        {
                            InsuranceTypeName = name,
                            InsuranceTypeCode = code,
                            Description = Clean(model.Description)
                        };
                        db.InsuranceTypes.Add(insuranceType);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = insuranceType.InsuranceTypeId,
                            Name = insuranceType.InsuranceTypeName,
                            Code = insuranceType.InsuranceTypeCode,
                            Description = insuranceType.Description
                        });
                    case "suppliers":
                        var supplier = new SupplierCatalogItem
                        {
                            SupplierName = name,
                            SupplierCode = code,
                            Description = Clean(model.Description)
                        };
                        db.Suppliers.Add(supplier);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = supplier.SupplierId,
                            Name = supplier.SupplierName,
                            Code = supplier.SupplierCode,
                            Description = supplier.Description
                        });
                    case "materials":
                        var material = new MaterialCatalogItem
                        {
                            MaterialCode = "T" + Guid.NewGuid().ToString("N").Substring(0, 19),
                            MaterialName = name,
                            Classification = Clean(model.Classification),
                            UnitOfMeasure = Clean(model.UnitOfMeasure),
                            Quantity = model.Quantity.GetValueOrDefault(1)
                        };
                        db.Materials.Add(material);
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            await db.SaveChangesAsync();
                            material.MaterialCode = FormatMaterialCode(material.MaterialId);
                            await db.SaveChangesAsync();
                            transaction.Commit();
                        }
                        return Ok(ToCatalogItem(material));
                    case "inspection-types":
                        var inspectionType = new InspectionTypeCatalogItem
                        {
                            InspectionTypeName = name,
                            InspectionTypeCode = code,
                            Description = Clean(model.Description)
                        };
                        db.InspectionTypes.Add(inspectionType);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = inspectionType.InspectionTypeId,
                            Name = inspectionType.InspectionTypeName,
                            Code = inspectionType.InspectionTypeCode,
                            Description = inspectionType.Description
                        });
                    case "accidents-cause":
                        var accidentCause = new AccidentCauseCatalogItem
                        {
                            AccidentCauseName = name,
                            AccidentCauseCode = code,
                            Description = Clean(model.Description)
                        };
                        db.AccidentCauses.Add(accidentCause);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem { Id = accidentCause.AccidentCauseId, Name = accidentCause.AccidentCauseName, Code = accidentCause.AccidentCauseCode, Description = accidentCause.Description });
                    case "accidents-types":
                        var accidentType = new AccidentTypeCatalogItem
                        {
                            AccidentTypesName = name,
                            AccidentTypesCode = code,
                            Description = Clean(model.Description)
                        };
                        db.AccidentTypes.Add(accidentType);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem { Id = accidentType.AccidentTypesId, Name = accidentType.AccidentTypesName, Code = accidentType.AccidentTypesCode, Description = accidentType.Description });
                    default:
                        return BadRequest("Danh mục không hợp lệ.");
                }
            }
        }

        [HttpPut, Route("{categoryKey}/{id:int}")]
        public async Task<IHttpActionResult> Update(string categoryKey, int id, [FromBody] CatalogItem model)
        {
            if (!IsSupported(categoryKey)) return BadRequest("Danh mục không hợp lệ.");
            var validationError = Validate(categoryKey, model);
            if (validationError != null) return BadRequest(validationError);

            using (var db = new ApplicationDbContext())
            {
                var name = model.Name.Trim();
                var code = Clean(model.Code);
                if (await ExistsAsync(db, categoryKey, name, code, id))
                {
                    return Content(System.Net.HttpStatusCode.Conflict, "Tên hoặc mã đã tồn tại trong danh mục.");
                }

                switch (categoryKey.ToLowerInvariant())
                {
                    case "statuses":
                        var status = await db.Statuses.FindAsync(id);
                        if (status == null) return NotFound();
                        var oldStatusName = status.StatusName;
                        status.StatusName = name;
                        status.StatusCode = code;
                        status.Description = Clean(model.Description);
                        status.TextColor = Clean(model.TextColor);
                        status.BackgroundColor = Clean(model.BackgroundColor);
                        foreach (var item in await db.InspectionDevices.Where(x => x.Status == oldStatusName).ToListAsync()) item.Status = status.StatusName;
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = status.StatusId,
                            Name = status.StatusName,
                            Code = status.StatusCode,
                            Description = status.Description,
                            TextColor = status.TextColor,
                            BackgroundColor = status.BackgroundColor
                        });
                    case "units":
                        var unit = await db.Units.FindAsync(id);
                        if (unit == null) return NotFound();
                        var oldUnitName = unit.UnitName;
                        unit.UnitName = name;
                        unit.UnitCode = code;
                        foreach (var item in await db.InspectionDevices.Where(x => x.UnitOfMeasure == oldUnitName).ToListAsync()) item.UnitOfMeasure = unit.UnitName;
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = unit.UnitId,
                            Name = unit.UnitName,
                            Code = unit.UnitCode
                        });
                    case "classifications":
                        var classification = await db.Classifications.FindAsync(id);
                        if (classification == null) return NotFound();
                        var oldClassificationName = classification.ClassificationName;
                        classification.ClassificationName = name;
                        classification.ClassificationCode = code;
                        classification.Description = Clean(model.Description);
                        foreach (var item in await db.InspectionDevices.Where(x => x.Classification == oldClassificationName).ToListAsync()) item.Classification = classification.ClassificationName;
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = classification.ClassificationId,
                            Name = classification.ClassificationName,
                            Code = classification.ClassificationCode,
                            Description = classification.Description
                        });
                    case "projects":
                        var project = await db.DepartmentsProjects.FindAsync(id);
                        if (project == null) return NotFound();
                        var oldProjectName = project.Name;
                        project.Name = name;
                        project.Code = code;
                        project.EntityType = model.EntityType.Trim().ToUpperInvariant();
                        project.Description = Clean(model.Description);
                        foreach (var item in await db.InspectionDevices.Where(x => x.Department == oldProjectName).ToListAsync()) item.Department = project.Name;
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = project.DepartmentProjectId,
                            Name = project.Name,
                            Code = project.Code,
                            EntityType = project.EntityType,
                            Description = project.Description
                        });
                    case "insurance-types":
                        var insuranceType = await db.InsuranceTypes.FindAsync(id);
                        if (insuranceType == null) return NotFound();
                        insuranceType.InsuranceTypeName = name;
                        insuranceType.InsuranceTypeCode = code;
                        insuranceType.Description = Clean(model.Description);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = insuranceType.InsuranceTypeId,
                            Name = insuranceType.InsuranceTypeName,
                            Code = insuranceType.InsuranceTypeCode,
                            Description = insuranceType.Description
                        });
                    case "suppliers":
                        var supplier = await db.Suppliers.FindAsync(id);
                        if (supplier == null) return NotFound();
                        var oldSupplierName = supplier.SupplierName;
                        supplier.SupplierName = name;
                        supplier.SupplierCode = code;
                        supplier.Description = Clean(model.Description);
                        foreach (var item in await db.InspectionDevices.Where(x => x.Supplier == oldSupplierName).ToListAsync()) item.Supplier = supplier.SupplierName;
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = supplier.SupplierId,
                            Name = supplier.SupplierName,
                            Code = supplier.SupplierCode,
                            Description = supplier.Description
                        });
                    case "materials":
                        var material = await db.Materials.FindAsync(id);
                        if (material == null) return NotFound();
                        material.MaterialName = name;
                        material.Classification = Clean(model.Classification);
                        material.UnitOfMeasure = Clean(model.UnitOfMeasure);
                        material.Quantity = model.Quantity.GetValueOrDefault(1);
                        var devices = await db.Devices.Where(item => item.MaterialCode == material.MaterialCode).ToListAsync();
                        foreach (var device in devices)
                        {
                            
                            device.DeviceName = material.MaterialName;
                            device.Classification = material.Classification;
                            device.UnitOfMeasure = material.UnitOfMeasure;
                        }
                        var inspectionDevices = await db.InspectionDevices.Where(item => item.MaterialCode == material.MaterialCode).ToListAsync();
                        foreach (var device in inspectionDevices)
                        {
                            device.DeviceName = material.MaterialName;
                            device.Classification = material.Classification;
                            device.UnitOfMeasure = material.UnitOfMeasure;
                        }
                        var requestAssets = await db.InsuranceAssets.Where(item => item.MaterialCode == material.MaterialCode).ToListAsync();
                        foreach (var asset in requestAssets)
                        {
                            asset.Equipment = material.MaterialName;
                            asset.UnitOfMeasure = material.UnitOfMeasure;
                        }
                        var inspectionAssets = await db.InspectionAssets.Where(item => item.MaterialCode == material.MaterialCode).ToListAsync();
                        foreach (var asset in inspectionAssets)
                        {
                            asset.DeviceName = material.MaterialName;
                            asset.Classification = material.Classification;
                            asset.UnitOfMeasure = material.UnitOfMeasure;
                        }
                        await db.SaveChangesAsync();
                        return Ok(ToCatalogItem(material));
                    case "inspection-types":
                        var inspectionType = await db.InspectionTypes.FindAsync(id);
                        if (inspectionType == null) return NotFound();
                        inspectionType.InspectionTypeName = name;
                        inspectionType.InspectionTypeCode = code;
                        inspectionType.Description = Clean(model.Description);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem
                        {
                            Id = inspectionType.InspectionTypeId,
                            Name = inspectionType.InspectionTypeName,
                            Code = inspectionType.InspectionTypeCode,
                            Description = inspectionType.Description
                        });
                    case "accidents-cause":
                        var accidentCause = await db.AccidentCauses.FindAsync(id);
                        if (accidentCause == null) return NotFound();
                        accidentCause.AccidentCauseName = name;
                        accidentCause.AccidentCauseCode = code;
                        accidentCause.Description = Clean(model.Description);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem { Id = accidentCause.AccidentCauseId, Name = accidentCause.AccidentCauseName, Code = accidentCause.AccidentCauseCode, Description = accidentCause.Description });
                    case "accidents-types":
                        var accidentType = await db.AccidentTypes.FindAsync(id);
                        if (accidentType == null) return NotFound();
                        accidentType.AccidentTypesName = name;
                        accidentType.AccidentTypesCode = code;
                        accidentType.Description = Clean(model.Description);
                        await db.SaveChangesAsync();
                        return Ok(new CatalogItem { Id = accidentType.AccidentTypesId, Name = accidentType.AccidentTypesName, Code = accidentType.AccidentTypesCode, Description = accidentType.Description });
                    default:
                        return BadRequest("Danh mục không hợp lệ.");
                }
            }
        }

        [HttpDelete, Route("{categoryKey}/{id:int}")]
        public async Task<IHttpActionResult> Delete(string categoryKey, int id)
        {
            if (!IsSupported(categoryKey)) return BadRequest("Danh mục không hợp lệ.");

            using (var db = new ApplicationDbContext())
            {
                switch (categoryKey.ToLowerInvariant())
                {
                    case "statuses":
                        var status = await db.Statuses.FindAsync(id);
                        if (status == null) return NotFound();
                        if (await db.InspectionDevices.AnyAsync(item => item.Status == status.StatusName)) return BadRequest("Tình trạng đang được thiết bị kiểm định sử dụng.");
                        db.Statuses.Remove(status);
                        break;
                    case "units":
                        var unit = await db.Units.FindAsync(id);
                        if (unit == null) return NotFound();
                        if (await db.InspectionDevices.AnyAsync(item => item.UnitOfMeasure == unit.UnitName)) return BadRequest("Đơn vị tính đang được thiết bị kiểm định sử dụng.");
                        db.Units.Remove(unit);
                        break;
                    case "classifications":
                        var classification = await db.Classifications.FindAsync(id);
                        if (classification == null) return NotFound();
                        if (await db.InspectionDevices.AnyAsync(item => item.Classification == classification.ClassificationName)) return BadRequest("Phân loại đang được thiết bị kiểm định sử dụng.");
                        db.Classifications.Remove(classification);
                        break;
                    case "projects":
                        var project = await db.DepartmentsProjects.FindAsync(id);
                        if (project == null) return NotFound();
                        if (await db.InspectionDevices.AnyAsync(item => item.Department == project.Name)) return BadRequest("Bộ phận/dự án đang được thiết bị kiểm định sử dụng.");
                        db.DepartmentsProjects.Remove(project);
                        break;
                    case "insurance-types":
                        var insuranceType = await db.InsuranceTypes.FindAsync(id);
                        if (insuranceType == null) return NotFound();
                        db.InsuranceTypes.Remove(insuranceType);
                        break;
                    case "suppliers":
                        var supplier = await db.Suppliers.FindAsync(id);
                        if (supplier == null) return NotFound();
                        if (await db.InspectionDevices.AnyAsync(item => item.Supplier == supplier.SupplierName)) return BadRequest("Nhà cung cấp đang được thiết bị kiểm định sử dụng.");
                        db.Suppliers.Remove(supplier);
                        break;
                    case "materials":
                        var material = await db.Materials.FindAsync(id);
                        if (material == null) return NotFound();
                        if (await db.Devices.AnyAsync(item => item.MaterialCode == material.MaterialCode) ||
                            await db.InspectionDevices.AnyAsync(item => item.MaterialCode == material.MaterialCode) ||
                            await db.InsuranceAssets.AnyAsync(item => item.MaterialCode == material.MaterialCode) ||
                            await db.InspectionAssets.AnyAsync(item => item.MaterialCode == material.MaterialCode))
                            return BadRequest("Vật tư đang được sử dụng trong thiết bị hoặc phiếu; không thể xóa.");
                        db.Materials.Remove(material);
                        break;
                    case "inspection-types":
                        var inspectionType = await db.InspectionTypes.FindAsync(id);
                        if (inspectionType == null) return NotFound();
                        
                        db.InspectionTypes.Remove(inspectionType);
                        break;
                    case "accidents-cause":
                        var accidentCause = await db.AccidentCauses.FindAsync(id);
                        if (accidentCause == null) return NotFound();
                        if (await db.Accidents.AnyAsync(item => !item.IsDeleted && item.AccidentCause == accidentCause.AccidentCauseName))
                            return BadRequest("Nguyên nhân đang được phiếu tai nạn/sự cố sử dụng.");
                        db.AccidentCauses.Remove(accidentCause);
                        break;
                    case "accidents-types":
                        var accidentType = await db.AccidentTypes.FindAsync(id);
                        if (accidentType == null) return NotFound();
                        if (await db.Accidents.AnyAsync(item => !item.IsDeleted && item.AccidentType == accidentType.AccidentTypesName))
                            return BadRequest("Loại tai nạn/sự cố đang được phiếu sử dụng.");
                        db.AccidentTypes.Remove(accidentType);
                        break;
                    default:
                        return BadRequest("Danh mục không hợp lệ.");
                }
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        private static async Task<bool> ExistsAsync(
            ApplicationDbContext db, string categoryKey, string name, string code, int? excludedId)
        {
            switch (categoryKey.ToLowerInvariant())
            {
                case "statuses":
                    return await db.Statuses.AnyAsync(item =>
                        item.StatusId != (excludedId ?? 0) &&
                        (item.StatusName == name || (code != null && item.StatusCode == code)));
                case "units":
                    return await db.Units.AnyAsync(item =>
                        item.UnitId != (excludedId ?? 0) &&
                        (item.UnitName == name || (code != null && item.UnitCode == code)));
                case "classifications":
                    return await db.Classifications.AnyAsync(item =>
                        item.ClassificationId != (excludedId ?? 0) &&
                        (item.ClassificationName == name || (code != null && item.ClassificationCode == code)));
                case "projects":
                    return await db.DepartmentsProjects.AnyAsync(item =>
                        item.DepartmentProjectId != (excludedId ?? 0) &&
                        (item.Name == name || (code != null && item.Code == code)));
                case "insurance-types":
                    return await db.InsuranceTypes.AnyAsync(item =>
                        item.InsuranceTypeId != (excludedId ?? 0) &&
                        (item.InsuranceTypeName == name || (code != null && item.InsuranceTypeCode == code)));
                case "suppliers":
                    return await db.Suppliers.AnyAsync(item =>
                        item.SupplierId != (excludedId ?? 0) &&
                        (item.SupplierName == name || (code != null && item.SupplierCode == code)));
                case "materials":
                    return await db.Materials.AnyAsync(item =>
                        item.MaterialId != (excludedId ?? 0) &&
                        (item.MaterialName == name || (code != null && item.MaterialCode == code)));
                case "accidents-cause":
                    return await db.AccidentCauses.AnyAsync(item => item.AccidentCauseId != (excludedId ?? 0) &&
                        (item.AccidentCauseName == name || (code != null && item.AccidentCauseCode == code)));
                case "accidents-types":
                    return await db.AccidentTypes.AnyAsync(item => item.AccidentTypesId != (excludedId ?? 0) &&
                        (item.AccidentTypesName == name || (code != null && item.AccidentTypesCode == code)));
                default:
                    return false;
            }
        }

        private static bool IsSupported(string categoryKey)
        {
            return CategoryKeys.Contains(categoryKey, StringComparer.OrdinalIgnoreCase);
        }

        private static string Validate(string categoryKey, CatalogItem model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return "Tên danh mục là bắt buộc.";
            var key = categoryKey.ToLowerInvariant();
            var nameLimit = key == "statuses" || key == "units"
                ? 100
                : key == "classifications" || key == "projects" ? 150
                : key == "insurance-types" || key == "suppliers" || key == "accidents-cause" || key == "accidents-types" ? 200 : 250;
            if (model.Name.Trim().Length > nameLimit)
                return "Tên danh mục vượt quá giới hạn ký tự.";
            if (!string.IsNullOrWhiteSpace(model.Description) && model.Description.Trim().Length > 255)
                return "Mô tả không được vượt quá 255 ký tự.";

            var codeLimit = key == "projects" ? 30 : 20;
            if (key != "materials" &&
                (string.IsNullOrWhiteSpace(model.Code) || model.Code.Trim().Length > codeLimit))
                return "Mã danh mục là bắt buộc và không được vượt quá " + codeLimit + " ký tự.";

            if (key == "projects" &&
                !string.Equals(model.EntityType, "DEPARTMENT", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(model.EntityType, "PROJECT", StringComparison.OrdinalIgnoreCase))
                return "Loại đối tượng phải là DEPARTMENT hoặc PROJECT.";
            if (key == "materials")
            {
                if (!string.IsNullOrWhiteSpace(model.Classification) && model.Classification.Trim().Length > 100)
                    return "Phân loại không được vượt quá 100 ký tự.";
                if (!string.IsNullOrWhiteSpace(model.UnitOfMeasure) && model.UnitOfMeasure.Trim().Length > 50)
                    return "Đơn vị tính không được vượt quá 50 ký tự.";
                if (model.Quantity.GetValueOrDefault(1) < 1)
                    return "Số lượng phải lớn hơn 0.";
            }

            if (key == "statuses")
            {
                if (!IsValidColor(model.TextColor))
                    return "Mã màu chữ phải có định dạng #RRGGBB.";
                if (!IsValidColor(model.BackgroundColor))
                    return "Mã màu nền phải có định dạng #RRGGBB.";
            }

            return null;
        }

        private static bool IsValidColor(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                Regex.IsMatch(value.Trim(), "^#[0-9a-fA-F]{6}$");
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string FormatMaterialCode(int id)
        {
            return "VT" + id.ToString("D4");
        }

        private static CatalogItem ToCatalogItem(MaterialCatalogItem material)
        {
            return new CatalogItem
            {
                Id = material.MaterialId,
                Name = material.MaterialName,
                Code = material.MaterialCode,
                Classification = material.Classification,
                UnitOfMeasure = material.UnitOfMeasure,
                Quantity = material.Quantity
            };
        }
    }
}
