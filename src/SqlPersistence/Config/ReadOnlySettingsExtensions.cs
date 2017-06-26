using System;
using NServiceBus.Persistence;
using NServiceBus.Settings;

static class ReadOnlySettingsExtensions
{
    internal static TValue GetValue<TValue, TStorageType>(this ReadOnlySettings settings, string suffix, Func<TValue> defaultValue)
        where TStorageType : StorageType
    {
        var key = $"SqlPersistence.{typeof(TStorageType).Name}.{suffix}";
        if (settings.TryGet(key, out TValue value))
        {
            return value;
        }
        if (settings.TryGet($"SqlPersistence.{suffix}", out value))
        {
            return value;
        }
        return defaultValue();
    }
}