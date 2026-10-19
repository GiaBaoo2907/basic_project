using System;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    public class RequestAttachment
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }

        public int? RequestAssetId { get; set; }

        [Required, MaxLength(260)]
        public string FileName { get; set; }

        [Required, MaxLength(260)]
        public string StoredFileName { get; set; }

        [MaxLength(150)]
        public string ContentType { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
