﻿namespace NServiceBus.Persistence.Sql
{
    using System;

    class SerializationException : Exception
    {
        public SerializationException(Exception innerException) : base("Serialization failed", innerException)
        {
        }
        public SerializationException(string message) : base(message)
        {
        }
    }
}