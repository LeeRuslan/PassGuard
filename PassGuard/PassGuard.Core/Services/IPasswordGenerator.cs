using System;
using System.Text;

namespace PassGuard
{
    public static class PasswordGenerator
    {
        private static readonly Random _random = new Random();

        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";

        public static string Generate(int length = 12, bool includeUppercase = true, bool includeDigits = true)
        {
            if (length < 8) length = 8;
            if (length > 32) length = 32;

            var charSet = new StringBuilder(Lowercase);

            if (includeUppercase)
                charSet.Append(Uppercase);

            if (includeDigits)
                charSet.Append(Digits);

            var password = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                int index = _random.Next(charSet.Length);
                password.Append(charSet[index]);
            }

            return password.ToString();
        }

        public static string GenerateSimple()
        {
            return Generate(10, true, true);
        }
    }
}