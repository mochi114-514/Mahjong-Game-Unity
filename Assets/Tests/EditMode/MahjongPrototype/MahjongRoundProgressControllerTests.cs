using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongRoundProgressControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundProgressController, Assembly-CSharp";
        private const string WindProgressTypeName =
            "MahjongPrototype.Domain.WindProgress, Assembly-CSharp";
        private const string RoundWindTypeName =
            "MahjongPrototype.Domain.RoundWind, Assembly-CSharp";
        private const string SeatIdTypeName =
            "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [TestCase("East", 1, "East", "\u6771\u4e00\u5c40", "\u6771")]
        [TestCase("East", 2, "South", "\u6771\u4e8c\u5c40", "\u5357")]
        [TestCase("South", 4, "North", "\u5357\u56db\u5c40", "\u5317")]
        public void TryPlay_UsesRoundAndSelfWindAndSuppressesTheSameRound(
            string roundWind,
            int handNumber,
            string selfSeatName,
            string expectedRound,
            string expectedSelfWind)
        {
            GameObject host = new GameObject("RoundProgressControllerTestHost");
            host.SetActive(false);

            try
            {
                Type controllerType = RequireType(ControllerTypeName);
                GameObject root = CreateChild(host.transform, "Round Progress");
                Component roundText = CreateText(root.transform, "Round Text");
                Component myWindText = CreateText(root.transform, "My Wind Text");
                Component windText = CreateText(root.transform, "Wind Text");
                SetText(myWindText, "\u30a2\u30ca\u30bf\u306f\u3000\u3067\u3059");

                Component controller = host.AddComponent(controllerType);
                SetPrivateField(controller, "roundProgressRoot", root);
                SetPrivateField(controller, "roundText", roundText);
                SetPrivateField(controller, "myWindText", myWindText);
                SetPrivateField(controller, "windText", windText);
                host.SetActive(true);

                object progress = CreateWindProgress(roundWind, handNumber);
                object selfSeat = Enum.Parse(RequireType(SeatIdTypeName), selfSeatName);
                bool firstPlayed = (bool)controllerType
                    .GetMethod("TryPlay")
                    .Invoke(controller, new[] { progress, selfSeat });
                bool duplicatePlayed = (bool)controllerType
                    .GetMethod("TryPlay")
                    .Invoke(controller, new[] { progress, selfSeat });

                controllerType.GetMethod("ResetPlaybackHistory").Invoke(controller, null);
                bool replayedAfterReset = (bool)controllerType
                    .GetMethod("TryPlay")
                    .Invoke(controller, new[] { progress, selfSeat });

                Assert.That(firstPlayed, Is.True);
                Assert.That(duplicatePlayed, Is.False);
                Assert.That(replayedAfterReset, Is.True);
                Assert.That(Text(roundText), Is.EqualTo(expectedRound));
                Assert.That(Text(windText), Is.EqualTo(expectedSelfWind));
                Assert.That(Text(myWindText), Is.EqualTo("\u30a2\u30ca\u30bf\u306f\u3000\u3067\u3059"));
                Assert.That(root.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static object CreateWindProgress(string roundWindName, int handNumber)
        {
            object roundWind = Enum.Parse(RequireType(RoundWindTypeName), roundWindName);
            return Activator.CreateInstance(
                RequireType(WindProgressTypeName),
                new[] { roundWind, (object)handNumber });
        }

        private static Component CreateText(Transform parent, string name)
        {
            return CreateChild(parent, name)
                .AddComponent(RequireType(TextMeshProUguiTypeName));
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            return child;
        }

        private static string Text(Component text)
        {
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }

        private static void SetText(Component text, string value)
        {
            text.GetType().GetProperty("text").SetValue(text, value);
        }

        private static void SetPrivateField(Component component, string name, object value)
        {
            FieldInfo field = component.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}.");
            field.SetValue(component, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Type not found: {typeName}");
            return type;
        }
    }
}
