using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.Utility
{
    internal class Encryption
    {
        internal const string DefaultPublicKey = "CoMEaLtD";

        internal const string DefaultPrivateKey = "CeL.DhK.LaL1@3$5";

        protected Encryption()
        {
        }

        protected static void Initialize(string privateKey, string publicKey, out byte[] rgbKey, out byte[] rgbIV, int keySize = 128, int blockSize = 64)
        {
            try
            {
                keySize /= 8;
                if (privateKey.Length < keySize)
                {
                    privateKey = privateKey.PadLeft(keySize);
                }

                if (privateKey.Length > keySize)
                {
                    privateKey = privateKey.Substring(0, keySize);
                }

                blockSize /= 8;
                if (publicKey.Length < blockSize)
                {
                    publicKey = publicKey.PadLeft(blockSize);
                }

                if (publicKey.Length > blockSize)
                {
                    publicKey = publicKey.Substring(0, blockSize);
                }

                rgbKey = Encoding.UTF8.GetBytes(privateKey);
                rgbIV = Encoding.UTF8.GetBytes(publicKey);
            }
            catch
            {
                throw new Exception("Invaid Key");
            }
        }

        protected static string ByteToHex(byte[] value)
        {
            string text = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                text += ByteToHex(value[i]);
            }

            return text;
        }

        protected static string ByteToHex(byte value)
        {
            return Convert.ToString(value, 16).PadLeft(2, '0').ToUpperInvariant();
        }

        protected static string IntToHex(int intData)
        {
            return Convert.ToString(intData, 16).PadLeft(2, '0');
        }

        protected static byte[] HexToBytes(string value)
        {
            byte[] array = new byte[value.Length / 2];
            for (int i = 0; i < value.Length; i += 2)
            {
                array[i / 2] = HexToByte(value.Substring(i, 2));
            }

            return array;
        }

        protected static byte HexToByte(string value)
        {
            return Convert.ToByte(value, 16);
        }

        protected static int HexToInt(string hexData)
        {
            return Convert.ToInt32(hexData, 16);
        }

        internal static string Encrypt(string key, string data)
        {
            try
            {
                string text = string.Empty;
                data = (char)data.Length + data;
                if (data.Length < 10)
                {
                    data = data.PadRight(10);
                }

                char[] array = data.ToCharArray();
                while (key.Length < data.Length)
                {
                    key += key;
                }

                key = key.Substring(0, data.Length);
                char[] array2 = key.ToCharArray();
                for (int i = 0; i < data.Length; i++)
                {
                    char c = array[i];
                    char c2 = array2[i];
                    text += IntToHex(c ^ c2);
                }

                return text.ToUpper();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        internal static string Decrypt(string key, string data)
        {
            string text = string.Empty;
            int num = data.Length / 2;
            while (key.Length < num)
            {
                key += key;
            }

            key = key.Substring(0, num);
            char[] array = key.ToCharArray();
            char[] array2 = data.ToCharArray();
            for (int i = 0; i < num; i++)
            {
                string text2 = string.Empty;
                for (int j = i * 2; j < i * 2 + 2; j++)
                {
                    text2 += $"{array2.GetValue(j)}";
                }

                char c = (char)HexToInt(text2);
                char c2 = array[i];
                text += (char)(c2 ^ c);
            }

            num = text.ToCharArray()[0];
            return text.Substring(1, num);
        }

        internal static string EncryptByRSA(string xmlPublicKey, string data)
        {
            try
            {
                using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
                rSACryptoServiceProvider.FromXmlString(xmlPublicKey);
                return Convert.ToBase64String(rSACryptoServiceProvider.Encrypt(Encoding.UTF8.GetBytes(data), fOAEP: false));
            }
            catch
            {
                throw new Exception("Invalid Message");
            }
        }
    }
}
