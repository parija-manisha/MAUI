using SQLite;

namespace NotesApp.Models;

public class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public bool HasChecklist { get; set; }

    [Ignore]
    public bool HasImages { get; set; }

    [Ignore]
    public bool HasAudio { get; set; }

    [Ignore]
    public bool HasFiles { get; set; }
}
