using System.Security.Cryptography;
using System.Text;

namespace Collecto.CoreAPI.Models.Objects
{
    /// <summary>
    /// 
    /// </summary>
    public static class Base32Encoding
    {
        /// <summary>
        /// The different characters allowed in Base32 encoding.
        /// </summary>
        /// <remarks>
        /// This is a 32-character subset of the twenty-six letters A–Z and six digits 2–7.
        /// </remarks>
        private const string Base32AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>
        /// Converts a byte array into a Base32 string.
        /// </summary>
        /// <param name="input">The string to convert to Base32.</param>
        /// <param name="addPadding">Whether or not to add RFC3548 '='-padding to the string.</param>
        /// <returns>A Base32 string.</returns>
        /// <remarks>
        /// https://tools.ietf.org/html/rfc3548#section-2.2 indicates padding MUST be added unless the reference to the RFC tells us otherswise.
        /// https://github.com/google/google-authenticator/wiki/Key-Uri-Format indicates that padding SHOULD be omitted.
        /// To meet both requirements, you can omit padding when required.
        /// </remarks>
        public static string ToBase32String(this byte[] input, bool addPadding)
        {
            if (input == null || input.Length == 0)
            {
                return string.Empty;
            }

            var bits = input.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')).Aggregate((a, b) => a + b).PadRight((int)(Math.Ceiling((input.Length * 8) / 5d) * 5), '0');
            var result = Enumerable.Range(0, bits.Length / 5).Select(i => Base32AllowedCharacters.Substring(Convert.ToInt32(bits.Substring(i * 5, 5), 2), 1)).Aggregate((a, b) => a + b);
            if (addPadding)
            {
                result = result.PadRight((int)(Math.Ceiling(result.Length / 8d) * 8), '=');
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="addPadding"></param>
        /// <returns></returns>
        public static string EncodeAsBase32String(this string input, bool addPadding)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(input);
            var result = bytes.ToBase32String(addPadding);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string DecodeFromBase32String(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var bytes = input.ToByteArray();
            var result = Encoding.UTF8.GetString(bytes);
            return result;
        }

        /// <summary>
        /// Converts a Base32 string into the corresponding byte array, using 5 bits per character.
        /// </summary>
        /// <param name="input">The Base32 String</param>
        /// <returns>A byte array containing the properly encoded bytes.</returns>
        public static byte[] ToByteArray(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Array.Empty<byte>();
            }

            var bits = input.TrimEnd('=').ToUpper().ToCharArray().Select(c => Convert.ToString(Base32AllowedCharacters.IndexOf(c), 2).PadLeft(5, '0')).Aggregate((a, b) => a + b);
            var result = Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
            return result;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TOtpService
    {
        private TimeSpan DefaultClockDriftTolerance { get; set; }
        private readonly static DateTime _epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 
        /// </summary>
        public TOtpService()
        {
            DefaultClockDriftTolerance = TimeSpan.FromMinutes(1);
        }

        private static string GeneratePINAtInterval(string secretKey, long counter, int digits)
        {
            return GenerateHashedCode(secretKey, counter, digits);
        }

        private static string GenerateHashedCode(string secretKey, long iterationNumber, int digits)
        {
            byte[] key = Base32Encoding.ToByteArray(secretKey.ToUpper());
            return GenerateHashedCode(key, iterationNumber, digits);
        }

        private static string GenerateHashedCode(byte[] key, long iterationNumber, int digits)
        {
            byte[] counter = BitConverter.GetBytes(iterationNumber);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);

            //using HMACSHA1 hmac = new(key, true);
            using HMACSHA1 hmac = new(key);
            byte[] hash = hmac.ComputeHash(counter);
            int offset = hash[^1] & 0xf;

            //Convert the 4 bytes into an integer, ignoring the sign.
            int binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | (hash[offset + 3]);
            int password = binary % (int)Math.Pow(10, digits);
            return password.ToString(new string('0', digits));
        }

        private static long GetCurrentCounter(DateTime now)
        {
            return GetCurrentCounter(now, _epoch, 30);
        }

        private static long GetCurrentCounter(DateTime now, DateTime epoch, int timeStep)
        {
            return (long)(now - epoch).TotalSeconds / timeStep;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="otpCode"></param>
        /// <returns></returns>
        public bool ValidateTwoFactorPIN(string secretKey, string otpCode)
        {
            return ValidateTwoFactorPIN(secretKey, otpCode, DateTime.UtcNow, DefaultClockDriftTolerance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="otpCode"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool ValidateTwoFactorPIN(string secretKey, string otpCode, DateTime now)
        {
            return ValidateTwoFactorPIN(secretKey, otpCode, now, DefaultClockDriftTolerance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="otpCode"></param>
        /// <param name="timeTolerance"></param>
        /// <returns></returns>
        public static bool ValidateTwoFactorPIN(string secretKey, string otpCode, TimeSpan timeTolerance)
        {
            return ValidateTwoFactorPIN(secretKey, otpCode, DateTime.UtcNow, timeTolerance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="otpCode"></param>
        /// <param name="now"></param>
        /// <param name="timeTolerance"></param>
        /// <returns></returns>
        public static bool ValidateTwoFactorPIN(string secretKey, string otpCode, DateTime now, TimeSpan timeTolerance)
        {
            var codes = GetCurrentPINs(secretKey, now, timeTolerance);
            return codes.Any(c => c == otpCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public static string GetCurrentPIN(string secretKey)
        {
            return GetCurrentPIN(secretKey, DateTime.UtcNow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static string GetCurrentPIN(string secretKey, DateTime now)
        {
            return GeneratePINAtInterval(secretKey, GetCurrentCounter(now, _epoch, 30), 6);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public string[] GetCurrentPINs(string secretKey)
        {
            return GetCurrentPINs(secretKey, DateTime.UtcNow, DefaultClockDriftTolerance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public string[] GetCurrentPINs(string secretKey, DateTime now)
        {
            return GetCurrentPINs(secretKey, now, DefaultClockDriftTolerance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="now"></param>
        /// <param name="timeTolerance"></param>
        /// <returns></returns>
        public static string[] GetCurrentPINs(string secretKey, DateTime now, TimeSpan timeTolerance)
        {
            int iterationOffset = 0;
            List<string> codes = new();
            long iterationCounter = GetCurrentCounter(now);

            if (timeTolerance.TotalSeconds > 30)
            {
                iterationOffset = Convert.ToInt32(timeTolerance.TotalSeconds / 30.00);
            }

            long iterationStart = iterationCounter - iterationOffset;
            long iterationEnd = iterationCounter + iterationOffset;

            for (long counter = iterationStart; counter <= iterationEnd; counter++)
            {
                codes.Add(GeneratePINAtInterval(secretKey, counter, 6));
            }

            return [.. codes];
        }
    }
}
