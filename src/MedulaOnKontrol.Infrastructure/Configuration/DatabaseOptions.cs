using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace MedulaOnKontrol.Infrastructure.Configuration;
public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
    public string OwnerConnectionString { get; set; } = "";
    public string SecretDirectory { get; set; } = "secrets";
    public bool Seed { get; set; } = true;
}
