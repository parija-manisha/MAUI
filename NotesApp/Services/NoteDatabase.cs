using NotesApp.Models;
using SQLite;

namespace NotesApp.Services;

public class NoteDatabase
{
    private readonly AttachmentStorage _attachmentStorage;
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public NoteDatabase(AttachmentStorage attachmentStorage)
    {
        _attachmentStorage = attachmentStorage;
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_database is not null)
            return _database;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.CreateTableAsync<Note>();
        await _database.CreateTableAsync<ChecklistItem>();
        await _database.CreateTableAsync<NoteAttachment>();
        return _database;
    }

    // Notes

    public async Task<List<Note>> GetNotesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>().OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public async Task<List<Note>> GetNotesWithSummariesAsync()
    {
        var notes = await GetNotesAsync();
        if (notes.Count == 0)
            return notes;

        var db = await GetConnectionAsync();
        var checklistCounts = (await db.Table<ChecklistItem>().ToListAsync())
            .GroupBy(c => c.NoteId)
            .ToDictionary(g => g.Key, g => g.Count());
        var attachmentKinds = (await db.Table<NoteAttachment>().ToListAsync())
            .GroupBy(a => a.NoteId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Kind).ToHashSet());

        foreach (var note in notes)
        {
            note.HasChecklist = checklistCounts.ContainsKey(note.Id);

            if (attachmentKinds.TryGetValue(note.Id, out var kinds))
            {
                note.HasImages = kinds.Contains(AttachmentKind.Image);
                note.HasAudio = kinds.Contains(AttachmentKind.Audio);
                note.HasFiles = kinds.Contains(AttachmentKind.File);
            }
        }

        return notes;
    }

    public async Task<Note?> GetNoteAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>().Where(n => n.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveNoteAsync(Note note)
    {
        var db = await GetConnectionAsync();
        note.UpdatedAt = DateTime.Now;

        if (note.Id != 0)
            await db.UpdateAsync(note);
        else
            await db.InsertAsync(note);
    }

    public async Task DeleteNoteAsync(Note note)
    {
        var db = await GetConnectionAsync();

        var attachments = await GetAttachmentsAsync(note.Id);
        foreach (var attachment in attachments)
            _attachmentStorage.DeleteFile(attachment.StoredFileName);

        await db.Table<NoteAttachment>().Where(a => a.NoteId == note.Id).DeleteAsync();
        await db.Table<ChecklistItem>().Where(c => c.NoteId == note.Id).DeleteAsync();
        await db.DeleteAsync(note);
    }

    // Checklist items

    public async Task<List<ChecklistItem>> GetChecklistItemsAsync(int noteId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<ChecklistItem>()
            .Where(c => c.NoteId == noteId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task SaveChecklistItemAsync(ChecklistItem item)
    {
        var db = await GetConnectionAsync();

        if (item.Id != 0)
            await db.UpdateAsync(item);
        else
            await db.InsertAsync(item);
    }

    public async Task DeleteChecklistItemAsync(ChecklistItem item)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(item);
    }

    // Attachments

    public async Task<List<NoteAttachment>> GetAttachmentsAsync(int noteId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<NoteAttachment>()
            .Where(a => a.NoteId == noteId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveAttachmentAsync(NoteAttachment attachment)
    {
        var db = await GetConnectionAsync();
        await db.InsertAsync(attachment);
    }

    public async Task DeleteAttachmentAsync(NoteAttachment attachment)
    {
        var db = await GetConnectionAsync();
        _attachmentStorage.DeleteFile(attachment.StoredFileName);
        await db.DeleteAsync(attachment);
    }
}
