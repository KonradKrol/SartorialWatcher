using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace SartorialWatcher.Lambda;

public class LambdaHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "SartorialWatcher";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; }
        = new NullFileProvider();
}