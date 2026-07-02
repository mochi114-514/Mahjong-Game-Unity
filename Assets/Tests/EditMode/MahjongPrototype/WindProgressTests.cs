using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class WindProgressTests
    {
        private const string RoundWindTypeName =
            "MahjongPrototype.Domain.RoundWind, Assembly-CSharp";
        private const string WindProgressTypeName =
            "MahjongPrototype.Domain.WindProgress, Assembly-CSharp";
        private const string WallTypeName =
            "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName =
            "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string MahjongGameFlowTypeName =
            "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string SeatIdTypeName =
            "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WinTypeTypeName =
            "MahjongPrototype.Domain.WinType, Assembly-CSharp";
        private const string TableCenterTextPresenterTypeName =
            "MahjongPrototype.UI3D.MahjongTableCenterTextPresenter, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [Test]
        public void East1_ReturnsEastFirstHand()
        {
            object east1 = GetEast1();

            AssertWindProgress(east1, "East", 1);
        }

        [Test]
        public void TryGetNext_AdvancesWithinEastRound()
        {
            object east1 = GetEast1();

            bool hasNext = TryGetNext(east1, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(next, "East", 2);
        }

        [Test]
        public void TryGetNext_AdvancesEastFourToSouthOne()
        {
            object east4 = CreateWindProgress("East", 4);

            bool hasNext = TryGetNext(east4, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(next, "South", 1);
        }

        [Test]
        public void TryGetNext_AdvancesSouthThreeToSouthFour()
        {
            object south3 = CreateWindProgress("South", 3);

            bool hasNext = TryGetNext(south3, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(next, "South", 4);
        }

        [Test]
        public void TryGetNext_ReturnsFalseAfterSouthFour()
        {
            object south4 = CreateWindProgress("South", 4);

            bool hasNext = TryGetNext(south4, out object next);

            Assert.That(hasNext, Is.False);
            AssertWindProgress(next, "South", 4);
        }

        [Test]
        public void Constructor_RejectsHandNumberBelowOne()
        {
            AssertInvalidHandNumberThrowsOutOfRange(0);
        }

        [Test]
        public void Constructor_RejectsHandNumberAboveFour()
        {
            AssertInvalidHandNumberThrowsOutOfRange(5);
        }

        [Test]
        public void MahjongGameState_DefaultConstructorUsesEastOne()
        {
            object state = CreateGameState();

            AssertWindProgress(GetProperty(state, "WindProgress"), "East", 1);
        }

        [Test]
        public void MahjongGameState_WindProgressConstructorStoresProgress()
        {
            object state = CreateGameState(CreateWindProgress("South", 3));

            AssertWindProgress(GetProperty(state, "WindProgress"), "South", 3);
        }

        [Test]
        public void MahjongGameFlow_StartNewRoundUsesEastOne()
        {
            GameObject gameObject = new GameObject("WindProgressStartNewRoundTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);

                Invoke(gameFlow, "StartNewRound");

                object state = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(state, "WindProgress"), "East", 1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyStartsNextRound()
        {
            GameObject gameObject = new GameObject("WindProgressWallEmptyNextRoundTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");

                InvokeEndRound(gameFlow, "WallEmpty");

                object state = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(state, "WindProgress"), "East", 2);
                Assert.That(GetProperty(state, "IsRoundEnded"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyAfterSouthFourStaysRoundEnded()
        {
            GameObject gameObject = new GameObject("WindProgressSouthFourEndTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                object south4 = CreateWindProgress("South", 4);
                Invoke(gameFlow, "StartRound", south4, false);

                InvokeEndRound(gameFlow, "WallEmpty");

                object state = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(state, "WindProgress"), "South", 4);
                Assert.That(GetProperty(state, "IsRoundEnded"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_TsumoWinStartsNextRound()
        {
            GameObject gameObject = new GameObject("WindProgressTsumoWinNextRoundTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object state = GetProperty(gameFlow, "CurrentState");
                BeginWinDecisionDetailed(state, "East", "Tsumo", null);

                Invoke(gameFlow, "RequestDeclareWin");

                object nextState = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(nextState, "WindProgress"), "East", 2);
                Assert.That(GetProperty(nextState, "IsRoundEnded"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_RonWinStartsNextRound()
        {
            GameObject gameObject = new GameObject("WindProgressRonWinNextRoundTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object state = GetProperty(gameFlow, "CurrentState");
                BeginWinDecisionDetailed(state, "East", "Ron", "South");

                Invoke(gameFlow, "RequestDeclareWin");

                object nextState = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(nextState, "WindProgress"), "East", 2);
                Assert.That(GetProperty(nextState, "IsRoundEnded"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_WinAfterSouthFourStaysRoundEnded()
        {
            GameObject gameObject = new GameObject("WindProgressSouthFourWinEndTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                object south4 = CreateWindProgress("South", 4);
                Invoke(gameFlow, "StartRound", south4, false);
                object state = GetProperty(gameFlow, "CurrentState");
                BeginWinDecisionDetailed(state, "East", "Tsumo", null);

                Invoke(gameFlow, "RequestDeclareWin");

                object currentState = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(currentState, "WindProgress"), "South", 4);
                Assert.That(GetProperty(currentState, "IsRoundEnded"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MahjongGameFlow_DeclineWinDoesNotStartNextRound()
        {
            GameObject gameObject = new GameObject("WindProgressDeclineWinNoNextRoundTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object state = GetProperty(gameFlow, "CurrentState");
                BeginWinDecisionDetailed(state, "East", "Tsumo", null);

                Invoke(gameFlow, "RequestDeclineWin");

                object currentState = GetProperty(gameFlow, "CurrentState");
                AssertWindProgress(GetProperty(currentState, "WindProgress"), "East", 1);
                Assert.That(GetProperty(currentState, "IsRoundEnded"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TableCenterTextPresenter_RefreshShowsWindProgress()
        {
            GameObject root = new GameObject("WindProgressPresenterTest");
            try
            {
                Component presenter = root.AddComponent(Type.GetType(TableCenterTextPresenterTypeName, true));
                Component windProgressText = AssignAllTextReferences(root.transform, presenter);
                object state = CreateGameState(CreateWindProgress("South", 3));

                Invoke(presenter, "Refresh", state);

                Assert.That(GetProperty(windProgressText, "text"), Is.EqualTo("南三局"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TableCenterTextPresenter_ClearShowsDashForWindProgress()
        {
            GameObject root = new GameObject("WindProgressPresenterClearTest");
            try
            {
                Component presenter = root.AddComponent(Type.GetType(TableCenterTextPresenterTypeName, true));
                Component windProgressText = AssignAllTextReferences(root.transform, presenter);

                Invoke(presenter, "Refresh", new object[] { null });

                Assert.That(GetProperty(windProgressText, "text"), Is.EqualTo("-"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object GetEast1()
        {
            PropertyInfo property = GetWindProgressType().GetProperty(
                "East1",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(null);
        }

        private static object CreateWindProgress(string roundWindName, int handNumber)
        {
            Type windProgressType = GetWindProgressType();
            Type roundWindType = Type.GetType(RoundWindTypeName, true);
            ConstructorInfo constructor = windProgressType.GetConstructor(new[] { roundWindType, typeof(int) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new[] { Enum.Parse(roundWindType, roundWindName), handNumber });
        }

        private static bool TryGetNext(object progress, out object next)
        {
            MethodInfo method = GetWindProgressType().GetMethod("TryGetNext");
            Assert.That(method, Is.Not.Null);
            object[] args = { null };
            bool result = (bool)method.Invoke(progress, args);
            next = args[0];
            return result;
        }

        private static void BeginWinDecisionDetailed(
            object state,
            string seatName,
            string winTypeName,
            string sourceSeatName)
        {
            object sourceSeat = sourceSeatName == null ? null : ParseSeat(sourceSeatName);
            Invoke(
                state,
                "BeginWinDecisionDetailed",
                ParseSeat(seatName),
                ParseWinType(winTypeName),
                null,
                sourceSeat,
                GetProperty(state, "TurnIndex"));
        }

        private static object CreateGameState()
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            return Activator.CreateInstance(gameStateType, CreateWall());
        }

        private static object CreateGameState(object windProgress)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            return Activator.CreateInstance(gameStateType, CreateWall(), windProgress);
        }

        private static object CreateWall()
        {
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);
            return createWall.Invoke(null, new object[] { 12345 });
        }

        private static object AddConfiguredGameFlow(GameObject gameObject)
        {
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "participantCount", 1);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", Enum.Parse(Type.GetType(SeatIdTypeName, true), "East"));
            SetPrivateField(gameFlow, "logWarnings", false);
            return gameFlow;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object ParseWinType(string winTypeName)
        {
            return Enum.Parse(Type.GetType(WinTypeTypeName, true), winTypeName);
        }

        private static Component AssignAllTextReferences(Transform parent, Component presenter)
        {
            Component selfBottomWindText = CreateLabel(parent, "SelfBottomWind");
            Component nextLeftWindText = CreateLabel(parent, "NextLeftWind");
            Component acrossTopWindText = CreateLabel(parent, "AcrossTopWind");
            Component previousRightWindText = CreateLabel(parent, "PreviousRightWind");
            Component wallPointText = CreateLabel(parent, "WallPoint");
            Component windProgressText = CreateLabel(parent, "WindProgress");

            SetPrivateField(presenter, "selfBottomWindText", selfBottomWindText);
            SetPrivateField(presenter, "nextLeftWindText", nextLeftWindText);
            SetPrivateField(presenter, "acrossTopWindText", acrossTopWindText);
            SetPrivateField(presenter, "previousRightWindText", previousRightWindText);
            SetPrivateField(presenter, "wallPointText", wallPointText);
            SetPrivateField(presenter, "windProgressText", windProgressText);
            return windProgressText;
        }

        private static Component CreateLabel(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
        }

        private static void AssertWindProgress(object progress, string expectedRoundWind, int expectedHandNumber)
        {
            Assert.That(GetProperty(progress, "RoundWind").ToString(), Is.EqualTo(expectedRoundWind));
            Assert.That(GetProperty(progress, "HandNumber"), Is.EqualTo(expectedHandNumber));
        }

        private static void AssertInvalidHandNumberThrowsOutOfRange(int handNumber)
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => CreateWindProgress("East", handNumber));
            Assert.That(exception.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        private static Type GetWindProgressType()
        {
            return Type.GetType(WindProgressTypeName, true);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object InvokeEndRound(object target, string reason)
        {
            MethodInfo method = target.GetType().GetMethod(
                "EndRound",
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, new object[] { reason });
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
