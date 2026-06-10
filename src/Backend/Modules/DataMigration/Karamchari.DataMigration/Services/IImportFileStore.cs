// -----------------------------------------------------------------------
// <copyright file="IImportFileStore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Abstraction for storing and retrieving import files.
/// </summary>
public interface IImportFileStore
{
    Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task<Stream> GetFileAsync(string fileId, CancellationToken ct = default);
    Task DeleteFileAsync(string fileId, CancellationToken ct = default);
}

/// <summary>
/// Local file system implementation of <see cref="IImportFileStore"/> for development.
/// </summary>
public sealed class LocalImportFileStore : IImportFileStore
{
    private readonly string _storagePath;

    public LocalImportFileStore(string storagePath)
    {
        _storagePath = storagePath;
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var fileId = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
        var path = Path.Combine(_storagePath, fileId);
        using var fileStream = File.Create(path);
        await stream.CopyToAsync(fileStream, ct);
        return fileId;
    }

    public Task<Stream> GetFileAsync(string fileId, CancellationToken ct = default)
    {
        var path = Path.Combine(_storagePath, fileId);
        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    public Task DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        var path = Path.Combine(_storagePath, fileId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}
