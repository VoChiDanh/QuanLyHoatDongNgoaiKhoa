using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace QL_HDNK.Services
{
    public static class QrTokenService
    {
        private const int DefaultQrIntervalSeconds = 20;
        private const int DefaultAllowedQrWindows = 1;
        private const int DefaultSubmissionMinutes = 1;
        private const string QrPrefix = "qr";
        private const string SubmissionPrefix = "form";

        public static string CreateToken(string eventId, DateTime now)
        {
            return CreateQrToken(eventId, now);
        }

        public static bool ValidateToken(string eventId, string token, DateTime now)
        {
            return ValidateQrToken(eventId, token, now);
        }

        public static string CreateQrToken(string eventId, DateTime now)
        {
            var bucket = GetBucket(now, QrIntervalSeconds);
            return CreateSignedToken(QrPrefix, eventId + "|" + bucket);
        }

        public static bool ValidateQrToken(string eventId, string token, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string payload;
            if (!TryReadSignedToken(token, QrPrefix, out payload))
            {
                return false;
            }

            var parts = payload.Split('|');
            long tokenBucket;
            if (parts.Length != 2 || parts[0] != eventId || !long.TryParse(parts[1], out tokenBucket))
            {
                return false;
            }

            var currentBucket = GetBucket(now, QrIntervalSeconds);
            var age = currentBucket - tokenBucket;
            return age >= 0 && age <= AllowedQrWindows;
        }

        public static string CreateSubmissionToken(string eventId, string userId, DateTime now)
        {
            return CreateSignedToken(SubmissionPrefix, eventId + "|" + userId + "|" + ToUnixSeconds(now));
        }

        public static bool ValidateSubmissionToken(string eventId, string userId, string token, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string payload;
            if (!TryReadSignedToken(token, SubmissionPrefix, out payload))
            {
                return false;
            }

            var parts = payload.Split('|');
            long issuedSeconds;
            if (parts.Length != 3 || parts[0] != eventId || parts[1] != userId || !long.TryParse(parts[2], out issuedSeconds))
            {
                return false;
            }

            var ageSeconds = ToUnixSeconds(now) - issuedSeconds;
            return ageSeconds >= 0 && ageSeconds <= SubmissionLifetimeSeconds;
        }

        public static int QrIntervalSeconds
        {
            get { return ReadPositiveIntSetting("QrIntervalSeconds", DefaultQrIntervalSeconds); }
        }

        private static int AllowedQrWindows
        {
            get { return ReadPositiveIntSetting("QrAllowedWindows", DefaultAllowedQrWindows); }
        }

        private static int SubmissionLifetimeSeconds
        {
            get { return ReadPositiveIntSetting("QrSubmissionMinutes", DefaultSubmissionMinutes) * 60; }
        }

        private static long GetBucket(DateTime value, int intervalSeconds)
        {
            return ToUnixSeconds(value) / intervalSeconds;
        }

        private static long ToUnixSeconds(DateTime value)
        {
            var utc = value.ToUniversalTime();
            return (long)(utc - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private static string CreateSignedToken(string prefix, string payload)
        {
            var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            var signature = Sign(prefix + "." + encodedPayload);
            return prefix + "." + encodedPayload + "." + signature;
        }

        private static bool TryReadSignedToken(string token, string expectedPrefix, out string payload)
        {
            payload = null;
            var parts = token.Split('.');
            if (parts.Length != 3 || parts[0] != expectedPrefix)
            {
                return false;
            }

            var signedValue = parts[0] + "." + parts[1];
            if (!SlowEquals(Sign(signedValue), parts[2]))
            {
                return false;
            }

            try
            {
                payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string Sign(string value)
        {
            var secret = ConfigurationManager.AppSettings["QrSecretKey"] ?? "QL_HDNK_QR_SECRET";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var padded = value.Replace("-", "+").Replace("_", "/");
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            return Convert.FromBase64String(padded);
        }

        private static int ReadPositiveIntSetting(string key, int fallback)
        {
            int value;
            return int.TryParse(ConfigurationManager.AppSettings[key], out value) && value > 0 ? value : fallback;
        }

        private static bool SlowEquals(string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            var diff = left.Length ^ right.Length;
            for (var i = 0; i < left.Length && i < right.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
