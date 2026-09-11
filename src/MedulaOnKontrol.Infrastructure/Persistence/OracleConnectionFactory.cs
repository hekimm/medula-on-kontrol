using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace MedulaOnKontrol.Infrastructure.Persistence;
public sealed class OracleConnectionFactory(IOptions<DatabaseOptions> options) : IOracleConnectionFactory
{
    static OracleConnectionFactory() => OracleColumnMappings.Configure();

    public async Task<OracleConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new OracleConnection(options.Value.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition("ALTER SESSION SET CURRENT_SCHEMA=MEDULA", cancellationToken: cancellationToken));
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
