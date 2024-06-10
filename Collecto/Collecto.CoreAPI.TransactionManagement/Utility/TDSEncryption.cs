using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.TransactionManagement.Utility
{
    internal class TDSEncryption : Encryption
    {
        internal TDSEncryption()
        {
        }

        internal static string Encrypt(string privateKey, string publicKey, string data, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 1)
        {
            try
            {
                Encryption.Initialize(privateKey, publicKey, out var rgbKey, out var rgbIV, keySize);
                using TripleDES tripleDES = TripleDES.Create();
                tripleDES.Mode = mode;
                tripleDES.Padding = padding;
                using ICryptoTransform transform = tripleDES.CreateEncryptor(rgbKey, rgbIV);
                byte[] array;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
                    using (StreamWriter streamWriter = new StreamWriter(stream))
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

        internal static string Decrypt(string privateKey, string publicKey, string data, int input = 3, int keySize = 128, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, int output = 3)
        {
            string text;
            try
            {
                Encryption.Initialize(privateKey, publicKey, out var rgbKey, out var rgbIV, keySize);
                byte[] buffer = input switch
                {
                    1 => Encryption.HexToBytes(data),
                    2 => Convert.FromBase64String(data),
                    _ => Encoding.UTF8.GetBytes(data),
                };
                using TripleDES tripleDES = TripleDES.Create();
                using ICryptoTransform transform = tripleDES.CreateDecryptor(rgbKey, rgbIV);
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
