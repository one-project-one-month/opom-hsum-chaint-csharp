using HsumChaint.Infrastructure.Models;

namespace HsumChaint.Infrastructure.Validations
{
    public class UserValidation
    {
        //public bool ValidateForUserUpdate(User user,out string message)
        public bool ValidateForUserUpdate(User? user, out string errorMessage)
        {
            // bool isValid = false;

            errorMessage = string.Empty;

            if (user is null)
            {
                errorMessage = "User cannot be null.";
                return false;
            }

            if (string.IsNullOrEmpty(user.Name))
            {
                errorMessage = "User name cannot be null or empty.";
                return false;
            }

            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                errorMessage = "User phone number cannot be null or empty.";
                return false;
            }

            return true;
        }
    }
}
