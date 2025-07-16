public class ChatInfo
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Username { get; set; }
    public long AddedByUserId { get; set; }
    public DateTime DateAdded { get; set; }
}
