﻿namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        internal abstract Task<StorageSession> TryAdaptTransportConnection(
            TransportTransaction transportTransaction,
            ContextBag context,
            IConnectionManager connectionManager,
            Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory,
            CancellationToken cancellationToken = default);
    }
}
