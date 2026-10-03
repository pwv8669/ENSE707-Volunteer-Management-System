using System.ComponentModel.DataAnnotations;
using WebApp.Data;

namespace WebApp.Services.Profile
{
    // Form model for the "Personal details" section. The data annotations drive both the
    // on-page validation messages and the server-side check in VolunteerProfileService.
    public sealed class PersonalDetailsInput : IValidatableObject
    {
        private string? _phoneNumber;
        private string? _city;
        private string? _bio;
        private string? _emergencyContactName;
        private string? _emergencyContactPhone;

        [Required(ErrorMessage = "Enter your first name.")]
        [StringLength(ProfileFieldLimits.FirstName, ErrorMessage = "First name must be {1} characters or fewer.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [StringLength(ProfileFieldLimits.LastName, ErrorMessage = "Last name must be {1} characters or fewer.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        // Optional fields turn blank input into null, so an empty box isn't treated as an invalid phone number.
        [Phone(ErrorMessage = "Enter a valid phone number, e.g. 021 123 4567.")]
        [StringLength(ProfileFieldLimits.PhoneNumber, ErrorMessage = "Phone number must be {1} characters or fewer.")]
        [Display(Name = "Phone number")]
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => _phoneNumber = Clean(value);
        }

        [StringLength(ProfileFieldLimits.City, ErrorMessage = "City or suburb must be {1} characters or fewer.")]
        [Display(Name = "City or suburb")]
        public string? City
        {
            get => _city;
            set => _city = Clean(value);
        }

        [StringLength(ProfileFieldLimits.Bio, ErrorMessage = "About me must be {1} characters or fewer.")]
        [Display(Name = "About me")]
        public string? Bio
        {
            get => _bio;
            set => _bio = Clean(value);
        }

        [StringLength(ProfileFieldLimits.EmergencyContactName, ErrorMessage = "Emergency contact name must be {1} characters or fewer.")]
        [Display(Name = "Emergency contact name")]
        public string? EmergencyContactName
        {
            get => _emergencyContactName;
            set => _emergencyContactName = Clean(value);
        }

        [Phone(ErrorMessage = "Enter a valid phone number for your emergency contact.")]
        [StringLength(ProfileFieldLimits.PhoneNumber, ErrorMessage = "Emergency contact phone must be {1} characters or fewer.")]
        [Display(Name = "Emergency contact phone")]
        public string? EmergencyContactPhone
        {
            get => _emergencyContactPhone;
            set => _emergencyContactPhone = Clean(value);
        }

        // Rules that involve more than one field.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasName = EmergencyContactName is not null;
            bool hasPhone = EmergencyContactPhone is not null;

            // An emergency contact is only useful with both a name and a number.
            if (hasName && !hasPhone)
            {
                yield return new ValidationResult(
                    "Add a phone number for your emergency contact.",
                    [nameof(EmergencyContactPhone)]);
            }
            else if (hasPhone && !hasName)
            {
                yield return new ValidationResult(
                    "Add a name for your emergency contact.",
                    [nameof(EmergencyContactName)]);
            }
        }

        // A copy for the edit form, so Cancel can throw away changes without touching the saved values.
        public PersonalDetailsInput Clone() => (PersonalDetailsInput)MemberwiseClone();

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
