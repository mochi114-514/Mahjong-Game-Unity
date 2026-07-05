using System;
using MahjongPrototype.Tests.TestSupport.Core;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameFlowTestSession : IDisposable
    {
        private readonly MahjongGameFlowTestHarness harness;
        private bool disposed;

        private MahjongGameFlowTestSession(MahjongGameFlowTestHarness harness)
        {
            this.harness = harness;
            Query = MahjongGameStateTestQuery.ForHarness(harness);
            Commands = new MahjongGameFlowTestCommands(harness);
        }

        public object GameFlow => harness.GameFlow;
        public object EventNotifier => harness.EventNotifier;
        public object GameLogRecorder => harness.GameLogRecorder;
        public object CurrentState => harness.CurrentState;
        public ReflectionTestAccess Reflection => harness.Reflection;
        public CollectionTestAccess Collections => harness.Collections;
        public MahjongTestTypes Types => harness.Types;
        public MahjongTestDataFactory DataFactory => harness.DataFactory;
        public MahjongGameStateTestQuery Query { get; }
        public MahjongGameFlowTestCommands Commands { get; }

        public static MahjongGameFlowTestSession Create(MahjongGameFlowTestOptions options)
        {
            return new MahjongGameFlowTestSession(MahjongGameFlowTestHarness.Create(options));
        }

        public static MahjongGameFlowTestSession Create(
            MahjongGameFlowTestOptions options,
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory)
        {
            return new MahjongGameFlowTestSession(
                MahjongGameFlowTestHarness.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory));
        }

        public void RegisterOwnedScriptableObject(object value)
        {
            harness.RegisterOwnedScriptableObject(value);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            harness.Dispose();
        }
    }
}
