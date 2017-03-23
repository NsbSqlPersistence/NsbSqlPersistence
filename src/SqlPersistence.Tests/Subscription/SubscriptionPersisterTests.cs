using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using ObjectApproval;

public abstract class SubscriptionPersisterTests
{

    BuildSqlVariant sqlVariant;
    string schema;
    Func<DbConnection> dbConnection;
    protected abstract Func<DbConnection> GetConnection();
    SubscriptionPersister persister;

    public SubscriptionPersisterTests(BuildSqlVariant sqlVariant, string schema)
    {
        this.sqlVariant = sqlVariant;
        this.schema = schema;
    }

    [SetUp]
    public void Setup()
    {
        dbConnection = GetConnection();
        persister = new SubscriptionPersister(
            connectionBuilder: dbConnection,
            tablePrefix: $"{nameof(SubscriptionPersisterTests)}_",
            sqlVariant: sqlVariant.Convert(),
            schema: schema,
            cacheFor: TimeSpan.FromSeconds(10)
        );
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), nameof(SubscriptionPersisterTests), schema: schema);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlVariant), nameof(SubscriptionPersisterTests), schema: schema);
        }
    }

    [TearDown]
    public void TearDown()
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), nameof(SubscriptionPersisterTests), schema: schema);
        }
    }

    [Test]
    public void Subscribe()
    {
        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        var type2 = new MessageType("type2", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            type1,
            type2,
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine2", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine2", "endpoint"), type2, null).Await();
        var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result;
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public async Task Cached_get_should_be_faster()
    {
        var type = new MessageType("type1", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            type,
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type, null).Await();
        var first = Stopwatch.StartNew();
        var subscribersFirst = await persister.GetSubscriberAddressesForMessage(messageTypes, null)
            .ConfigureAwait(false);
        var firstTime = first.ElapsedMilliseconds;
        var second = Stopwatch.StartNew();
        var subscribersSecond = await persister.GetSubscriberAddressesForMessage(messageTypes, null)
            .ConfigureAwait(false);
        var secondTime = second.ElapsedMilliseconds;
        Assert.IsTrue(secondTime * 1000 < firstTime);
        Assert.AreEqual(subscribersFirst.Count(), subscribersSecond.Count());
    }

    [Test]
    public void Should_be_cached()
    {
        var type = new MessageType("type1", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            type,
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type, null).Await();
        persister.GetSubscriberAddressesForMessage(messageTypes, null).Await();
        VerifyCache(persister.Cache);
    }

    static void VerifyCache(ConcurrentDictionary<string, SubscriptionPersister.CacheItem> cache)
    {
        var items = cache.Values.Select(_=>_.Subscribers.Result);
        ObjectApprover.VerifyWithJson(items);
    }

    [Test]
    public void Subscribe_with_same_type_should_clear_cache()
    {
        var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            matchingType
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        persister.GetSubscriberAddressesForMessage(messageTypes, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        VerifyCache(persister.Cache);
    }

    [Test]
    public void Unsubscribe_with_same_type_should_clear_cache()
    {
        var matchingType = new MessageType("matchingType", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            matchingType
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        persister.GetSubscriberAddressesForMessage(messageTypes, null).Await();
        persister.Unsubscribe(new Subscriber("e@machine1", "endpoint"), matchingType, null).Await();
        VerifyCache(persister.Cache);
    }

    [Test]
    public void Subscribe_duplicate_add()
    {
        var type1 = new MessageType("type1", new Version(0, 0, 0, 0));
        var type2 = new MessageType("type2", new Version(0, 0, 0, 0));
        var messageTypes = new List<MessageType>
        {
            type1,
            type2,
        };
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type1, null).Await();
        persister.Subscribe(new Subscriber("e@machine1", "endpoint"), type2, null).Await();
        var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result;
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void Unsubscribe()
    {
        var message2 = new MessageType("type2", new Version(0, 0));
        var message1 = new MessageType("type1", new Version(0, 0));
        var messageTypes = new List<MessageType>
        {
            message2,
            message1,
        };
        var address1 = new Subscriber("address1@machine1", "endpoint");
        persister.Subscribe(address1, message2, null).Await();
        persister.Subscribe(address1, message1, null).Await();
        var address2 = new Subscriber("address2@machine2", "endpoint");
        persister.Subscribe(address2, message2, null).Await();
        persister.Subscribe(address2, message1, null).Await();
        persister.Unsubscribe(address1, message2, null).Await();
        var result = persister.GetSubscriberAddressesForMessage(messageTypes, null).Result;
        ObjectApprover.VerifyWithJson(result);
    }

}