using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.Utility
{
    internal class SHA384Encryption
    {
        internal SHA384Encryption()
        {
        }

        public static string Encrypt(string data)
        {
            try
            {
                byte[] array = SHA384.HashData(Encoding.UTF8.GetBytes(data));
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < array.Length; i++)
                {
                    stringBuilder.Append(array[i].ToString("x2"));
                }

                return stringBuilder.ToString();
            }
            catch
            {
                throw new Exception("Invalid Message");
            }
        }
    }
}
