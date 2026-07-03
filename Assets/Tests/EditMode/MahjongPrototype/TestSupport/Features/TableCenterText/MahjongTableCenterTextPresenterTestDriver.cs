using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Features.TableCenterText
{
    internal sealed class MahjongTableCenterTextPresenterTestDriver : IDisposable
    {
        private const string TableCenterTextPresenterTypeName =
            "MahjongPrototype.UI3D.MahjongTableCenterTextPresenter, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly GameObject root;
        private readonly Component presenter;
        private readonly Component windProgressText;
        private bool disposed;

        private MahjongTableCenterTextPresenterTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            GameObject root,
            Component presenter,
            Component windProgressText)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.root = root;
            this.presenter = presenter;
            this.windProgressText = windProgressText;
        }

        public static MahjongTableCenterTextPresenterTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("MahjongTableCenterTextPresenterTest");

            try
            {
                Component presenter = root.AddComponent(
                    reflection.RequireType(TableCenterTextPresenterTypeName));
                Component windProgressText = AssignAllTextReferences(
                    reflection,
                    root.transform,
                    presenter);

                return new MahjongTableCenterTextPresenterTestDriver(
                    reflection,
                    dataFactory,
                    root,
                    presenter,
                    windProgressText);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        public string WindProgressText =>
            (string)reflection.GetProperty(windProgressText, "text");

        public void Refresh(string roundWindName, int handNumber)
        {
            object windProgress = dataFactory.CreateWindProgress(roundWindName, handNumber);
            object state = dataFactory.CreateGameStateWithWindProgress(windProgress);
            reflection.Invoke(presenter, "Refresh", state);
        }

        public void RefreshNull()
        {
            reflection.Invoke(presenter, "Refresh", new object[] { null });
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        private static Component AssignAllTextReferences(
            ReflectionTestAccess reflection,
            Transform parent,
            Component presenter)
        {
            Component selfBottomWindText = CreateLabel(reflection, parent, "SelfBottomWind");
            Component nextLeftWindText = CreateLabel(reflection, parent, "NextLeftWind");
            Component acrossTopWindText = CreateLabel(reflection, parent, "AcrossTopWind");
            Component previousRightWindText = CreateLabel(reflection, parent, "PreviousRightWind");
            Component wallPointText = CreateLabel(reflection, parent, "WallPoint");
            Component windProgressText = CreateLabel(reflection, parent, "WindProgress");

            reflection.SetPrivateField(presenter, "selfBottomWindText", selfBottomWindText);
            reflection.SetPrivateField(presenter, "nextLeftWindText", nextLeftWindText);
            reflection.SetPrivateField(presenter, "acrossTopWindText", acrossTopWindText);
            reflection.SetPrivateField(presenter, "previousRightWindText", previousRightWindText);
            reflection.SetPrivateField(presenter, "wallPointText", wallPointText);
            reflection.SetPrivateField(presenter, "windProgressText", windProgressText);
            return windProgressText;
        }

        private static Component CreateLabel(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent(reflection.RequireType(TextMeshProUguiTypeName));
        }
    }
}
