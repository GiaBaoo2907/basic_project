namespace DeviceManagement.Models
{
    public class CatalogItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string TextColor { get; set; }

        public string BackgroundColor { get; set; }

        public string EntityType { get; set; }

        public string Description { get; set; }

        public string Classification { get; set; }

        public string Department { get; set; }

        public string UnitOfMeasure { get; set; }

        public int? Quantity { get; set; }
    }
}
