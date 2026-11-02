using System;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    public class InsuranceAttachment
    {
        [Key]
        public int Id { get; set; }

        public int InsuranceId { get; set; }

        public int? InsuranceAssetId { get; set; }

        [Required, MaxLength(260)]
        public string FileName { get; set; }

        [Required, MaxLength(260)]
        public string StoredFileName { get; set; }

        [MaxLength(150)]
        public string ContentType { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
