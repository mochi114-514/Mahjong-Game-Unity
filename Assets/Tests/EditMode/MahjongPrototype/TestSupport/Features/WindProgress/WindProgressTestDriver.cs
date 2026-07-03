using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.WindProgress
{
    internal sealed class WindProgressTestDriver
    {
        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly MahjongTestTypes types;

        private WindProgressTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            MahjongTestTypes types)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.types = types;
        }

        public static WindProgressTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new WindProgressTestDriver(reflection, dataFactory, types);
        }

        public object EastOne()
        {
            return reflection.GetStaticProperty(types.WindProgress, "East1");
        }

        public object CreateProgress(string roundWindName, int handNumber)
        {
            return dataFactory.CreateWindProgress(roundWindName, handNumber);
        }

        public bool TryGetNext(object progress, out object next)
        {
            object[] args = { null };
            bool result = (bool)reflection.Invoke(progress, "TryGetNext", args);
            next = args[0];
            return result;
        }

        public string RoundWindName(object progress)
        {
            return reflection.GetProperty(progress, "RoundWind").ToString();
        }

        public int HandNumber(object progress)
        {
            return (int)reflection.GetProperty(progress, "HandNumber");
        }

        public object CreateDefaultGameState()
        {
            return dataFactory.CreateGameState();
        }

        public object CreateGameState(string roundWindName, int handNumber)
        {
            return dataFactory.CreateGameStateWithWindProgress(
                CreateProgress(roundWindName, handNumber));
        }

        public object WindProgressOf(object gameState)
        {
            return reflection.GetProperty(gameState, "WindProgress");
        }

        public Exception CaptureCreateException(string roundWindName, int handNumber)
        {
            try
            {
                CreateProgress(roundWindName, handNumber);
                return null;
            }
            catch (TargetInvocationException exception)
            {
                return exception.InnerException;
            }
        }
    }
}
