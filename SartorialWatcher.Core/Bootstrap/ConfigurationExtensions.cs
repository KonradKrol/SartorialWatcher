using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SartorialWatcher.Core.Bootstrap;

public static class ConfigurationExtensions
{
    public static async Task<IConfigurationBuilder> AddAppConfigurationAsync(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            var secretName =
                Environment.GetEnvironmentVariable("SECRET_NAME")
                ?? throw new InvalidOperationException();

            var secretsClient = new AmazonSecretsManagerClient();

            var secretResponse =
                await secretsClient.GetSecretValueAsync(
                    new GetSecretValueRequest
                    {
                        SecretId = secretName
                    });

            var secrets =
                JsonSerializer.Deserialize<Dictionary<string, string?>>(
                    secretResponse.SecretString!)
                ?? throw new InvalidOperationException("Secrets deserialization failed");

            configurationBuilder.AddInMemoryCollection(secrets);
        }

        return configurationBuilder;
    }

    public static IConfigurationBuilder AddAppConfiguration(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            var secretName =
                Environment.GetEnvironmentVariable("SECRET_NAME")
                ?? throw new InvalidOperationException();

            var secretsClient = new AmazonSecretsManagerClient();

            var secretResponse =
                secretsClient.GetSecretValueAsync(
                    new GetSecretValueRequest
                    {
                        SecretId = secretName
                    }).GetAwaiter().GetResult();

            var secrets =
                JsonSerializer.Deserialize<Dictionary<string, string?>>(
                    secretResponse.SecretString!)
                ?? throw new InvalidOperationException("Secrets deserialization failed");

            configurationBuilder.AddInMemoryCollection(secrets);
        }

        return configurationBuilder;
    }
}