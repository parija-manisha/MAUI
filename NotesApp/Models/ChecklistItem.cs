using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace NotesApp.Models;

public partial class ChecklistItem : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int NoteId { get; set; }

    public int SortOrder { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}
