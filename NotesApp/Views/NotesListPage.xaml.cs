using NotesApp.ViewModels;

namespace NotesApp.Views;

public partial class NotesListPage : ContentPage
{
    private readonly NotesListViewModel _viewModel;

    public NotesListPage(NotesListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadNotesCommand.ExecuteAsync(null);
    }
}
