namespace WebApp.Services.Profile
{
    // The outcome of a profile change: whether it worked, and a message the page can show the volunteer.
    public sealed record ProfileResult(bool Succeeded, string Message)
    {
        public static ProfileResult Success(string message) => new(true, message);

        public static ProfileResult Failure(string message) => new(false, message);
    }
}
