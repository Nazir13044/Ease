using Collecto.CoreAPI.TransactionManagement.Helper;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.DataAccess
{
    public class ConnectionNode
    {
        private string _conString;

        public string Key { get; set; }

        public string Provider { get; set; }

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(EncryptKey))
                {
                    return _conString;
                }

                string @string = Encoding.UTF8.GetString(Convert.FromBase64String(EncryptKey));
                return Global.CipherFunctions.DecryptByAES(@string, @string, _conString, 2);
            }
            set
            {
                _conString = value;
            }
        }

        public string SqlSyntax { get; set; }

        public string EncryptKey { get; set; }
    }
}
