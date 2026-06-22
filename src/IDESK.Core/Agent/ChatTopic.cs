namespace IDESK.Core.Agent;

public class ChatTopic
{
    public int Id { get; set; }
    public string Name { get; set; } = "新对话";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static string GetAbbreviation(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        char first = name[0];
        if (first >= 0x4E00 && first <= 0x9FFF)
            return first.ToString();
        var letters = name.Where(char.IsLetter).Take(2).Select(char.ToUpper).ToArray();
        return letters.Length > 0 ? new string(letters) : char.ToUpper(first).ToString();
    }
}
