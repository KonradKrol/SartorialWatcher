namespace SartorialWatcher.Core.Domain;

public interface IScrapingConfigurations
{
    List<ScrapingConfiguration> Configurations { get; }
}