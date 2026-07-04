using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameFlowTestHarness : IDisposable
    {
        private readonly List<UnityEngine.Object> ownedScriptableObjects = new List<UnityEngine.Object>();
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

            Root = new GameObject(options.RootName ?? "MahjongGameFlowTestHarness");
            if (options.AddEventNotifier)
                EventNotifier = Root.AddComponent(types.MahjongEventNotifier);

            GameFlow = Root.AddComponent(types.MahjongGameFlow);
            ApplyOptions(options);
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
            UnityEngine.Object unityObject = scriptableObject as UnityEngine.Object;
            if (unityObject != null && !ownedScriptableObjects.Contains(unityObject))
                ownedScriptableObjects.Add(unityObject);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            for (int i = 0; i < ownedScriptableObjects.Count; i++)
            {
                if (ownedScriptableObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(ownedScriptableObjects[i]);
            }

            ownedScriptableObjects.Clear();

            if (Root != null)
                UnityEngine.Object.DestroyImmediate(Root);
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
