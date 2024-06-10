namespace Collecto.CoreAPI.Models.Objects.Setups
{
    public class BaseObject
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public StatusEnum Status { get; set; }
        public int SeqId { get; set; }
        public string StatusDetail => Status.ToString();
    }
}
