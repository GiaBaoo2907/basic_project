using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagement.Models
{
    [Table("Materials")]
    public class MaterialCatalogItem
    {
        [Key]
        public int MaterialId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string MaterialCode { get; set; }

        [Required, StringLength(250)]
        public string MaterialName { get; set; }

        [StringLength(100)]
        public string Classification { get; set; }

        [StringLength(50)]
        public string UnitOfMeasure { get; set; }

        public int Quantity { get; set; }
    }

    [Table("Status")]
    public class StatusCatalogItem
    {
        [Key]
        public int StatusId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string StatusCode { get; set; }

        [Required, StringLength(100)]
        public string StatusName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required, StringLength(7)]
        public string TextColor { get; set; }

        [Required, StringLength(7)]
        public string BackgroundColor { get; set; }
    }

    [Table("Units")]
    public class UnitCatalogItem
    {
        [Key]
        public int UnitId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string UnitCode { get; set; }

        [Required, StringLength(100)]
        public string UnitName { get; set; }
    }

    [Table("Classifications")]
    public class ClassificationCatalogItem
    {
        [Key]
        public int ClassificationId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string ClassificationCode { get; set; }

        [Required, StringLength(150)]
        public string ClassificationName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }

    [Table("DepartmentsProjects")]
    public class ProjectCatalogItem
    {
        [Key]
        public int DepartmentProjectId { get; set; }

        [Required, StringLength(30), Column(TypeName = "varchar")]
        public string Code { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string EntityType { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }

    [Table("InsuranceTypes")]
    public class InsuranceTypeCatalogItem
    {
        [Key]
        public int InsuranceTypeId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string InsuranceTypeCode { get; set; }

        [Required, StringLength(200)]
        public string InsuranceTypeName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }

    [Table("Suppliers")]
    public class SupplierCatalogItem
    {
        [Key]
        public int SupplierId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string SupplierCode { get; set; }

        [Required, StringLength(200)]
        public string SupplierName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }

    [Table("InspectionTypes")]
    public class InspectionTypeCatalogItem
    {
        [Key]
        public int InspectionTypeId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string InspectionTypeCode { get; set; }

        [Required, StringLength(200)]
        public string InspectionTypeName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }
    [Table("AccidentCause")]
    public class AccidentCauseCatalogItem
    {
        [Key]
        public int AccidentCauseId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string AccidentCauseCode { get; set; }

        [Required, StringLength(200)]
        public string AccidentCauseName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }

    [Table("AccidentTypes")]
    public class AccidentTypeCatalogItem
    {
        [Key]
        public int AccidentTypesId { get; set; }

        [Required, StringLength(20), Column(TypeName = "varchar")]
        public string AccidentTypesCode { get; set; }

        [Required, StringLength(200)]
        public string AccidentTypesName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }
    }
}
