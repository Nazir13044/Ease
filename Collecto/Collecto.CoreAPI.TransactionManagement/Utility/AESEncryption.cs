using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.Utility
{
    internal class AESEncryption : Encryption
    {
        private AESEncryption()
        {
        }

        public static string Encrypt(string privateKey, string publicKey, string data, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 1)
        {
            try
            {
                Encryption.Initialize(privateKey, publicKey, out var rgbKey, out var rgbIV, keySize, 128);
                using Aes aes = Aes.Create();
                aes.Mode = mode;
                aes.Padding = padding;
                using ICryptoTransform transform = aes.CreateEncryptor(rgbKey, rgbIV);
                byte[] array;
                using (MemoryStream memoryStream = new())
                {
                    using CryptoStream stream = new(memoryStream, transform, CryptoStreamMode.Write);
                    using (StreamWriter streamWriter = new(stream))
                    {
                        streamWriter.Write(data);
                    }

                    array = memoryStream.ToArray();
                }

                return output switch
                {
                    1 => Encryption.ByteToHex(array),
                    2 => Convert.ToBase64String(array),
                    _ => Encoding.UTF8.GetString(array),
                };
            }
            catch
            {
                throw new Exception("Invalid Message");
            }
        }

        public static string Decrypt(string privateKey, string publicKey, string data, int input = 3, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 3)
        {
            string text;
            try
            {
                Encryption.Initialize(privateKey, publicKey, out var rgbKey, out var rgbIV, keySize, 128);
                byte[] buffer = input switch
                {
                    1 => Encryption.HexToBytes(data),
                    2 => Convert.FromBase64String(data),
                    _ => Encoding.UTF8.GetBytes(data),
                };
                using Aes aes = Aes.Create();
                aes.Mode = mode;
                aes.Padding = padding;
                using ICryptoTransform transform = aes.CreateDecryptor(rgbKey, rgbIV);
                using MemoryStream stream = new MemoryStream(buffer);
                using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                using StreamReader streamReader = new StreamReader(stream2, Encoding.UTF8);
                text = streamReader.ReadToEnd();
                switch (output)
                {
                    case 1:
                        text = Encryption.ByteToHex(Encoding.UTF8.GetBytes(text));
                        break;
                    case 2:
                        text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                        break;
                }
            }
            catch
            {
                throw new Exception("Invalid Message");
            }

            return text;
        }
    }
}
