using NotesApp.ViewModels;

namespace NotesApp.Views;

[QueryProperty(nameof(NoteId), "noteId")]
public partial class NoteEditPage : ContentPage
{
    private readonly NoteEditViewModel _viewModel;
    private string? _noteId;

    public string NoteId
    {
        get => _noteId ?? string.Empty;
        set
        {
            _noteId = value;
            if (int.TryParse(value, out var id))
                _ = _viewModel.LoadAsync(id);
        }
    }

    public NoteEditPage(NoteEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (string.IsNullOrEmpty(NoteId))
            _ = _viewModel.LoadAsync(0);
    }
}
