using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotesApp.Models;
using NotesApp.Services;
using NotesApp.Views;

namespace NotesApp.ViewModels;

public partial class NotesListViewModel : ObservableObject
{
    private readonly NoteDatabase _database;
    private List<Note> _allNotes = new();

    public ObservableCollection<Note> Notes { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public NotesListViewModel(NoteDatabase database)
    {
        _database = database;
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        IsRefreshing = true;
        _allNotes = await _database.GetNotesWithSummariesAsync();
        ApplyFilter();
        IsRefreshing = false;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allNotes
            : _allNotes.Where(n =>
                n.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Notes.Clear();
        foreach (var note in filtered)
            Notes.Add(note);

        IsEmpty = Notes.Count == 0;
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        await Shell.Current.GoToAsync(nameof(NoteEditPage));
    }

    [RelayCommand]
    private async Task SelectNoteAsync(Note? note)
    {
        if (note is null)
            return;

        await Shell.Current.GoToAsync($"{nameof(NoteEditPage)}?noteId={note.Id}");
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(Note? note)
    {
        if (note is null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Delete note", $"Delete \"{note.Title}\"?", "Delete", "Cancel");
        if (!confirmed)
            return;

        await _database.DeleteNoteAsync(note);
        _allNotes.Remove(note);
        ApplyFilter();
    }
}
