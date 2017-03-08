using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerSubscriptionPersisterTests : SubscriptionPersisterTests
{
    public SqlServerSubscriptionPersisterTests() : base(BuildSqlVariant.MsSqlServer, "dbo")
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return MsSqlConnectionBuilder.Build;
    }
}