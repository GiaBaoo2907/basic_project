using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Models
{
    public class RequestForm
    {
        public int Id { get; set; }
        public string Code { get; set; }

        [Required, StringLength(500)]
        public string Title { get; set; }

        [Required, StringLength(200)]
        public string Creator { get; set; }

        [Required, StringLength(200)]
        public string DepartmentProject { get; set; }

        [StringLength(100)]
        public string Classification { get; set; }

        [StringLength(200)]
        public string InsuranceType { get; set; }

        public DateTime? InsuranceExpiry { get; set; }

        [Required]
        public DateTime? CreatedDate { get; set; }

        public List<RequestAssetForm> Assets { get; set; } = new List<RequestAssetForm>();
    }

    public class RequestAssetForm
    {
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Equipment { get; set; }

        [StringLength(260)]
        public string DocumentName { get; set; }

        [StringLength(200)]
        public string Supplier { get; set; }

        [Required]
        public DateTime? PurchaseDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        public decimal Value { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        public decimal Premium { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(200)]
        public string InsuranceType { get; set; }

        [Required, StringLength(100)]
        public string ContractNumber { get; set; }
    }
}
