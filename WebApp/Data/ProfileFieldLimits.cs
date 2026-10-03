namespace WebApp.Data
{
    // Maximum lengths for volunteer profile fields. The database columns and the
    // profile forms both use these values, so the two can never disagree.
    public static class ProfileFieldLimits
    {
        public const int FirstName = 50;
        public const int LastName = 50;
        public const int City = 80;
        public const int Bio = 500;
        public const int EmergencyContactName = 100;
        public const int PhoneNumber = 20;
        public const int AvailabilityNote = 100;
    }
}
