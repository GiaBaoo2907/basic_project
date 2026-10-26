using System;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string DeviceName { get; set; }

        public int? Year { get; set; }

        [StringLength(100)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string InsuranceType { get; set; }

        [StringLength(200)]
        public string Department { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? Quantity { get; set; }

        public decimal Value { get; set; }

        public decimal InsuranceFee { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(200)]
        public string Supplier { get; set; }
    }
}
