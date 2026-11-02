using System;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    // Separate tracking record for the inspection module. It shares only the root material catalog.
    public class InspectionDevice
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string MaterialCode { get; set; }

        [Required, StringLength(250)]
        public string DeviceName { get; set; }

        public int? Year { get; set; }

        [StringLength(100)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string Department { get; set; }

        public DateTime? PurchaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? Quantity { get; set; }

        [StringLength(50)]
        public string UnitOfMeasure { get; set; }

        public decimal Value { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(200)]
        public string Supplier { get; set; }
    }
}
