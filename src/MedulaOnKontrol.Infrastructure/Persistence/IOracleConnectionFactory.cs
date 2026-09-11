using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace MedulaOnKontrol.Infrastructure.Persistence;
public interface IOracleConnectionFactory
{
    Task<OracleConnection> OpenAsync(CancellationToken cancellationToken);
}
