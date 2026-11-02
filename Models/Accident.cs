using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagement.Models
{
    [Table("Accidents")]
    public class Accident
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Column(TypeName = "varchar")]
        public string AccidentCode { get; set; }

        [Required, StringLength(20)]
        [Column(TypeName = "varchar")]
        public string MaterialCode { get; set; }

        [Required, StringLength(250)]
        public string MaterialName { get; set; }

        [StringLength(150)]
        public string Classification { get; set; }

        [StringLength(150)]
        public string Department { get; set; }

        public decimal Value { get; set; }

        [Required, StringLength(1000)]
        public string LossDescription { get; set; }

        public DateTime AccidentDate { get; set; }

        [StringLength(200)]
        public string AccidentType { get; set; }

        [StringLength(200)]
        public string AccidentCause { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
