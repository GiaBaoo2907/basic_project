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
                        x.DeviceName,
                        x.Year,
                        x.Classification,
                        x.InsuranceType,
                        x.Department,
                        x.PurchaseDate,
                        x.ExpiryDate,
                        x.Quantity,
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
                        x.DeviceName,
                        x.Year,
                        x.Classification,
                        x.InsuranceType,
                        x.Department,
                        x.PurchaseDate,
                        x.ExpiryDate,
                        x.Quantity,
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
            if (device == null || string.IsNullOrWhiteSpace(device.DeviceName))
                return BadRequest("Tên thiết bị là bắt buộc.");
            if (!device.PurchaseDate.HasValue)
                return BadRequest("Ngày mua bảo hiểm cần được nhập trong danh mục thiết bị.");
            if (device.ExpiryDate.HasValue && device.ExpiryDate.Value.Date < device.PurchaseDate.Value.Date)
                return BadRequest("Ngày đến hạn không được trước ngày mua bảo hiểm.");
            if (device.Value < 0 || device.InsuranceFee < 0)
                return BadRequest("Giá trị và phí bảo hiểm không được âm.");

            device.DeviceName = device.DeviceName.Trim();
            device.Classification = Clean(device.Classification);
            device.InsuranceType = Clean(device.InsuranceType);
            device.Department = Clean(device.Department);
            device.Quantity = device.Quantity.GetValueOrDefault(1) < 1 ? 1 : device.Quantity;
            device.Status = Clean(device.Status) ?? "Đang sử dụng";
            device.Supplier = Clean(device.Supplier);

            using (var db = new ApplicationDbContext())
            {
                db.Devices.Add(device);
                await db.SaveChangesAsync();
                return Ok(new
                {
                    DeviceId = device.Id,
                    device.DeviceName,
                    device.Year,
                    device.Classification,
                    device.InsuranceType,
                    device.Department,
                    device.PurchaseDate,
                    device.ExpiryDate,
                    device.Quantity,
                    device.Value,
                    device.InsuranceFee,
                    device.Status,
                    device.Supplier
                });
            }
        }

        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] Device updated)
        {
            if (updated == null || string.IsNullOrWhiteSpace(updated.DeviceName))
                return BadRequest("Tên thiết bị là bắt buộc.");
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

                device.DeviceName = updated.DeviceName.Trim();
                device.Year = updated.Year;
                device.Classification = Clean(updated.Classification);
                device.InsuranceType = Clean(updated.InsuranceType);
                device.Department = Clean(updated.Department);
                device.PurchaseDate = updated.PurchaseDate;
                device.ExpiryDate = updated.ExpiryDate;
                device.Quantity = updated.Quantity.GetValueOrDefault(1) < 1 ? 1 : updated.Quantity;
                device.Value = updated.Value;
                device.InsuranceFee = updated.InsuranceFee;
                device.Status = Clean(updated.Status) ?? "Đang sử dụng";
                device.Supplier = Clean(updated.Supplier);

                await db.SaveChangesAsync();
                return Ok(new { DeviceId = device.Id });
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
    }
}
