﻿using System;
using NServiceBus;
using NServiceBus.Features;

class SqlOutboxFeature : Feature
{
    SqlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var connectionBuilder = settings.GetConnectionBuilder();
        var tablePrefix = settings.GetTablePrefix();
        var sqlDialect = settings.GetSqlDialect();
        var outboxPersister = new OutboxPersister(connectionBuilder, tablePrefix, sqlDialect);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);

        if (settings.GetOrDefault<bool>(DisableCleanup))
        {
            return;
        }

        context.RegisterStartupTask(b =>
        {
            var frequencyToRunCleanup = settings.GetOrDefault<TimeSpan?>(FrequencyToRunDeduplicationDataCleanup) ?? TimeSpan.FromMinutes(1);
            var timeToKeepDeduplicationData = settings.GetOrDefault<TimeSpan?>(TimeToKeepDeduplicationData) ?? TimeSpan.FromDays(7);

            return new OutboxCleaner(outboxPersister.RemoveEntriesOlderThan, b.Build<CriticalError>().Raise, timeToKeepDeduplicationData, frequencyToRunCleanup, new AsyncTimer());
        });
    }

    internal const string TimeToKeepDeduplicationData = "Persistence.Sql.Outbox.TimeToKeepDeduplicationData";
    internal const string FrequencyToRunDeduplicationDataCleanup = "Persistence.Sql.Outbox.FrequencyToRunDeduplicationDataCleanup";
    internal const string DisableCleanup = "Persistence.Sql.Outbox.DisableCleanup";
}