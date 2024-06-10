using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.Utility
{
    internal class MD5Encryption : Encryption
    {
        internal MD5Encryption()
        {
        }

        public static string Encrypt(string data)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                return Encryption.ByteToHex(MD5.HashData(bytes.AsSpan(0, bytes.Length)));
            }
            catch
            {
                throw new Exception("Invalid Message");
            }
        }
    }
}
