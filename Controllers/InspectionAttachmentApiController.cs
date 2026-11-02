using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DeviceManagement.Data;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    [RoutePrefix("api/inspectionapi/{requestId:int}/attachments")]
    public class InspectionAttachmentApiController : ApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Get(int requestId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.InspectionRequests.Any(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                return Ok(db.InspectionAttachments.Where(x => x.InspectionRequestId == requestId)
                    .OrderBy(x => x.UploadedAt)
                    .Select(x => new { x.Id, x.FileName, x.ContentType, x.UploadedAt, x.InspectionAssetId }).ToList());
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Upload(int requestId)
        {
            if (!Request.Content.IsMimeMultipartContent()) return BadRequest("Tệp cần được gửi theo dạng multipart/form-data.");
            using (var db = new ApplicationDbContext())
            {
                if (!await db.InspectionRequests.AnyAsync(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var provider = await Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider());
                var part = provider.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.FileName != null);
                if (part == null) return BadRequest("Chưa chọn tệp đính kèm.");
                int? assetId = null;
                var assetPart = provider.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.Name != null && x.Headers.ContentDisposition.Name.Trim('"') == "assetId");
                if (assetPart != null)
                {
                    int parsed;
                    if (!int.TryParse(await assetPart.ReadAsStringAsync(), out parsed) ||
                        !await db.InspectionAssets.AnyAsync(x => x.InspectionAssetId == parsed && x.InspectionRequestCode == db.InspectionRequests.Where(r => r.Id == requestId).Select(r => r.Code).FirstOrDefault()))
                        return BadRequest("Thiết bị đính kèm không thuộc phiếu kiểm định này.");
                    assetId = parsed;
                }
                var fileName = Path.GetFileName(part.Headers.ContentDisposition.FileName.Trim('"'));
                if (string.IsNullOrWhiteSpace(fileName)) return BadRequest("Tên tệp không hợp lệ.");
                var directory = HttpContext.Current.Server.MapPath("~/App_Data/InspectionFiles/" + requestId);
                Directory.CreateDirectory(directory);
                var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
                var path = Path.Combine(directory, storedName);
                using (var input = await part.ReadAsStreamAsync())
                using (var output = File.Create(path)) await input.CopyToAsync(output);
                var attachment = new InspectionAttachment
                {
                    InspectionRequestId = requestId, InspectionAssetId = assetId, FileName = fileName,
                    StoredFileName = storedName,
                    ContentType = part.Headers.ContentType == null ? "application/octet-stream" : part.Headers.ContentType.MediaType,
                    UploadedAt = DateTime.UtcNow
                };
                db.InspectionAttachments.Add(attachment);
                await db.SaveChangesAsync();
                return Ok(new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.UploadedAt, attachment.InspectionAssetId });
            }
        }

        [HttpGet, Route("{attachmentId:int}")]
        public IHttpActionResult Download(int requestId, int attachmentId) { return FileResponse(requestId, attachmentId, false); }
        [HttpGet, Route("{attachmentId:int}/preview")]
        public IHttpActionResult Preview(int requestId, int attachmentId) { return FileResponse(requestId, attachmentId, true); }

        private IHttpActionResult FileResponse(int requestId, int attachmentId, bool inline)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.InspectionRequests.Any(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var file = db.InspectionAttachments.FirstOrDefault(x => x.Id == attachmentId && x.InspectionRequestId == requestId);
                if (file == null) return NotFound();
                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/InspectionFiles/" + requestId), file.StoredFileName);
                if (!System.IO.File.Exists(path)) return NotFound();
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(System.IO.File.OpenRead(path)) };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue(inline ? "inline" : "attachment") { FileName = file.FileName };
                return ResponseMessage(response);
            }
        }

        [HttpDelete, Route("{attachmentId:int}")]
        public async Task<IHttpActionResult> Delete(int requestId, int attachmentId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!await db.InspectionRequests.AnyAsync(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var file = await db.InspectionAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId && x.InspectionRequestId == requestId);
                if (file == null) return NotFound();
                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/InspectionFiles/" + requestId), file.StoredFileName);
                db.InspectionAttachments.Remove(file);
                await db.SaveChangesAsync();
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                return Ok();
            }
        }
    }
}
