using Collecto.CoreAPI.TransactionManagement.Helper;
using Microsoft.Reporting.NETCore;
using System.Security.Claims;

namespace Collecto.CoreAPI.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Claim CreateClaim(string type, string value)
        {
            return new Claim(type: type, value: value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expirationInMinutes"></param>
        /// <returns></returns>
        public static DateTimeOffset CreateCollectoCacheOptions(double expirationInMinutes = 360)
        {
            return DateTime.Now.AddMinutes(expirationInMinutes);
        }

        /// <summary>
        /// Decrypt data by 128 bit AES encryption algorithm
        /// </summary>
        /// <param name="secret">Used to decrypt data</param>
        /// <param name="data">Value to decrypt and value must be Base64String</param>
        /// <returns>Return plain text data if successful otherwise throw error</returns>
        public static string DecryptData(string secret, string data)
        {
            if (data == null || data.Length <= 0)
                throw new ArgumentNullException(nameof(data));

            if (secret == null || secret.Length <= 0)
                throw new ArgumentNullException(nameof(secret));

            try
            {
                data = Global.CipherFunctions.DecryptByAES(privateKey: secret, publicKey: secret, data: data, input: 2, output: 3);
            }

            catch (Exception ex)
            {
                data = $"Error in Key/Data => {ex.Message}";
            }
            return data;
        }

        /// <summary>
        /// Encrypt data by 128 bit AES encryption algorithm
        /// </summary>
        /// <param name="secret">Used to encrypt data</param>
        /// <param name="data">Value to encrypt</param>
        /// <returns> Return Base64String encrypted data if successful otherwise throw error</returns>
        public static string EncryptData(string secret, string data)
        {
            if (data == null || data.Length <= 0)
                throw new ArgumentNullException(nameof(data));

            if (secret == null || secret.Length <= 0)
                throw new ArgumentNullException(nameof(secret));
            try
            {
                data = Global.CipherFunctions.EncryptByAES(privateKey: secret, publicKey: secret, data: data, output: 2);
            }
            catch
            {
                data = "Error in Key/Data";
            }

            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        public static byte[] GeneratePdf(LocalReport report)
        {
            try
            {
                byte[] bytes = report.Render(format: "PDF", deviceInfo: null, mimeType: out string mimeType, encoding: out string encoding, fileNameExtension: out string filenameExtension, streams: out string[] streamids, warnings: out Warning[] warnings);
                return bytes;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static class SystemDefinedCustTranTypeID
        {
            public const int Invoice = 1;
            public const int Collection = 2;
            public const int Return = 3;
            public const int PrimaryInvoice = 4;
            public const int InvoicePayment = 5;
        }
    }
}
