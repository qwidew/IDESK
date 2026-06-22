using System.ComponentModel.DataAnnotations;

namespace IDESK.Widgets.Notes.Models;

public class NoteItem
{
    [Key]
    public int Id { get; set; }
    public string Title { get; set; } = "笔记";
    public string Content { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
