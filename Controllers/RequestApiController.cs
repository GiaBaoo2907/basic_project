using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DeviceManagement.Models;
using DeviceManagement.Services;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/requestapi")]
    public class RequestApiController : ApiController
    {
        private readonly RequestService _service = new RequestService();

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
        public async Task<IHttpActionResult> Create([FromBody] RequestForm model)
        {
            string validationError;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (!ValidateForm(model, out validationError))
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
        public async Task<IHttpActionResult> Update(int id, [FromBody] RequestForm model)
        {
            string validationError;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != model.Id)
                return BadRequest("Id mismatch");

            if (!ValidateForm(model, out validationError))
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

        private static bool ValidateForm(RequestForm model, out string error)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title) ||
                string.IsNullOrWhiteSpace(model.Creator) || string.IsNullOrWhiteSpace(model.DepartmentProject) ||
                !model.CreatedDate.HasValue)
            {
                error = "Vui lòng nhập đầy đủ tiêu đề, người lập, bộ phận/dự án và ngày thực hiện kiểm tra.";
                return false;
            }

            if (model.Assets == null)
                model.Assets = new System.Collections.Generic.List<RequestAssetForm>();

            foreach (var asset in model.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.Equipment) ||
                    !asset.PurchaseDate.HasValue ||
                    string.IsNullOrWhiteSpace(asset.ContractNumber) || asset.Quantity < 1 ||
                    asset.Value < 0 || asset.Premium < 0)
                {
                    error = "Mỗi vật tư cần có thiết bị, ngày mua bảo hiểm, giá trị, phí bảo hiểm, số lượng và số hợp đồng hợp lệ.";
                    return false;
                }
            }

            error = null;
            return true;
        }

    }
}
