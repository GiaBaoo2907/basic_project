using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagement.Models
{
    public class Request
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [Index("IX_Requests_Code", IsUnique = true)]
        public string Code { get; set; }

        [Required, MaxLength(500)]
        public string Title { get; set; }

        [Required, MaxLength(200)]
        public string Creator { get; set; }

        [Required, MaxLength(200)]
        public string DepartmentProject { get; set; }

        [MaxLength(100)]
        public string Classification { get; set; }

        [MaxLength(200)]
        public string InsuranceType { get; set; }

        public DateTime? InsuranceExpiry { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
