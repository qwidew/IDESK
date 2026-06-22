namespace IDESK.Core.Agent;

public class ChatMessage
{
    public string Role { get; set; } = "user";   // "user" | "assistant"
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
