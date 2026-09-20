using NotesApp.Views;

namespace NotesApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(NoteEditPage), typeof(NoteEditPage));
        }
    }
}
