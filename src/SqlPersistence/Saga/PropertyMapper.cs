﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    class PropertyMapper<TSagaData>: IMessagePropertyMapper
        where TSagaData : IContainSagaData, new()
    {
        SagaPropertyMapper<TSagaData> sagaPropertyMapper;
        Expression<Func<TSagaData, object>> correlationExpression;

        internal PropertyMapper(SagaPropertyMapper<TSagaData> sagaPropertyMapper, Type sagaType)
        {
            this.sagaPropertyMapper = sagaPropertyMapper;
            correlationExpression = GetExpression(sagaType);
        }

        internal static Expression<Func<TSagaData, object>>  GetExpression(Type sagaType)
        {
            var sagaDataType = typeof (TSagaData);
            var sqlSagaAttribute = sagaType.GetCustomAttribute<SqlSagaAttribute>();
            if (sqlSagaAttribute == null)
            {
                var message = $"Implementations of SqlSaga require a [{nameof(SqlSagaAttribute)}] to be applied.";
                throw new Exception(message);
            }
            if (sqlSagaAttribute.CorrelationProperty == null)
            {
                var message = $"When implementing a SqlSaga it is necessary to provide a CorrelationProperty via a [{nameof(SqlSagaAttribute)}]. Either provide a CorrelationProperty or inherit from {nameof(Saga)} instead.";
                throw new Exception(message);
            }
            var correlationProperty = sagaDataType
                .GetProperty(sqlSagaAttribute.CorrelationProperty, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
            if (correlationProperty == null)
            {
                var message = $"Expected to fing a property named {sqlSagaAttribute.CorrelationProperty} on  [{sagaDataType.FullName}].";
                throw new Exception(message);
            }
            var parameterExpression = Expression.Parameter(typeof (TSagaData), "s");

            var propertyExpression = Expression.Property(parameterExpression, correlationProperty);
            if (correlationProperty.PropertyType == typeof (string))
            {
                return Expression.Lambda<Func<TSagaData, object>>(propertyExpression, parameterExpression);
            }
            var convert = Expression.Convert(propertyExpression, typeof(object));
            return Expression.Lambda<Func<TSagaData, object>>(convert, parameterExpression);
        }

        public void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (correlationExpression == null)
            {
                var message = $"You are attempting to map a message property but no correlation property has been defined for '{typeof (TSagaData).FullName}'. Add a [{nameof(SqlSagaAttribute)}] to the saga.";
                throw new Exception(message);
            }
            var configureMapping = sagaPropertyMapper.ConfigureMapping(messageProperty);
            configureMapping.ToSaga(correlationExpression);
        }
    }
}