namespace DeviceManagement.Models
{
    public class ManageCategory
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public System.Collections.Generic.IList<string> Items { get; set; }
    }
}
