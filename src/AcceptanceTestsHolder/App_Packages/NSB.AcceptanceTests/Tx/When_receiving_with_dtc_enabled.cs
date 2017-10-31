﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_dtc_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enlist_the_receive_in_the_dtc_tx()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<DTCEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.HandlerInvoked)
                .Run();

            Assert.False(context.CanEnlistPromotable, "There should exists a DTC tx");
        }

#if NET452
        [Test]
        public void Basic_assumptions_promotable_should_fail_if_durable_already_exists()
        {
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Transaction.Current.EnlistDurable(FakePromotableResourceManager.ResourceManagerId, new FakePromotableResourceManager(), EnlistmentOptions.None);
                Assert.False(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                tx.Complete();
            }
        }
#endif

        [Test]
        public void Basic_assumptions_second_promotable_should_fail()
        {
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Assert.True(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                Assert.False(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                tx.Complete();
            }
        }

        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public bool CanEnlistPromotable { get; set; }
        }

        public class DTCEndpoint : EndpointConfigurationBuilder
        {
            public DTCEndpoint()
            {
                // sql needs a TransactionScope
                EndpointSetup<DefaultServer>(c => c.ConfigureTransport().Transactions(TransportTransactionMode.TransactionScope));
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.CanEnlistPromotable = Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager());
                    Context.HandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }
        }


        public class MyMessage : ICommand
        {
        }
    }
}