namespace WebApplication;

public class VolunteerNameDirectory
{
    private readonly Dictionary<Guid, string> _names = new();

    public void Load(Dictionary<Guid, string> names)
    {
        foreach (KeyValuePair<Guid, string> entry in names)
        {
            _names[entry.Key] = entry.Value;
        }
    }

    public string GetName(Guid volunteerId)
    {
        return _names.TryGetValue(volunteerId, out string? name)
            ? name
            : volunteerId.ToString();
    }
}
