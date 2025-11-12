using MongoDB.Bson;

namespace ApiHosterLink.Services
{
    public interface IValidationService
    {
        bool IsValidObjectId(string id);
        bool IsValidEmail(string email);
        bool IsValidPhone(string phone);
        bool IsValidAmount(decimal amount);
        bool IsValidDateRange(DateTime start, DateTime end);
    }

    public class ValidationService : IValidationService
    {
        public bool IsValidObjectId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 24)
                return false;

            // Verificar que solo contiene caracteres hexadecimales
            return id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Eliminar espacios, guiones, paréntesis
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            return cleanPhone.Length >= 10 && cleanPhone.Length <= 15;
        }

        public bool IsValidAmount(decimal amount)
        {
            return amount >= 0 && amount <= 9999999.99m; // Límite razonable
        }

        public bool IsValidDateRange(DateTime start, DateTime end)
        {
            return start < end && (end - start).TotalDays <= 365; // Máximo 1 año
        }
    }
}