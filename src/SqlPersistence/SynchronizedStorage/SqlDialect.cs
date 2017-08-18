﻿namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Transport;

    public partial class SqlDialect
    {
        internal abstract Task<CompletableSynchronizedStorageSession> TryAdaptTransportConnection(TransportTransaction transportTransaction, ContextBag context, Func<DbConnection> connectionBuilder, Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory);
    }
}