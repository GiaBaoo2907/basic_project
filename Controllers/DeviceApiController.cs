using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;
using System.Data.Entity;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/deviceapi")]
    public class DeviceApiController : ApiController
    {
        [HttpGet, Route("")]
        public async Task<IHttpActionResult> Get()
        {
            using (var db = new ApplicationDbContext())
            {
                var devices = await db.Devices
                    .OrderBy(x => x.DeviceName)
                    .Select(x => new
                    {
                        DeviceId = x.Id,
                        x.MaterialCode,
                        x.DeviceName,
                        x.Year,
                        x.Classification,
                        x.InsuranceType,
                        x.Department,
                        x.PurchaseDate,
                        x.ExpiryDate,
                        x.Quantity,
                        x.UnitOfMeasure,
                        x.Value,
                        x.InsuranceFee,
                        x.Status,
                        x.Supplier
                    }).ToListAsync();
                return Ok(devices);
            }
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var device = await db.Devices
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        DeviceId = x.Id,
                        x.MaterialCode,
                        x.DeviceName,
                        x.Year,
                        x.Classification,
                        x.InsuranceType,
                        x.Department,
                        x.PurchaseDate,
                        x.ExpiryDate,
                        x.Quantity,
                        x.UnitOfMeasure,
                        x.Value,
                        x.InsuranceFee,
                        x.Status,
                        x.Supplier
                    }).FirstOrDefaultAsync();

                if (device == null) return NotFound();
                return Ok(device);
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] Device device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.MaterialCode))
                return BadRequest("Vui lòng chọn mã vật tư từ danh mục Quản trị.");
            if (!device.PurchaseDate.HasValue)
                return BadRequest("Ngày mua bảo hiểm cần được nhập trong danh mục thiết bị.");
            if (device.ExpiryDate.HasValue && device.ExpiryDate.Value.Date < device.PurchaseDate.Value.Date)
                return BadRequest("Ngày đến hạn không được trước ngày mua bảo hiểm.");
            if (device.Value < 0 || device.InsuranceFee < 0)
                return BadRequest("Giá trị và phí bảo hiểm không được âm.");

            device.MaterialCode = device.MaterialCode.Trim();
            device.InsuranceType = Clean(device.InsuranceType);
            device.Department = Clean(device.Department);
            device.Quantity = device.Quantity.GetValueOrDefault(1) < 1 ? 1 : device.Quantity;
            device.Status = Clean(device.Status) ?? "Đang sử dụng";
            device.Supplier = Clean(device.Supplier);

            using (var db = new ApplicationDbContext())
            {
                var catalogError = await ValidateCatalogSelectionsAsync(db, device.Classification, device.Department,
                    device.InsuranceType, device.Status, device.Supplier, device.UnitOfMeasure);
                if (catalogError != null) return BadRequest(catalogError);
                using (var transaction = db.Database.BeginTransaction())
                {
                    var material = await db.Materials.FirstOrDefaultAsync(item => item.MaterialCode == device.MaterialCode);
                    if (material == null)
                        return BadRequest("Mã vật tư không tồn tại trong danh mục Quản trị.");
                    device.MaterialCode = material.MaterialCode;
                    device.DeviceName = material.MaterialName;
                    device.Classification = Clean(device.Classification);
                    device.UnitOfMeasure = Clean(device.UnitOfMeasure);
                    
                    db.Devices.Add(device);
                    await db.SaveChangesAsync();
                    transaction.Commit();
                    return Ok(new
                    {
                        DeviceId = device.Id,
                        device.MaterialCode,
                        device.DeviceName,
                        device.Year,
                        device.Classification,
                        device.InsuranceType,
                        device.Department,
                        device.PurchaseDate,
                        device.ExpiryDate,
                        device.Quantity,
                        device.UnitOfMeasure,
                        device.Value,
                        device.InsuranceFee,
                        device.Status,
                        device.Supplier
                    });
                }
            }
        }

        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] Device updated)
        {
            if (updated == null || string.IsNullOrWhiteSpace(updated.MaterialCode))
                return BadRequest("Vui lòng chọn mã vật tư từ danh mục Quản trị.");
            if (!updated.PurchaseDate.HasValue)
                return BadRequest("Ngày mua bảo hiểm cần được nhập trong danh mục thiết bị.");
            if (updated.ExpiryDate.HasValue && updated.ExpiryDate.Value.Date < updated.PurchaseDate.Value.Date)
                return BadRequest("Ngày đến hạn không được trước ngày mua bảo hiểm.");
            if (updated.Value < 0 || updated.InsuranceFee < 0)
                return BadRequest("Giá trị và phí bảo hiểm không được âm.");

            using (var db = new ApplicationDbContext())
            {
                var device = await db.Devices.FindAsync(id);
                if (device == null) return NotFound();

                var catalogError = await ValidateCatalogSelectionsAsync(db, updated.Classification, updated.Department,
                    updated.InsuranceType, updated.Status, updated.Supplier, updated.UnitOfMeasure);
                if (catalogError != null) return BadRequest(catalogError);

                device.Year = updated.Year;
                device.InsuranceType = Clean(updated.InsuranceType);
                device.Department = Clean(updated.Department);
                device.PurchaseDate = updated.PurchaseDate;
                device.ExpiryDate = updated.ExpiryDate;
                device.Quantity = updated.Quantity.GetValueOrDefault(1) < 1 ? 1 : updated.Quantity;
                device.Value = updated.Value;
                device.InsuranceFee = updated.InsuranceFee;
                device.Status = Clean(updated.Status) ?? "Đang sử dụng";
                device.Supplier = Clean(updated.Supplier);
                device.Classification = Clean(updated.Classification);
                device.UnitOfMeasure = Clean(updated.UnitOfMeasure);

                var material = await db.Materials.FirstOrDefaultAsync(item => item.MaterialCode == updated.MaterialCode);
                if (material == null)
                    return BadRequest("Mã vật tư không tồn tại trong danh mục Quản trị.");
                device.MaterialCode = material.MaterialCode;
                
                device.DeviceName = material.MaterialName;
                await db.SaveChangesAsync();
                return Ok(new { DeviceId = device.Id, device.MaterialCode, device.DeviceName });
            }
        }

        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var device = await db.Devices.FindAsync(id);
                if (device == null) return NotFound();

                db.Devices.Remove(device);
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static async Task<string> ValidateCatalogSelectionsAsync(
            ApplicationDbContext db, string classification, string department, string insuranceType,
            string status, string supplier, string unitOfMeasure)
        {
            if (!string.IsNullOrWhiteSpace(classification) &&
                !await db.Classifications.AnyAsync(item => item.ClassificationName == classification.Trim()))
                return "Phân loại không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(department) &&
                !await db.DepartmentsProjects.AnyAsync(item => item.Name == department.Trim()))
                return "Bộ phận/dự án không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(insuranceType) &&
                !await db.InsuranceTypes.AnyAsync(item => item.InsuranceTypeName == insuranceType.Trim()))
                return "Loại bảo hiểm không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(status) &&
                !await db.Statuses.AnyAsync(item => item.StatusName == status.Trim()))
                return "Tình trạng không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(supplier) &&
                !await db.Suppliers.AnyAsync(item => item.SupplierName == supplier.Trim()))
                return "Nhà cung cấp không có trong danh mục Quản trị.";
            if (!string.IsNullOrWhiteSpace(unitOfMeasure) &&
                !await db.Units.AnyAsync(item => item.UnitName == unitOfMeasure.Trim()))
                return "Đơn vị tính không có trong danh mục Quản trị.";
            return null;
        }

    }
}
