using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Data
{
    // An account in the system. ASP.NET Core Identity supplies the login fields
    // (email, password hash, phone number, ...); the properties below add the
    // volunteer profile. They are all optional in the database so that accounts
    // created before the profile feature existed still load without changes.
    public class ApplicationUser : IdentityUser
    {
        // Personal information. [PersonalData] includes these in Identity's
        // "Download personal data" export and its delete-account flow.
        [PersonalData]
        [MaxLength(ProfileFieldLimits.FirstName)]
        public string? FirstName { get; set; }

        [PersonalData]
        [MaxLength(ProfileFieldLimits.LastName)]
        public string? LastName { get; set; }

        [PersonalData]
        [MaxLength(ProfileFieldLimits.City)]
        public string? City { get; set; }

        [PersonalData]
        [MaxLength(ProfileFieldLimits.Bio)]
        public string? Bio { get; set; }

        [PersonalData]
        [MaxLength(ProfileFieldLimits.EmergencyContactName)]
        public string? EmergencyContactName { get; set; }

        [PersonalData]
        [MaxLength(ProfileFieldLimits.PhoneNumber)]
        public string? EmergencyContactPhone { get; set; }

        // Skills and interests are stored as rows in VolunteerProfileTags (see VolunteerProfileTag).
    }
}
