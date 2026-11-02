using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/accidentapi")]
    public class AccidentApiController : ApiController
    {
        [HttpGet, Route("")]
        public async Task<IHttpActionResult> Get()
        {
            using (var db = new ApplicationDbContext())
            {
                var rows = await db.Accidents.Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.AccidentDate).ThenBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id, x.AccidentCode, x.MaterialCode, x.MaterialName, x.Classification,
                        x.Department, x.Value, x.LossDescription, x.AccidentDate,
                        x.AccidentType, x.AccidentCause
                    }).ToListAsync();
                return Ok(rows);
            }
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var row = await db.Accidents.Where(x => x.Id == id && !x.IsDeleted)
                    .Select(x => new
                    {
                        x.Id, x.AccidentCode, x.MaterialCode, x.MaterialName, x.Classification,
                        x.Department, x.Value, x.LossDescription, x.AccidentDate,
                        x.AccidentType, x.AccidentCause
                    }).FirstOrDefaultAsync();
                if (row == null) return NotFound();
                return Ok(row);
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] Accident input)
        {
            if (input == null) return BadRequest("Dữ liệu phiếu tai nạn không hợp lệ.");
            var validation = ValidateInput(input);
            if (validation != null) return BadRequest(validation);

            using (var db = new ApplicationDbContext())
            {
                var relatedError = await ValidateReferencesAsync(db, input);
                if (relatedError != null) return BadRequest(relatedError);

                var material = await db.Materials.FirstAsync(x => x.MaterialCode == input.MaterialCode.Trim());
                var incident = new Accident
                {
                    AccidentCode = "SC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N").Substring(0, 6),
                    MaterialCode = material.MaterialCode,
                    MaterialName = material.MaterialName,
                    Classification = material.Classification,
                    Department = input.Department.Trim(),
                    Value = input.Value,
                    LossDescription = input.LossDescription.Trim(),
                    AccidentDate = input.AccidentDate,
                    AccidentType = Clean(input.AccidentType),
                    AccidentCause = Clean(input.AccidentCause),
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                db.Accidents.Add(incident);
                await db.SaveChangesAsync();
                return Ok(ToResult(incident));
            }
        }

        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] Accident input)
        {
            if (input == null) return BadRequest("Dữ liệu phiếu tai nạn không hợp lệ.");
            var validation = ValidateInput(input);
            if (validation != null) return BadRequest(validation);

            using (var db = new ApplicationDbContext())
            {
                var incident = await db.Accidents.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (incident == null) return NotFound();
                var relatedError = await ValidateReferencesAsync(db, input);
                if (relatedError != null) return BadRequest(relatedError);

                var material = await db.Materials.FirstAsync(x => x.MaterialCode == input.MaterialCode.Trim());
                incident.MaterialCode = material.MaterialCode;
                incident.MaterialName = material.MaterialName;
                incident.Classification = material.Classification;
                incident.Department = input.Department.Trim();
                incident.Value = input.Value;
                incident.LossDescription = input.LossDescription.Trim();
                incident.AccidentDate = input.AccidentDate;
                incident.AccidentType = Clean(input.AccidentType);
                incident.AccidentCause = Clean(input.AccidentCause);

                await db.SaveChangesAsync();
                return Ok(ToResult(incident));
            }
        }

        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var db = new ApplicationDbContext())
            {
                var incident = await db.Accidents.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (incident == null) return NotFound();
                incident.IsDeleted = true;
                incident.DeletedAt = DateTime.Now;
                await db.SaveChangesAsync();
                return Ok();
            }
        }

        private static string ValidateInput(Accident input)
        {
            if (string.IsNullOrWhiteSpace(input.MaterialCode)) return "Vui lòng chọn vật tư.";
            if (string.IsNullOrWhiteSpace(input.Department)) return "Vui lòng chọn bộ phận/dự án.";
            if (string.IsNullOrWhiteSpace(input.LossDescription)) return "Vui lòng nhập mô tả tai nạn/tổn thất.";
            if (input.LossDescription.Trim().Length > 1000) return "Mô tả không được vượt quá 1000 ký tự.";
            if (input.Value < 0) return "Giá trị không được nhỏ hơn 0.";
            if (input.AccidentDate == default(DateTime)) return "Vui lòng chọn ngày xảy ra tai nạn/sự cố.";
            return null;
        }

        private static async Task<string> ValidateReferencesAsync(ApplicationDbContext db, Accident input)
        {
            var materialCode = input.MaterialCode.Trim();
            if (!await db.Materials.AnyAsync(x => x.MaterialCode == materialCode)) return "Vật tư đã chọn không tồn tại trong danh mục.";
            var department = input.Department.Trim();
            if (!await db.DepartmentsProjects.AnyAsync(x => x.Name == department)) return "Bộ phận/dự án đã chọn không tồn tại trong danh mục.";
            if (!string.IsNullOrWhiteSpace(input.AccidentCause))
            {
                var cause = input.AccidentCause.Trim();
                if (!await db.AccidentCauses.AnyAsync(x => x.AccidentCauseName == cause)) return "Nguyên nhân đã chọn không tồn tại trong danh mục.";
            }
            if (!string.IsNullOrWhiteSpace(input.AccidentType))
            {
                var type = input.AccidentType.Trim();
                if (!await db.AccidentTypes.AnyAsync(x => x.AccidentTypesName == type)) return "Loại tai nạn/sự cố đã chọn không tồn tại trong danh mục.";
            }
            return null;
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static object ToResult(Accident x)
        {
            return new
            {
                x.Id, x.AccidentCode, x.MaterialCode, x.MaterialName, x.Classification,
                x.Department, x.Value, x.LossDescription, x.AccidentDate, x.AccidentType, x.AccidentCause
            };
        }
    }
}
