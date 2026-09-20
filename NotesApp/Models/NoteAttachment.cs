using SQLite;

namespace NotesApp.Models;

public enum AttachmentKind
{
    Image,
    File,
    Audio
}

public class NoteAttachment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int NoteId { get; set; }

    public AttachmentKind Kind { get; set; }

    /// <summary>Name of the copy stored under FileSystem.AppDataDirectory/Attachments.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>Original file name shown to the user.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public bool IsImage => Kind == AttachmentKind.Image;

    [Ignore]
    public bool IsAudio => Kind == AttachmentKind.Audio;

    [Ignore]
    public bool IsFile => Kind == AttachmentKind.File;

    [Ignore]
    public string FullPath => Path.Combine(FileSystem.AppDataDirectory, "Attachments", StoredFileName);
}
