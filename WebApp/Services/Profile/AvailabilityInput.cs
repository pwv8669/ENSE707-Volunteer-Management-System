using System.ComponentModel.DataAnnotations;
using WebApp.Data;

namespace WebApp.Services.Profile
{
    // Form model for adding or editing one availability window.
    public sealed class AvailabilityInput : IValidatableObject
    {
        // The longest a single window can be. Catches typos like picking next year instead of next week.
        public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(31);

        private string? _note;

        [Required(ErrorMessage = "Choose when your availability starts.")]
        [Display(Name = "From")]
        public DateTime? StartsAt { get; set; }

        [Required(ErrorMessage = "Choose when your availability ends.")]
        [Display(Name = "Until")]
        public DateTime? EndsAt { get; set; }

        [StringLength(ProfileFieldLimits.AvailabilityNote, ErrorMessage = "Keep the note to {1} characters or fewer.")]
        [Display(Name = "Note (optional)")]
        public string? Note
        {
            get => _note;
            set => _note = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // Rules that compare the two times. Whether the time is in the past is checked by
        // VolunteerProfileService, because that depends on the current time.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartsAt is null || EndsAt is null)
            {
                yield break;
            }

            if (EndsAt <= StartsAt)
            {
                yield return new ValidationResult(
                    "The end time must be after the start time.",
                    [nameof(EndsAt)]);
            }
            else if (EndsAt.Value - StartsAt.Value > MaxDuration)
            {
                yield return new ValidationResult(
                    $"A single availability window can't be longer than {MaxDuration.TotalDays:0} days.",
                    [nameof(EndsAt)]);
            }
        }
    }
}
