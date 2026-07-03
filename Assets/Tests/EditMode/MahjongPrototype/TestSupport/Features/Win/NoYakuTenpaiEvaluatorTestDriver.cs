using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class NoYakuTenpaiEvaluatorTestDriver : IDisposable
    {
        private readonly WinFeatureTestSupport support;
        private bool disposed;

        private NoYakuTenpaiEvaluatorTestDriver(WinFeatureTestSupport support)
        {
            this.support = support;
        }

        public static NoYakuTenpaiEvaluatorTestDriver Create()
        {
            return new NoYakuTenpaiEvaluatorTestDriver(WinFeatureTestSupport.Create());
        }

        public object CreateCatalog(params object[] definitions)
        {
            return support.CreateYakuCatalog(definitions);
        }

        public object CreateDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman = false)
        {
            return support.CreateYakuDefinition(
                yakuKindName,
                closedHanName,
                openHanName,
                isYakuman);
        }

        public object Evaluate(
            object catalog,
            string handText,
            bool isReachDeclared = false)
        {
            object evaluator = support.CreateNoYakuTenpaiEvaluator(catalog);
            return support.EvaluateNoYakuTenpai(evaluator, handText, isReachDeclared);
        }

        public object EvaluateWithoutWinDeclarationEvaluator(string handText)
        {
            object evaluator = support.CreateNoYakuTenpaiEvaluatorWithoutWinDeclarationEvaluator();
            return support.EvaluateNoYakuTenpai(evaluator, handText, false);
        }

        public bool IsEvaluated(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "IsEvaluated");
        }

        public bool IsTenpai(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "IsTenpai");
        }

        public bool HasAnyYakuWait(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "HasAnyYakuWait");
        }

        public bool ShouldShowZeroHanTenpai(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "ShouldShowZeroHanTenpai");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }
    }
}
