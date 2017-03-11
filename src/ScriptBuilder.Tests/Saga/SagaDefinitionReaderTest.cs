﻿using System.IO;
using ApprovalTests;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaDefinitionReaderTest
{
    ModuleDefinition module;

    public SagaDefinitionReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        module = ModuleDefinition.ReadModule(path, readerParameters);
    }

    [Test]
    public void WithGeneric()
    {
        var sagaType = module.GetTypeDefinition<WithGenericSaga<int>>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        });
        Approvals.Verify(exception.Message);
    }

    [CorrelatedSaga(
        correlationProperty: nameof(SagaData.Correlation)
    )]
    public class WithGenericSaga<T> : SqlSaga<WithGenericSaga<T>.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void AlwaysStart()
    {
        var sagaType = module.GetTypeDefinition<AlwaysStartSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [AlwaysStartNewSaga]
    public class AlwaysStartSaga : SqlSaga<AlwaysStartSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }


    [Test]
    public void Abstract()
    {
        var sagaType = module.GetTypeDefinition<AbstractSaga>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        });
        Approvals.Verify(exception.Message);
    }

    [CorrelatedSaga(
        correlationProperty: nameof(SagaData.Correlation)
    )]
    abstract class AbstractSaga : SqlSaga<AbstractSaga.SagaData>
    {

        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void SqlSagaWithNoAttribute()
    {
        var sagaType = module.GetTypeDefinition<WithNoAttributeSaga>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        });
        Approvals.Verify(exception.Message);
    }

    public class WithNoAttributeSaga : SqlSaga<WithNoAttributeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void NonSqlSaga()
    {
        var sagaType = module.GetTypeDefinition<NonSqlSagaSaga>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        });
        Approvals.Verify(exception.Message);
    }

    public class NonSqlSagaSaga : Saga<NonSqlSagaSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void Simple()
    {
        var sagaType = module.GetTypeDefinition<SimpleSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [CorrelatedSaga(
        correlationProperty: nameof(SagaData.Correlation),
        TransitionalCorrelationProperty= nameof(SagaData.Transitional)
    )]
    public class SimpleSaga : SqlSaga<SimpleSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string Transitional { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void SqlSaga()
    {
        var sagaType = module.GetTypeDefinition<SimpleSqlSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [CorrelatedSaga(
        correlationProperty: nameof(SagaData.Correlation),
        TransitionalCorrelationProperty= nameof(SagaData.Transitional)
    )]
    public class SimpleSqlSaga : SqlSaga<SimpleSqlSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string Transitional { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void WithNoCorrelation()
    {
        var sagaType = module.GetTypeDefinition<WithNoCorrelationSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [AlwaysStartNewSaga]
    public class WithNoCorrelationSaga : SqlSaga<WithNoCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void WithNoTransitionalCorrelation()
    {
        var sagaType = module.GetTypeDefinition<WithNoTransitionalCorrelationSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [CorrelatedSaga(nameof(SagaData.Correlation))]
    public class WithNoTransitionalCorrelationSaga : SqlSaga<WithNoTransitionalCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void WithTableSuffix()
    {
        var sagaType = module.GetTypeDefinition<TableSuffixSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(sagaType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [CorrelatedSaga(
        correlationProperty: nameof(SagaData.Correlation),
        TableSuffix= "TheTableSuffix"
    )]
    public class TableSuffixSaga : SqlSaga<TableSuffixSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
        }
    }
}