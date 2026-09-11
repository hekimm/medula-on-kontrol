using Dapper;
using FluentAssertions;
using MedulaOnKontrol.Application;
using MedulaOnKontrol.Domain;
using MedulaOnKontrol.Infrastructure;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Xunit;
using static MedulaOnKontrol.Infrastructure.Persistence.OracleCommands;

namespace MedulaOnKontrol.IntegrationTests;
[CollectionDefinition("Oracle", DisableParallelization = true)]
public sealed class OracleCollection : ICollectionFixture<OracleFixture>
{
}
