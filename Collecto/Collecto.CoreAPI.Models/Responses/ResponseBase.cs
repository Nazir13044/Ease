using System.Collections;

namespace Collecto.CoreAPI.Models.Responses
{
    public abstract class ResponseBase
    {
        protected ResponseBase()
        {
            ReturnMessage = [];
        }
        public int ReturnStatus { get; set; }

        public List<string> ReturnMessage { get; set; }
    }

    public abstract class TotalRowsResponseBase : ResponseBase
    {
        public int TotalRows { get; set; }
    }

    public abstract class ResponseBase1 : ResponseBase
    {
        protected ResponseBase1() : base()
        {
            ValidationErrors = new Hashtable();
        }
        public Hashtable ValidationErrors { get; set; }
    }

    public abstract class BaseObjectResponse : ResponseBase
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public StatusEnum Status { get; set; }
        public int SeqId { get; set; }
        public string StatusDetail => Status.ToString();
    }

    public class BooleanResponse : ResponseBase
    {
        public bool Value { get; set; }
    }

    public class IntegerResponse : ResponseBase
    {
        public int Value { get; set; }
    }

    public class StringResponse : ResponseBase
    {
        public string Value { get; set; }
    }

    public class StringArrayResponse : ResponseBase
    {
        public string[] Value { get; set; }
    }
}
