using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagement.Models
{
    public class InspectionRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Index("UX_InspectionRequests_Code", IsUnique = true)]
        public string Code { get; set; }

        [Required, StringLength(500)]
        public string Title { get; set; }

        [Required, StringLength(200)]
        public string Creator { get; set; }

        [Required, StringLength(200)]
        public string DepartmentProject { get; set; }

        [StringLength(150)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string InsuranceType { get; set; }

        public DateTime InspectionDate { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        [StringLength(200)]
        public string DeletedBy { get; set; }
    }

    public class InspectionRequestForm
    {
        public int Id { get; set; }

        public string Code { get; set; }

        [Required, StringLength(500)]
        public string Title { get; set; }

        [Required, StringLength(200)]
        public string Creator { get; set; }

        [Required, StringLength(200)]
        public string DepartmentProject { get; set; }

        [StringLength(150)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string InsuranceType { get; set; }

        [Required]
        public DateTime? InspectionDate { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }

        public List<int> DeviceIds { get; set; } = new List<int>();

        public List<InspectionEquipmentSnapshot> Equipment { get; set; } = new List<InspectionEquipmentSnapshot>();
    }

    public class InspectionEquipmentSnapshot
    {
        public int InspectionAssetId { get; set; }
        public int DeviceId { get; set; }
        public string MaterialCode { get; set; }
        public string DeviceName { get; set; }
        public int? Year { get; set; }
        public string Classification { get; set; }
        public string Department { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal Value { get; set; }
        public decimal InsuranceFee { get; set; }
        public string InspectionType { get; set; }
        public string Status { get; set; }
        public string Supplier { get; set; }
    }

    public class InspectionAsset
    {
        [Key]
        public int InspectionAssetId { get; set; }

        [Required, StringLength(30)]
        [Index("UX_InspectionAssets_InspectionRequestCode_DeviceId", 1, IsUnique = true)]
        public string InspectionRequestCode { get; set; }

        [Required, StringLength(20)]
        public string MaterialCode { get; set; }

        [Index("UX_InspectionAssets_InspectionRequestCode_DeviceId", 2, IsUnique = true)]
        public int DeviceId { get; set; }

        [Required, StringLength(250)]
        public string DeviceName { get; set; }

        public int? Year { get; set; }

        [StringLength(100)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string Department { get; set; }

        public System.DateTime? PurchaseDate { get; set; }
        public System.DateTime? ExpiryDate { get; set; }
        public int? Quantity { get; set; }

        [StringLength(50)]
        public string UnitOfMeasure { get; set; }

        public decimal Value { get; set; }

        [Column("InspectionFee")]
        public decimal InsuranceFee { get; set; }

        [StringLength(200)]
        public string InspectionType { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(200)]
        public string Supplier { get; set; }
    }
}
