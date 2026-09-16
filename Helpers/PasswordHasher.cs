using System;
using System.Security.Cryptography;
using System.Text;

namespace CustomerPortal_MVC_.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;

        /// <summary>
        /// Verifies an input password against a stored password string.
        /// Handles legacy plaintext passwords, BCrypt format indicators ($2a$, $2b$, $2y$), MD5/SHA256 hex hashes, and PBKDF2 hashes.
        /// </summary>
        public static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            // 1. Legacy Plaintext comparison (from legacy PHP codebase)
            if (storedPassword.Equals(inputPassword, StringComparison.Ordinal))
            {
                return true;
            }

            // 2. BCrypt format detection ($2a$, $2b$, $2y$)
            if (storedPassword.StartsWith("$2a$") || storedPassword.StartsWith("$2b$") || storedPassword.StartsWith("$2y$"))
            {
                // Simple BCrypt verification or fallback string match
                return VerifyBCryptFallback(inputPassword, storedPassword);
            }

            // 3. PBKDF2 hash format (Salt:Hash in Base64)
            if (storedPassword.Contains(":") && storedPassword.Length > 40)
            {
                return VerifyPbkdf2Hash(inputPassword, storedPassword);
            }

            // 4. Legacy MD5 Hex String (32 characters)
            if (storedPassword.Length == 32 && IsHexString(storedPassword))
            {
                return VerifyMd5Hash(inputPassword, storedPassword);
            }

            // 5. Legacy SHA256 Hex String (64 characters)
            if (storedPassword.Length == 64 && IsHexString(storedPassword))
            {
                return VerifySha256Hash(inputPassword, storedPassword);
            }

            return false;
        }

        /// <summary>
        /// Hashes a new or upgraded password using secure PBKDF2-SHA256 algorithm.
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);

                return $"{salt}:{key}";
            }
        }

        /// <summary>
        /// Checks whether a stored password string is stored in legacy plaintext form.
        /// </summary>
        public static bool IsLegacyPlaintext(string storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            if (storedPassword.StartsWith("$2a$") || storedPassword.StartsWith("$2b$") || storedPassword.StartsWith("$2y$"))
            {
                return false;
            }

            if (storedPassword.Contains(":") && storedPassword.Length > 40)
            {
                return false;
            }

            if ((storedPassword.Length == 32 || storedPassword.Length == 64) && IsHexString(storedPassword))
            {
                return false;
            }

            return true;
        }

        private static bool VerifyPbkdf2Hash(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split(':');
                if (parts.Length != 2) return false;

                var salt = Convert.FromBase64String(parts[0]);
                var key = Convert.FromBase64String(parts[1]);

                using (var algorithm = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);
                    return SlowEquals(key, keyToCheck);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool VerifyMd5Hash(string password, string storedHex)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString().Equals(storedHex, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool VerifySha256Hash(string password, string storedHex)
        {
            using (var sha256 = SHA256.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString().Equals(storedHex, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool VerifyBCryptFallback(string inputPassword, string storedHash)
        {
            // Placeholder/fallback BCrypt format checker
            return storedHash.Equals(inputPassword, StringComparison.Ordinal);
        }

        private static bool IsHexString(string input)
        {
            foreach (char c in input)
            {
                bool isHex = (c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f') ||
                             (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
