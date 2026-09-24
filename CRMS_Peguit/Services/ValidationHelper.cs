using System;
using System.Text.RegularExpressions;

namespace CRMS_Peguit.winforms.Services
{
    public static class ValidationHelper
    {
        // Supports English and international/accented letters (e.g. ñ, é, ü), spaces, dots, hyphens, and apostrophes
        private static readonly Regex ValidNameRegex = new Regex(@"^[\p{L}\s\.\-']+$", RegexOptions.Compiled);
        
        // Allowed characters in phone strings: digits, +, -, (, ), and spaces
        private static readonly Regex AllowedPhoneCharsRegex = new Regex(@"^[0-9\+\-\s\(\)]+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates that a person's name contains only letters, spaces, hyphens, and apostrophes.
        /// Rejects numbers and special symbols.
        /// </summary>
        public static bool IsValidPersonName(string? name, string fieldName, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            string trimmed = name.Trim();

            // Check if any character is a digit
            foreach (char c in trimmed)
            {
                if (char.IsDigit(c))
                {
                    errorMessage = $"{fieldName} cannot contain numbers. Please use letters only.";
                    return false;
                }
            }

            if (!ValidNameRegex.IsMatch(trimmed))
            {
                errorMessage = $"{fieldName} contains invalid characters. Use letters, spaces, hyphens, and apostrophes only.";
                return false;
            }

            if (trimmed.Length < 1 || trimmed.Length > 50)
            {
                errorMessage = $"{fieldName} must be between 1 and 50 characters.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Validates phone number input.
        /// Phone is optional; if provided, enforces digit constraints (7 to 15 digits according to ITU-T E.164 standard)
        /// and recognizes Philippine mobile (09XXXXXXXXX) and international (+63...) formats.
        /// </summary>
        public static bool IsValidPhoneNumber(string? phone, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                errorMessage = null;
                return true; // Phone is optional
            }

            string trimmed = phone.Trim();

            if (!AllowedPhoneCharsRegex.IsMatch(trimmed))
            {
                errorMessage = "Phone number contains invalid characters. Use digits, +, -, and spaces only.";
                return false;
            }

            // Extract only numeric digits
            string digits = Regex.Replace(trimmed, @"\D", "");

            if (digits.Length < 7 || digits.Length > 15)
            {
                errorMessage = "Phone number must contain between 7 and 15 digits (e.g. 09171234567 or +63 917 123 4567).";
                return false;
            }

            // If starts with Philippine local mobile prefix '09', it must be 11 digits
            if (trimmed.StartsWith("09") && digits.Length != 11)
            {
                errorMessage = "Philippine mobile numbers starting with '09' must have exactly 11 digits (e.g. 09171234567).";
                return false;
            }

            // If starts with +639, it must have 12 digits (639XXXXXXXXX)
            if ((trimmed.StartsWith("+639") || trimmed.StartsWith("639")) && digits.Length != 12)
            {
                errorMessage = "Philippine mobile numbers with +63 must have 12 digits total (e.g. +63 917 123 4567).";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
