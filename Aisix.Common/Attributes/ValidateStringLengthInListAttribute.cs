using System.ComponentModel.DataAnnotations;

namespace Aisix.Common.Attributes
{
    public class ValidateStringLengthInListAttribute : ValidationAttribute
    {
        private readonly int _maxLength;

        public ValidateStringLengthInListAttribute(int maxLength)
        {
            _maxLength = maxLength;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var list = value as List<string>;
            if (list != null)
            {
                foreach (var str in list)
                {
                    if (str.Length > _maxLength)
                    {
                        var memberName = validationContext.DisplayName ?? validationContext.MemberName;
                        return new ValidationResult(ErrorMessage ?? $"Each string in the list {memberName} must be less than or equal to {_maxLength} characters.");
                    }
                }
            }
            return ValidationResult.Success;
        }
    }
}
