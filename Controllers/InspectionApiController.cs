using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/inspectionapi")]
    public class InspectionApiController : ApiController
    {
        [HttpGet, Route("")]
        public async Task<IHttpActionResult> Get()
        {
            using (var db = new ApplicationDbContext())
            {
                var requests = await db.InspectionRequests
                    .Where(request => !request.IsDeleted)
                    .OrderByDescending(request => request.InspectionDate)
                    .ThenByDescending(request => request.Id)
                    .ToListAsync();
                var codes = requests.Select(request => request.Code).ToArray();
                var assets = await db.InspectionAssets
                    .Where(asset => codes.Contains(asset.InspectionRequestCode))
                    .OrderBy(asset => asset.InspectionAssetId)
                    .ToListAsync();
                return Ok(requests.Select(request => ToForm(request, assets.Where(asset => asset.InspectionRequestCode == request.Code))));
            }
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var request = await db.InspectionRequests.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
                if (request == null) return NotFound();
                var assets = await db.InspectionAssets
                    .Where(asset => asset.InspectionRequestCode == request.Code)
                    .OrderBy(asset => asset.InspectionAssetId)
                    .ToListAsync();
                return Ok(ToForm(request, assets));
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] InspectionRequestForm form)
        {
            var validationError = Validate(form);
            if (validationError != null) return BadRequest(validationError);

            using (var db = new ApplicationDbContext())
            using (var transaction = db.Database.BeginTransaction())
            {
                var catalogError = await ValidateCatalogSelectionsAsync(db, form);
                if (catalogError != null) return BadRequest(catalogError);
                var equipment = await LoadEquipmentAsync(db, form.DeviceIds, form.Equipment);
                if (equipment == null) return BadRequest("Một hoặc nhiều thiết bị không còn tồn tại.");

                var request = new InspectionRequest
                {
                    Code = "TMP" + Guid.NewGuid().ToString("N").Substring(0, 20),
                    Title = form.Title.Trim(),
                    Creator = form.Creator.Trim(),
                    DepartmentProject = form.DepartmentProject.Trim(),
                    Classification = Clean(form.Classification),
                    InsuranceType = Clean(form.InsuranceType),
                    InspectionDate = form.InspectionDate.Value.Date,
                    Notes = Clean(form.Notes),
                    CreatedAt = DateTime.Now
                };
                db.InspectionRequests.Add(request);
                await db.SaveChangesAsync();
                request.Code = FormatCode(request.Id);
                await db.SaveChangesAsync();
                var inspectionAssets = equipment.Select(item => ToAsset(request.Code, item)).ToList();
                db.InspectionAssets.AddRange(inspectionAssets);
                await db.SaveChangesAsync();
                transaction.Commit();

                return CreatedAtRoute("DefaultApi", new { controller = "inspectionapi", id = request.Id }, ToForm(request, inspectionAssets));
            }
        }

        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] InspectionRequestForm form)
        {
            if (form == null || form.Id != id) return BadRequest("Id yêu cầu kiểm định không hợp lệ.");
            var validationError = Validate(form);
            if (validationError != null) return BadRequest(validationError);

            using (var db = new ApplicationDbContext())
            using (var transaction = db.Database.BeginTransaction())
            {
                var catalogError = await ValidateCatalogSelectionsAsync(db, form);
                if (catalogError != null) return BadRequest(catalogError);
                var request = await db.InspectionRequests.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
                if (request == null) return NotFound();

                var equipment = await LoadEquipmentAsync(db, form.DeviceIds, form.Equipment);
                if (equipment == null) return BadRequest("Một hoặc nhiều thiết bị không còn tồn tại.");

                request.Title = form.Title.Trim();
                request.Creator = form.Creator.Trim();
                request.DepartmentProject = form.DepartmentProject.Trim();
                request.Classification = Clean(form.Classification);
                request.InsuranceType = Clean(form.InsuranceType);
                request.InspectionDate = form.InspectionDate.Value.Date;
                request.Notes = Clean(form.Notes);
                var existingAssets = await db.InspectionAssets
                    .Where(asset => asset.InspectionRequestCode == request.Code)
                    .ToListAsync();
                var requestedIds = equipment.Select(item => item.DeviceId).ToArray();
                var removedAssets = existingAssets.Where(asset => !requestedIds.Contains(asset.DeviceId)).ToList();
                if (removedAssets.Count > 0)
                {
                    var removedAssetIds = removedAssets.Select(asset => asset.InspectionAssetId).ToArray();
                    var linkedFiles = await db.InspectionAttachments.Where(file => file.InspectionAssetId.HasValue && removedAssetIds.Contains(file.InspectionAssetId.Value)).ToListAsync();
                    foreach (var file in linkedFiles) file.InspectionAssetId = null;
                    db.InspectionAssets.RemoveRange(removedAssets);
                }
                foreach (var snapshot in equipment)
                {
                    var saved = existingAssets.FirstOrDefault(asset => asset.DeviceId == snapshot.DeviceId);
                    if (saved == null) db.InspectionAssets.Add(ToAsset(request.Code, snapshot));
                    else CopySnapshot(saved, snapshot);
                }
                await db.SaveChangesAsync();
                var inspectionAssets = await db.InspectionAssets.Where(asset => asset.InspectionRequestCode == request.Code).OrderBy(asset => asset.InspectionAssetId).ToListAsync();
                transaction.Commit();
                return Ok(ToForm(request, inspectionAssets));
            }
        }

        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var request = await db.InspectionRequests.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
                if (request == null) return NotFound();
                request.IsDeleted = true;
                request.DeletedAt = System.DateTime.UtcNow;
                request.DeletedBy = "web";
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        private static string Validate(InspectionRequestForm form)
        {
            if (form == null || string.IsNullOrWhiteSpace(form.Title) ||
                string.IsNullOrWhiteSpace(form.Creator) ||
                string.IsNullOrWhiteSpace(form.DepartmentProject) ||
                !form.InspectionDate.HasValue)
                return "Vui lòng nhập tiêu đề, người lập, bộ phận/dự án và ngày kiểm định.";
            if (form.Title.Trim().Length > 500 || form.Creator.Trim().Length > 200 ||
                form.DepartmentProject.Trim().Length > 200 ||
                (!string.IsNullOrWhiteSpace(form.Classification) && form.Classification.Trim().Length > 150) ||
                (!string.IsNullOrWhiteSpace(form.InsuranceType) && form.InsuranceType.Trim().Length > 200) ||
                (!string.IsNullOrWhiteSpace(form.Notes) && form.Notes.Trim().Length > 2000))
                return "Một hoặc nhiều thông tin vượt quá giới hạn ký tự.";
            if (form.DeviceIds == null || form.DeviceIds.Count == 0)
                return "Vui lòng chọn ít nhất một thiết bị cần kiểm định.";
            if (form.DeviceIds.Any(id => id <= 0) || form.DeviceIds.Distinct().Count() != form.DeviceIds.Count)
                return "Danh sách thiết bị kiểm định không hợp lệ hoặc có thiết bị bị trùng.";
            if (form.Equipment != null && form.Equipment.Any(item => item.InsuranceFee < 0))
                return "Phí kiểm định không được âm.";
            return null;
        }

        private static async Task<string> ValidateCatalogSelectionsAsync(ApplicationDbContext db, InspectionRequestForm form)
        {
            if (!await db.DepartmentsProjects.AnyAsync(item => item.Name == form.DepartmentProject.Trim()))
                return "Bộ phận/dự án không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(form.Classification) &&
                !await db.Classifications.AnyAsync(item => item.ClassificationName == form.Classification.Trim()))
                return "Phân loại không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(form.InsuranceType) &&
                !await db.InsuranceTypes.AnyAsync(item => item.InsuranceTypeName == form.InsuranceType.Trim()))
                return "Loại bảo hiểm không có trong danh mục Quản trị.";
            var inspectionTypes = (form.Equipment ?? new System.Collections.Generic.List<InspectionEquipmentSnapshot>())
                .Select(item => Clean(item.InspectionType))
                .Where(name => name != null)
                .Distinct()
                .ToArray();
            foreach (var inspectionType in inspectionTypes)
                if (!await db.InspectionTypes.AnyAsync(item => item.InspectionTypeName == inspectionType))
                    return "Loại kiểm định không có trong danh mục Quản trị.";
            return null;
        }

        private static async Task<InspectionEquipmentSnapshot[]> LoadEquipmentAsync(
            ApplicationDbContext db, System.Collections.Generic.IList<int> deviceIds,
            System.Collections.Generic.IList<InspectionEquipmentSnapshot> submittedEquipment)
        {
            var ids = deviceIds.ToArray();
            var devices = await db.InspectionDevices
                .Where(device => ids.Contains(device.Id))
                .Select(device => new InspectionEquipmentSnapshot
                {
                    DeviceId = device.Id,
                    MaterialCode = device.MaterialCode,
                    DeviceName = device.DeviceName,
                    Year = device.Year,
                    Classification = device.Classification,
                    Department = device.Department,
                    PurchaseDate = device.PurchaseDate,
                    ExpiryDate = device.ExpiryDate,
                    Quantity = device.Quantity,
                    UnitOfMeasure = device.UnitOfMeasure,
                    Value = device.Value,
                    Status = device.Status,
                    Supplier = device.Supplier
                })
                .ToListAsync();

            if (devices.Count != ids.Length) return null;
            var feesByDeviceId = (submittedEquipment ?? new System.Collections.Generic.List<InspectionEquipmentSnapshot>())
                .GroupBy(item => item.DeviceId)
                .ToDictionary(group => group.Key, group => group.First().InsuranceFee);
            foreach (var device in devices)
            {
                device.InsuranceFee = feesByDeviceId.TryGetValue(device.DeviceId, out var fee) ? fee : 0;
                device.InspectionType = (submittedEquipment ?? new System.Collections.Generic.List<InspectionEquipmentSnapshot>())
                    .Where(item => item.DeviceId == device.DeviceId)
                    .Select(item => Clean(item.InspectionType))
                    .FirstOrDefault();
            }
            return ids.Select(id => devices.Single(device => device.DeviceId == id)).ToArray();
        }

        private static InspectionAsset ToAsset(string inspectionRequestCode, InspectionEquipmentSnapshot device)
        {
            return new InspectionAsset
            {
                InspectionRequestCode = inspectionRequestCode,
                MaterialCode = device.MaterialCode,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                Year = device.Year,
                Classification = device.Classification,
                Department = device.Department,
                PurchaseDate = device.PurchaseDate,
                ExpiryDate = device.ExpiryDate,
                Quantity = device.Quantity,
                UnitOfMeasure = device.UnitOfMeasure,
                Value = device.Value,
                InsuranceFee = device.InsuranceFee,
                InspectionType = Clean(device.InspectionType),
                Status = device.Status,
                Supplier = device.Supplier
            };
        }

        private static void CopySnapshot(InspectionAsset asset, InspectionEquipmentSnapshot device)
        {
            asset.MaterialCode = device.MaterialCode;
            asset.DeviceName = device.DeviceName;
            asset.Year = device.Year;
            asset.Classification = device.Classification;
            asset.Department = device.Department;
            asset.PurchaseDate = device.PurchaseDate;
            asset.ExpiryDate = device.ExpiryDate;
            asset.Quantity = device.Quantity;
            asset.UnitOfMeasure = device.UnitOfMeasure;
            asset.Value = device.Value;
            asset.InsuranceFee = device.InsuranceFee;
            asset.InspectionType = Clean(device.InspectionType);
            asset.Status = device.Status;
            asset.Supplier = device.Supplier;
        }

        private static InspectionRequestForm ToForm(InspectionRequest request, System.Collections.Generic.IEnumerable<InspectionAsset> assets)
        {
            return new InspectionRequestForm
            {
                Id = request.Id,
                Code = request.Code,
                Title = request.Title,
                Creator = request.Creator,
                DepartmentProject = request.DepartmentProject,
                Classification = request.Classification,
                InsuranceType = request.InsuranceType,
                InspectionDate = request.InspectionDate,
                Notes = request.Notes,
                Equipment = assets.Select(asset => new InspectionEquipmentSnapshot
                {
                    InspectionAssetId = asset.InspectionAssetId,
                    DeviceId = asset.DeviceId,
                    MaterialCode = asset.MaterialCode,
                    DeviceName = asset.DeviceName,
                    Year = asset.Year,
                    Classification = asset.Classification,
                    Department = asset.Department,
                    PurchaseDate = asset.PurchaseDate,
                    ExpiryDate = asset.ExpiryDate,
                    Quantity = asset.Quantity,
                    UnitOfMeasure = asset.UnitOfMeasure,
                    Value = asset.Value,
                    InsuranceFee = asset.InsuranceFee,
                    InspectionType = asset.InspectionType,
                    Status = asset.Status,
                    Supplier = asset.Supplier
                }).ToList(),
                DeviceIds = assets.Select(item => item.DeviceId).ToList()
            };
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string FormatCode(int id)
        {
            return "KD" + DateTime.Now.ToString("yyyy") + "-" + id.ToString("D5");
        }
    }
}
