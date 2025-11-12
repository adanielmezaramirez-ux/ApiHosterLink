namespace ApiHosterLink.Helpers
{
    public static class SecurityHelper
    {
        // Sanitizar inputs para prevenir inyección
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remover o escapar caracteres peligrosos
            var dangerousChars = new[] { '$', '{', '}', '[', ']' };
            foreach (var dangerousChar in dangerousChars)
            {
                input = input.Replace(dangerousChar.ToString(), "");
            }

            return input.Trim();
        }

        // Validar y limpiar email
        public static string SanitizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return email;

            return email.Trim().ToLowerInvariant();
        }

        // Validar fortaleza de contraseña
        public static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            // Al menos una mayúscula, una minúscula, un número y un carácter especial
            return password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}