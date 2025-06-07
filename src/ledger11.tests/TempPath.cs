public class TempPath : IDisposable
{
    public string Path { get; }

    public TempPath()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
        {
            // Console.WriteLine($"Deleting {Path}");
            System.IO.Directory.Delete(Path, true);
        }
    }

}