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
    [RoutePrefix("api/requestapi/{requestId:int}/attachments")]
    public class RequestAttachmentApiController : ApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Get(int requestId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.Requests.Any(x => x.Id == requestId && !x.IsDeleted))
                    return NotFound();

                var files = db.RequestAttachments.Where(x => x.RequestId == requestId)
                    .OrderBy(x => x.UploadedAt)
                    .Select(x => new { x.Id, x.FileName, x.ContentType, x.UploadedAt, x.RequestAssetId })
                    .ToList();
                return Ok(files);
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Upload(int requestId)
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Tệp cần được gửi theo dạng multipart/form-data.");

            using (var db = new ApplicationDbContext())
            {
                if (!db.Requests.Any(x => x.Id == requestId && !x.IsDeleted))
                    return NotFound();

                var provider = await Request.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider());
                var filePart = provider.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.FileName != null);
                if (filePart == null)
                    return BadRequest("Chưa chọn tệp đính kèm.");

                int? assetId = null;
                var assetPart = provider.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.Name != null && x.Headers.ContentDisposition.Name.Trim('"') == "assetId");
                if (assetPart != null)
                {
                    int parsedAssetId;
                    if (!int.TryParse(await assetPart.ReadAsStringAsync(), out parsedAssetId) ||
                        !db.RequestAssets.Any(x => x.Id == parsedAssetId && x.RequestId == requestId))
                        return BadRequest("Invalid asset attachment.");
                    assetId = parsedAssetId;
                }

                var rawName = filePart.Headers.ContentDisposition.FileName.Trim('"');
                var fileName = Path.GetFileName(rawName);
                if (string.IsNullOrWhiteSpace(fileName))
                    return BadRequest("Tên tệp không hợp lệ.");

                var directory = HttpContext.Current.Server.MapPath("~/App_Data/RequestFiles/" + requestId);
                Directory.CreateDirectory(directory);
                var storedFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
                var fullPath = Path.Combine(directory, storedFileName);

                using (var input = await filePart.ReadAsStreamAsync())
                using (var output = File.Create(fullPath))
                    await input.CopyToAsync(output);

                var attachment = new RequestAttachment
                {
                    RequestId = requestId,
                    RequestAssetId = assetId,
                    FileName = fileName,
                    StoredFileName = storedFileName,
                    ContentType = filePart.Headers.ContentType == null ? "application/octet-stream" : filePart.Headers.ContentType.MediaType,
                    UploadedAt = DateTime.UtcNow
                };
                db.RequestAttachments.Add(attachment);
                await db.SaveChangesAsync();

                return Ok(new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.UploadedAt });
            }
        }

        [HttpGet, Route("{attachmentId:int}")]
        public IHttpActionResult Download(int requestId, int attachmentId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.Requests.Any(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var attachment = db.RequestAttachments.FirstOrDefault(x => x.Id == attachmentId && x.RequestId == requestId);
                if (attachment == null)
                    return NotFound();

                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/RequestFiles/" + requestId), attachment.StoredFileName);
                if (!File.Exists(path))
                    return NotFound();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(File.OpenRead(path))
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = attachment.FileName
                };
                return ResponseMessage(response);
            }
        }

        [HttpGet, Route("{attachmentId:int}/preview")]
        public IHttpActionResult Preview(int requestId, int attachmentId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.Requests.Any(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var attachment = db.RequestAttachments.FirstOrDefault(x => x.Id == attachmentId && x.RequestId == requestId);
                if (attachment == null)
                    return NotFound();

                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/RequestFiles/" + requestId), attachment.StoredFileName);
                if (!File.Exists(path))
                    return NotFound();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(File.OpenRead(path))
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(attachment.ContentType ?? "application/octet-stream");
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
                {
                    FileName = attachment.FileName
                };
                return ResponseMessage(response);
            }
        }

        [HttpDelete, Route("{attachmentId:int}")]
        public async Task<IHttpActionResult> Delete(int requestId, int attachmentId)
        {
            using (var db = new ApplicationDbContext())
            {
                if (!db.Requests.Any(x => x.Id == requestId && !x.IsDeleted)) return NotFound();
                var attachment = await db.RequestAttachments.FirstOrDefaultAsync(x => x.Id == attachmentId && x.RequestId == requestId);
                if (attachment == null)
                    return NotFound();

                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/RequestFiles/" + requestId), attachment.StoredFileName);
                db.RequestAttachments.Remove(attachment);
                await db.SaveChangesAsync();
                if (File.Exists(path))
                    File.Delete(path);
                return Ok();
            }
        }
    }
}
