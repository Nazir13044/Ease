namespace Collecto.CoreAPI.Models.Objects
{
    public class FirebaseMessage
    {
        public List<string> To { get; set; }
        public FirebaseMessageData Notification { get; set; }
    }

    public class FirebaseMessageData
    {
        public string Title { get; set; }
        public string Body { get; set; }
    }
}
