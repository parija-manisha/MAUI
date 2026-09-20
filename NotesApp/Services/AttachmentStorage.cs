namespace NotesApp.Services;

public class AttachmentStorage
{
    private readonly string _rootFolder;

    public AttachmentStorage()
    {
        _rootFolder = Path.Combine(FileSystem.AppDataDirectory, "Attachments");
        Directory.CreateDirectory(_rootFolder);
    }

    public string GetFullPath(string storedFileName) => Path.Combine(_rootFolder, storedFileName);

    public async Task<string> SaveCopyAsync(Stream sourceStream, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = GetFullPath(storedFileName);

        using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return storedFileName;
    }

    public void DeleteFile(string storedFileName)
    {
        var path = GetFullPath(storedFileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}
