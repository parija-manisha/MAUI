using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotesApp.Models;
using NotesApp.Services;
using Plugin.Maui.Audio;

namespace NotesApp.ViewModels;

public partial class NoteEditViewModel : ObservableObject
{
    private readonly NoteDatabase _database;
    private readonly AttachmentStorage _attachmentStorage;
    private readonly IAudioManager _audioManager;

    private Note _note = new();
    private IAudioRecorder? _audioRecorder;
    private IAudioPlayer? _audioPlayer;
    private int _checklistSortCounter;

    public ObservableCollection<ChecklistItem> ChecklistItems { get; } = new();
    public ObservableCollection<NoteAttachment> Attachments { get; } = new();

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNewNote { get; set; } = true;

    [ObservableProperty]
    public partial string LastEditedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewChecklistItemText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string RecordingStatusText { get; set; } = string.Empty;

    public NoteEditViewModel(NoteDatabase database, AttachmentStorage attachmentStorage, IAudioManager audioManager)
    {
        _database = database;
        _attachmentStorage = attachmentStorage;
        _audioManager = audioManager;
    }

    public async Task LoadAsync(int noteId)
    {
        ChecklistItems.Clear();
        Attachments.Clear();

        if (noteId > 0)
        {
            var existing = await _database.GetNoteAsync(noteId);
            if (existing is not null)
            {
                _note = existing;
                IsNewNote = false;
                Title = existing.Title;
                Content = existing.Content;
                LastEditedText = $"Last edited {existing.UpdatedAt:g}";

                foreach (var item in await _database.GetChecklistItemsAsync(noteId))
                    AddChecklistItemToCollection(item);

                foreach (var attachment in await _database.GetAttachmentsAsync(noteId))
                    Attachments.Add(attachment);

                return;
            }
        }

        _note = new Note();
        IsNewNote = true;
        Title = string.Empty;
        Content = string.Empty;
        LastEditedText = string.Empty;
    }

    private async Task EnsureNoteSavedAsync()
    {
        if (_note.Id != 0)
            return;

        _note.Title = string.IsNullOrWhiteSpace(Title) ? "Untitled" : Title.Trim();
        _note.Content = Content;
        await _database.SaveNoteAsync(_note);
        IsNewNote = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Content) &&
            ChecklistItems.Count == 0 && Attachments.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Empty note", "Write something before saving.", "OK");
            return;
        }

        _note.Title = string.IsNullOrWhiteSpace(Title) ? "Untitled" : Title.Trim();
        _note.Content = Content;

        await _database.SaveNoteAsync(_note);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsNewNote)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync("Delete note", "Delete this note and all its attachments?", "Delete", "Cancel");
        if (!confirmed)
            return;

        await _database.DeleteNoteAsync(_note);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    // Checklist

    [RelayCommand]
    private async Task AddChecklistItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewChecklistItemText))
            return;

        await EnsureNoteSavedAsync();

        var item = new ChecklistItem
        {
            NoteId = _note.Id,
            Text = NewChecklistItemText.Trim(),
            SortOrder = _checklistSortCounter++
        };

        await _database.SaveChecklistItemAsync(item);
        AddChecklistItemToCollection(item);
        NewChecklistItemText = string.Empty;
    }

    private void AddChecklistItemToCollection(ChecklistItem item)
    {
        item.PropertyChanged += OnChecklistItemPropertyChanged;
        ChecklistItems.Add(item);
    }

    private void OnChecklistItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ChecklistItem item && e.PropertyName == nameof(ChecklistItem.IsChecked))
            _ = _database.SaveChecklistItemAsync(item);
    }

    [RelayCommand]
    private async Task DeleteChecklistItemAsync(ChecklistItem? item)
    {
        if (item is null)
            return;

        item.PropertyChanged -= OnChecklistItemPropertyChanged;
        await _database.DeleteChecklistItemAsync(item);
        ChecklistItems.Remove(item);
    }

    // Image / file attachments

    [RelayCommand]
    private async Task AddImageAsync()
    {
        var choice = await Shell.Current.DisplayActionSheetAsync("Add image", "Cancel", null, "Camera", "Photo library");
        if (choice is null || choice == "Cancel")
            return;

        try
        {
            FileResult? result = choice == "Camera"
                ? await MediaPicker.Default.CapturePhotoAsync()
                : (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Choose a photo" })).FirstOrDefault();

            if (result is null)
                return;

            await SaveAttachmentFromFileResultAsync(result, AttachmentKind.Image);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlertAsync("Not supported", "This device does not support that action.", "OK");
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlertAsync("Permission needed", "Camera/photo permission was denied.", "OK");
        }
    }

    [RelayCommand]
    private async Task AddFileAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Attach a file" });
        if (result is null)
            return;

        await SaveAttachmentFromFileResultAsync(result, AttachmentKind.File);
    }

    private async Task SaveAttachmentFromFileResultAsync(FileResult result, AttachmentKind kind)
    {
        await EnsureNoteSavedAsync();

        using var stream = await result.OpenReadAsync();
        var storedFileName = await _attachmentStorage.SaveCopyAsync(stream, result.FileName);

        var attachment = new NoteAttachment
        {
            NoteId = _note.Id,
            Kind = kind,
            StoredFileName = storedFileName,
            DisplayName = result.FileName
        };

        await _database.SaveAttachmentAsync(attachment);
        Attachments.Add(attachment);
    }

    [RelayCommand]
    private async Task OpenAttachmentAsync(NoteAttachment? attachment)
    {
        if (attachment is null)
            return;

        if (attachment.Kind == AttachmentKind.Audio)
        {
            await PlayAudioAsync(attachment);
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest(attachment.DisplayName, new ReadOnlyFile(attachment.FullPath)));
    }

    [RelayCommand]
    private async Task DeleteAttachmentAsync(NoteAttachment? attachment)
    {
        if (attachment is null)
            return;

        await _database.DeleteAttachmentAsync(attachment);
        Attachments.Remove(attachment);
    }

    // Audio notes

    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            await StopRecordingAsync();
            return;
        }

        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            await Shell.Current.DisplayAlertAsync("Permission needed", "Microphone permission was denied.", "OK");
            return;
        }

        _audioRecorder = _audioManager.CreateRecorder();
        await _audioRecorder.StartAsync();
        IsRecording = true;
        RecordingStatusText = "Recording…";
    }

    private async Task StopRecordingAsync()
    {
        if (_audioRecorder is null)
            return;

        var audioSource = await _audioRecorder.StopAsync();
        IsRecording = false;
        RecordingStatusText = string.Empty;

        await EnsureNoteSavedAsync();

        using var stream = audioSource.GetAudioStream();
        var storedFileName = await _attachmentStorage.SaveCopyAsync(stream, $"voice-note-{DateTime.Now:yyyyMMdd-HHmmss}.wav");

        var attachment = new NoteAttachment
        {
            NoteId = _note.Id,
            Kind = AttachmentKind.Audio,
            StoredFileName = storedFileName,
            DisplayName = "Voice note"
        };

        await _database.SaveAttachmentAsync(attachment);
        Attachments.Add(attachment);

        _audioRecorder = null;
    }

    [RelayCommand]
    private async Task PlayAudioAsync(NoteAttachment? attachment)
    {
        if (attachment is null || !attachment.IsAudio)
            return;

        _audioPlayer?.Stop();
        _audioPlayer?.Dispose();

        _audioPlayer = _audioManager.CreatePlayer(File.OpenRead(attachment.FullPath));
        _audioPlayer.Play();
        await Task.CompletedTask;
    }
}
