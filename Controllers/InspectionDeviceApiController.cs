using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/inspectiondeviceapi")]
    public class InspectionDeviceApiController : ApiController
    {
        [HttpGet, Route("")]
        public async Task<IHttpActionResult> Get()
        {
            using (var db = new ApplicationDbContext())
                return Ok((await db.InspectionDevices.OrderBy(x => x.DeviceName).ToListAsync()).Select(Project));
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var device = await db.InspectionDevices.FindAsync(id);
                return device == null ? (IHttpActionResult)NotFound() : Ok(Project(device));
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] InspectionDevice input)
        {
            if (!Valid(input, out var error)) return BadRequest(error);
            using (var db = new ApplicationDbContext())
            {
                if (!await db.Materials.AnyAsync(x => x.MaterialCode == input.MaterialCode.Trim())) return BadRequest("Mã vật tư không tồn tại trong danh mục.");
                if (!await CatalogsValid(db, input)) return BadRequest("Phân loại, bộ phận/dự án, tình trạng, đơn vị tính hoặc nhà cung cấp không có trong danh mục.");
                var material = await db.Materials.FirstAsync(x => x.MaterialCode == input.MaterialCode.Trim());
                input.MaterialCode = material.MaterialCode;
                input.DeviceName = material.MaterialName;
                input.Classification = Clean(input.Classification);
                input.Department = Clean(input.Department);
                input.UnitOfMeasure = Clean(input.UnitOfMeasure);
                input.Status = Clean(input.Status) ?? "Đang sử dụng";
                input.Supplier = Clean(input.Supplier);
                input.Quantity = input.Quantity.GetValueOrDefault(1) < 1 ? 1 : input.Quantity;
                db.InspectionDevices.Add(input);
                await db.SaveChangesAsync();
                return Ok(Project(input));
            }
        }

        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] InspectionDevice input)
        {
            if (!Valid(input, out var error)) return BadRequest(error);
            using (var db = new ApplicationDbContext())
            {
                var entity = await db.InspectionDevices.FindAsync(id);
                if (entity == null) return NotFound();
                if (!await db.Materials.AnyAsync(x => x.MaterialCode == input.MaterialCode.Trim())) return BadRequest("Mã vật tư không tồn tại trong danh mục.");
                if (!await CatalogsValid(db, input)) return BadRequest("Phân loại, bộ phận/dự án, tình trạng, đơn vị tính hoặc nhà cung cấp không có trong danh mục.");
                var material = await db.Materials.FirstAsync(x => x.MaterialCode == input.MaterialCode.Trim());
                entity.MaterialCode = material.MaterialCode;
                entity.DeviceName = material.MaterialName;
                entity.Year = input.Year;
                entity.Classification = Clean(input.Classification);
                entity.Department = Clean(input.Department);
                entity.PurchaseDate = input.PurchaseDate;
                entity.ExpiryDate = input.ExpiryDate;
                entity.Quantity = input.Quantity.GetValueOrDefault(1) < 1 ? 1 : input.Quantity;
                entity.UnitOfMeasure = Clean(input.UnitOfMeasure);
                entity.Value = input.Value;
                entity.Status = Clean(input.Status) ?? "Đang sử dụng";
                entity.Supplier = Clean(input.Supplier);
                await db.SaveChangesAsync();
                return Ok(Project(entity));
            }
        }

        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var entity = await db.InspectionDevices.FindAsync(id);
                if (entity == null) return NotFound();
                
                db.InspectionDevices.Remove(entity);
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        private static bool Valid(InspectionDevice x, out string error)
        {
            error = null;
            if (x == null || string.IsNullOrWhiteSpace(x.MaterialCode)) error = "Vui lòng chọn vật tư trong danh mục.";
            else if (x.Value < 0 || (x.Quantity.HasValue && x.Quantity < 0)) error = "Giá trị và số lượng không được âm.";
            else if (x.ExpiryDate.HasValue && x.PurchaseDate.HasValue && x.ExpiryDate.Value.Date < x.PurchaseDate.Value.Date) error = "Ngày đến hạn không được trước ngày mua.";
            return error == null;
        }

        private static async Task<bool> CatalogsValid(ApplicationDbContext db, InspectionDevice x)
        {
            return (string.IsNullOrWhiteSpace(x.Classification) || await db.Classifications.AnyAsync(i => i.ClassificationName == x.Classification.Trim())) &&
                   (string.IsNullOrWhiteSpace(x.Department) || await db.DepartmentsProjects.AnyAsync(i => i.Name == x.Department.Trim())) &&
                   (string.IsNullOrWhiteSpace(x.Status) || await db.Statuses.AnyAsync(i => i.StatusName == x.Status.Trim())) &&
                   (string.IsNullOrWhiteSpace(x.UnitOfMeasure) || await db.Units.AnyAsync(i => i.UnitName == x.UnitOfMeasure.Trim())) &&
                   (string.IsNullOrWhiteSpace(x.Supplier) || await db.Suppliers.AnyAsync(i => i.SupplierName == x.Supplier.Trim()));
        }

        private static string Clean(string value) { return string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }

        private static object Project(InspectionDevice x)
        {
            return new { DeviceId = x.Id, x.MaterialCode, x.DeviceName, x.Year, x.Classification, x.Department,
                x.PurchaseDate, x.ExpiryDate, x.Quantity, x.UnitOfMeasure, x.Value, x.Status, x.Supplier };
        }
    }
}
