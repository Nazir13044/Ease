namespace Collecto.CoreAPI.Models.Objects.Setups
{
    public class AuthModules
    {
        public string ModuleName { get; set; }
        public string ModuleId { get; set; }
        public int PendingItems { get; set; }
    }

    public class AuthModuleColumn
    {
        public string Field { get; set; }
        public string Title { get; set; }
        public int Width { get; set; }
        public string Attributes { get; set; }
    }

    public class AuthModuleBasic
    {
        public string ModuleId { get; set; }
        public string ModuleName { get; set; }
    }
}
