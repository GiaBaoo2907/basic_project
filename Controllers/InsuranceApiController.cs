using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Data.Entity;
using DeviceManagement.Data;
using DeviceManagement.Models;
using DeviceManagement.Services;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/requestapi")]
    public class InsuranceApiController : ApiController
    {
        private readonly InsuranceService _service = new InsuranceService();

        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            try
            {
                var data = _service.Query()
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route("deleted")]
        public IHttpActionResult GetDeleted()
        {
            try
            {
                var data = _service.QueryDeleted().OrderByDescending(x => x.DeletedAt).ToList();
                return Ok(data);
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        // GET api/requestapi/5
        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            try
            {
                var entity = await _service.GetFormAsync(id);
                if (entity == null)
                    return NotFound();

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/requestapi
        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create([FromBody] InsuranceForm model)
        {
            string validationError;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (!ValidateForm(model, out validationError))
                return BadRequest(validationError);
            validationError = await ValidateMaterialNamesAsync(model);
            if (validationError != null)
                return BadRequest(validationError);

            try
            {
                var created = await _service.SaveFormAsync(model);
                return CreatedAtRoute("DefaultApi", new { controller = "requestapi", id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // PUT api/requestapi/5
        [HttpPut, Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] InsuranceForm model)
        {
            string validationError;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != model.Id)
                return BadRequest("Id mismatch");

            if (!ValidateForm(model, out validationError))
                return BadRequest(validationError);
            validationError = await ValidateMaterialNamesAsync(model);
            if (validationError != null)
                return BadRequest(validationError);

            try
            {
                var updated = await _service.SaveFormAsync(model);
                if (updated == null)
                    return NotFound();
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // DELETE api/requestapi/5
        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            try
            {
                if (!await _service.DeleteAsync(id)) return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost, Route("{id:int}/restore")]
        public async Task<IHttpActionResult> Restore(int id)
        {
            try
            {
                if (!await _service.RestoreAsync(id)) return NotFound();
                return Ok();
            }
            catch (Exception ex) { return InternalServerError(ex); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _service?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static bool ValidateForm(InsuranceForm model, out string error)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title) ||
                string.IsNullOrWhiteSpace(model.Creator) || string.IsNullOrWhiteSpace(model.DepartmentProject) ||
                !model.CreatedDate.HasValue)
            {
                error = "Vui lòng nhập đầy đủ tiêu đề, người lập, bộ phận/dự án và ngày thực hiện kiểm tra.";
                return false;
            }

            if (model.Assets == null)
                model.Assets = new System.Collections.Generic.List<InsuranceAssetForm>();

            foreach (var asset in model.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.Equipment) ||
                    !asset.PurchaseDate.HasValue ||
                    string.IsNullOrWhiteSpace(asset.ContractNumber) || asset.Quantity < 1 ||
                    asset.Value < 0 || asset.Premium < 0)
                {
                    error = "Thiết bị trong phiếu chưa có đủ dữ liệu từ danh mục (tên, ngày mua, số lượng, giá trị, phí bảo hiểm và số hợp đồng). Hãy cập nhật danh mục thiết bị rồi chọn lại.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static async Task<string> ValidateMaterialNamesAsync(InsuranceForm model)
        {
            var materialCodes = model.Assets
                .Where(asset => asset != null && !string.IsNullOrWhiteSpace(asset.MaterialCode))
                .Select(asset => asset.MaterialCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (model.Assets.Count == 0) return null;

            using (var db = new ApplicationDbContext())
            {
                if (!await db.DepartmentsProjects.AnyAsync(item => item.Name == model.DepartmentProject.Trim()))
                    return "Bộ phận/dự án không có trong danh mục Quản trị.";
                if (!string.IsNullOrWhiteSpace(model.Classification) &&
                    !await db.Classifications.AnyAsync(item => item.ClassificationName == model.Classification.Trim()))
                    return "Phân loại không có trong danh mục Quản trị.";
                if (!string.IsNullOrWhiteSpace(model.InsuranceType) &&
                    !await db.InsuranceTypes.AnyAsync(item => item.InsuranceTypeName == model.InsuranceType.Trim()))
                    return "Loại bảo hiểm không có trong danh mục Quản trị.";

                var materials = await db.Materials
                    .Where(item => materialCodes.Contains(item.MaterialCode))
                    .Select(item => new { item.MaterialCode, item.MaterialName, item.UnitOfMeasure })
                    .ToListAsync();
                foreach (var asset in model.Assets)
                {
                    if (!string.IsNullOrWhiteSpace(asset.Supplier) &&
                        !await db.Suppliers.AnyAsync(item => item.SupplierName == asset.Supplier.Trim()))
                        return "Nhà cung cấp không có trong danh mục Quản trị.";
                    if (!string.IsNullOrWhiteSpace(asset.InsuranceType) &&
                        !await db.InsuranceTypes.AnyAsync(item => item.InsuranceTypeName == asset.InsuranceType.Trim()))
                        return "Loại bảo hiểm của vật tư không có trong danh mục Quản trị.";
                    if (!string.IsNullOrWhiteSpace(asset.UnitOfMeasure) &&
                        !await db.Units.AnyAsync(item => item.UnitName == asset.UnitOfMeasure.Trim()))
                        return "Đơn vị tính không có trong danh mục Quản trị.";
                    var material = materials.FirstOrDefault(item =>
                        string.Equals(item.MaterialCode, asset.MaterialCode, StringComparison.OrdinalIgnoreCase));
                    if (material == null)
                        return "Mã vật tư '" + asset.MaterialCode + "' không có trong danh mục Quản trị.";
                    asset.MaterialCode = material.MaterialCode;
                    asset.Equipment = material.MaterialName;
                    if (string.IsNullOrWhiteSpace(asset.UnitOfMeasure))
                        asset.UnitOfMeasure = material.UnitOfMeasure;
                }
                return null;
            }
        }

    }
}
