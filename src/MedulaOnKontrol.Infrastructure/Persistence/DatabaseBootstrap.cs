using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.Infrastructure.Persistence;
public sealed class DatabaseBootstrap(IOptions<DatabaseOptions> options, SentetikVeriSeeder seed, ILogger<DatabaseBootstrap> logger)
{
    public async Task RunAsync(string contentRoot, CancellationToken cancellationToken)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        await using var connection = new OracleConnection(options.Value.OwnerConnectionString);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                break;
            }
            catch (OracleException) when (attempt < 30)
            {
                await Task.Delay(2000, cancellationToken);
            }
        }

        var root = Path.Combine(contentRoot, "db");
        foreach (var file in Directory.GetFiles(Path.Combine(root, "migrations"), "*.sql").Order().Concat(Directory.GetFiles(Path.Combine(root, "packages"), "*.sql").Order()))
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            foreach (var statement in Regex.Split(text, @"^\s*/\s*$", RegexOptions.Multiline).Where(textValue => !string.IsNullOrWhiteSpace(textValue)))
                await connection.ExecuteAsync(CreateCommand(statement.Trim(), null, cancellationToken));
            logger.LogInformation("Veritabanı betiği uygulandı: {Script}", Path.GetFileName(file));
        }

        var errors = (await connection.QueryAsync<string>(CreateCommand("SELECT NAME||': '||TEXT FROM USER_ERRORS WHERE TYPE IN ('PACKAGE','PACKAGE BODY','TRIGGER')", null, cancellationToken))).ToList();
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));
        if (options.Value.Seed)
            await seed.RunAsync(connection, contentRoot, cancellationToken);
    }
}
