// Intentionally has different namespace from folder structure due to being imported code
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace CNCMaps.FileFormats.VirtualFileSystem;
#pragma warning restore IDE0130 // Namespace does not match folder structure


/// <summary>Virtual file from a memory buffer.</summary>
public class MemoryFile : VirtualFile
{

    public MemoryFile(byte[] buffer, bool isBuffered = true) :
        base(new MemoryStream(buffer), "MemoryFile", 0, buffer.Length, isBuffered)
    { }
}