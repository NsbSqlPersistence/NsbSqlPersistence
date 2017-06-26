using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the table prefix to be prepended to all Saga, Timeout, Subscription and Outbox tables.
        /// </summary>
        public static void TablePrefix(this PersistenceExtensions<SqlPersistence> configuration, string tablePrefix)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(tablePrefix), tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            configuration.GetSettings()
                .Set("SqlPersistence.TablePrefix", tablePrefix);
        }

        internal static string GetTablePrefix(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.TablePrefix", out string tablePrefix))
            {
                return tablePrefix;
            }
            var endpointName = settings.EndpointName();
            var clean = TableNameCleaner.Clean(endpointName);
            return $"{clean}_";
        }

    }
}