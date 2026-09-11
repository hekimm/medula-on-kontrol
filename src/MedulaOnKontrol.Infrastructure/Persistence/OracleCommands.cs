using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace MedulaOnKontrol.Infrastructure.Persistence;
public static class OracleCommands
{
    public static CommandDefinition CreateCommand(string sql, object? parameters, CancellationToken cancellationToken, IDbTransaction? transaction = null) => new(sql, parameters, transaction, commandTimeout: 120, cancellationToken: cancellationToken);
    public static async Task<long> InsertIdAsync(OracleConnection connection, string sql, object parameters, CancellationToken cancellationToken, OracleTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var property in parameters.GetType().GetProperties())
        {
            var value = property.GetValue(parameters);
            if (value is Enum enumValue)
                value = Convert.ToInt32(enumValue);
            if (value is bool isValid)
                value = isValid ? 1 : 0;
            command.Parameters.Add(new OracleParameter(property.Name, value ?? DBNull.Value));
        }

        var output = new OracleParameter("NewId", OracleDbType.Int64, ParameterDirection.Output);
        command.Parameters.Add(output);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return long.Parse(output.Value.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task BulkAsync<T>(OracleConnection connection, string sql, IReadOnlyList<T> rows, CancellationToken cancellationToken, OracleTransaction? transaction = null)
    {
        if (rows.Count == 0)
            return;
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.BindByName = true;
        command.ArrayBindCount = rows.Count;
        command.Transaction = transaction;
        var names = Regex.Matches(sql, @":([A-Za-z][A-Za-z0-9]*)").Select(match => match.Groups[1].Value).Distinct();
        foreach (var name in names)
        {
            var property = typeof(T).GetProperty(name) ?? throw new InvalidOperationException("Eksik bind alanı: " + name);
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var dbType = type == typeof(string) ? OracleDbType.Varchar2 : type == typeof(DateTime) ? OracleDbType.TimeStamp : type == typeof(decimal) ? OracleDbType.Decimal : OracleDbType.Int64;
            var values = rows.Select(row =>
            {
                var propertyValue = property.GetValue(row);
                return propertyValue is Enum enumValue ? Convert.ToInt64(enumValue) : propertyValue is bool isValid ? (isValid ? 1 : 0) : propertyValue ?? DBNull.Value;
            }).ToArray();
            command.Parameters.Add(new OracleParameter(name, dbType) { Value = values });
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
