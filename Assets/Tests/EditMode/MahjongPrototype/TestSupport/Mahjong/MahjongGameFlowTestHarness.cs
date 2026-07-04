using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Unity;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameFlowTestHarness : IDisposable
    {
        private readonly UnityObjectTestOwner owner = new UnityObjectTestOwner();
        private bool disposed;

        private MahjongGameFlowTestHarness(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            MahjongGameFlowTestOptions options)
        {
            Reflection = reflection;
            Collections = collections;
            Types = types;
            DataFactory = dataFactory;

            try
            {
                Root = owner.Own(new GameObject(options.RootName ?? "MahjongGameFlowTestHarness"));
                if (options.AddEventNotifier)
                    EventNotifier = Root.AddComponent(types.MahjongEventNotifier);

                GameFlow = Root.AddComponent(types.MahjongGameFlow);
                ApplyOptions(options);
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        public GameObject Root { get; }
        public object GameFlow { get; }
        public object EventNotifier { get; }
        public ReflectionTestAccess Reflection { get; }
        public CollectionTestAccess Collections { get; }
        public MahjongTestTypes Types { get; }
        public MahjongTestDataFactory DataFactory { get; }

        public object CurrentState => Reflection.GetProperty(GameFlow, "CurrentState");

        public static MahjongGameFlowTestHarness Create(MahjongGameFlowTestOptions options)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return Create(options, reflection, collections, types, dataFactory);
        }

        public static MahjongGameFlowTestHarness Create(
            MahjongGameFlowTestOptions options,
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory)
        {
            return new MahjongGameFlowTestHarness(
                reflection,
                collections,
                types,
                dataFactory,
                options ?? new MahjongGameFlowTestOptions());
        }

        public void RegisterOwnedScriptableObject(object scriptableObject)
        {
            owner.Register(scriptableObject);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            owner.Dispose();
        }

        private void ApplyOptions(MahjongGameFlowTestOptions options)
        {
            SetIfPresent("logWarnings", options.LogWarnings);
            SetIfPresent("participantCount", options.ParticipantCount);
            SetIfPresent("initialHandTileCount", options.InitialHandTileCount);
            SetIfPresent("autoStart", options.AutoStart);
            SetIfPresent("useFixedRandomSeed", options.UseFixedRandomSeed);
            SetIfPresent("fixedRandomSeed", options.FixedRandomSeed);
            SetIfPresent("enableAutoDraw", options.EnableAutoDraw);
            SetIfPresent("autoDiscardDrawnTileDelaySeconds", options.AutoDiscardDrawnTileDelaySeconds);
            SetIfPresent("randomizeSelfSeat", options.RandomizeSelfSeat);

            if (options.FixedSelfSeatName != null)
                Reflection.SetPrivateField(
                    GameFlow,
                    "fixedSelfSeat",
                    DataFactory.ParseSeat(options.FixedSelfSeatName));

            if (options.YakuDefinitionCatalog != null)
                Reflection.SetPrivateField(
                    GameFlow,
                    "yakuDefinitionCatalog",
                    options.YakuDefinitionCatalog);
        }

        private void SetIfPresent(string fieldName, bool? value)
        {
            if (value.HasValue)
                Reflection.SetPrivateField(GameFlow, fieldName, value.Value);
        }

        private void SetIfPresent(string fieldName, int? value)
        {
            if (value.HasValue)
                Reflection.SetPrivateField(GameFlow, fieldName, value.Value);
        }

        private void SetIfPresent(string fieldName, float? value)
        {
            if (value.HasValue)
                Reflection.SetPrivateField(GameFlow, fieldName, value.Value);
        }
    }
}
