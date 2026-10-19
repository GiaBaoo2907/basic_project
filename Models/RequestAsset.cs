using System;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    public class RequestAsset
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }

        [Required, MaxLength(250)]
        public string Equipment { get; set; }

        [MaxLength(260)]
        public string DocumentName { get; set; }

        [MaxLength(200)]
        public string Supplier { get; set; }

        public DateTime PurchaseDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public decimal Value { get; set; }

        public decimal Premium { get; set; }

        public int Quantity { get; set; }

        [MaxLength(200)]
        public string InsuranceType { get; set; }

        [Required, MaxLength(100)]
        public string ContractNumber { get; set; }
    }
}
