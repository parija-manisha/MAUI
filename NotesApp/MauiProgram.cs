using Microsoft.Extensions.Logging;
using NotesApp.Services;
using NotesApp.ViewModels;
using NotesApp.Views;
using Plugin.Maui.Audio;

namespace NotesApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<AttachmentStorage>();
            builder.Services.AddSingleton<NoteDatabase>();

            builder.Services.AddSingleton<NotesListViewModel>();
            builder.Services.AddSingleton<NotesListPage>();

            builder.Services.AddTransient<NoteEditViewModel>();
            builder.Services.AddTransient<NoteEditPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
