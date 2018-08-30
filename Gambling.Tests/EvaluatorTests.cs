using System;
using System.Collections.Generic;
using System.IO.Pipes;
using NUnit.Framework;
using StreamRC.Gambling.Cards;
using StreamRC.Gambling.Poker.Evaluation;

namespace Gambling.Tests {

    [TestFixture]
    public class EvaluatorTests {
        IEnumerable<Tuple<Board, HandRank>> Hands
        {
            get
            {
                yield return new Tuple<Board, HandRank>(Board.Parse("Th 3c Ks 2c 3h 7d 4s"), HandRank.Pair);
                yield return new Tuple<Board, HandRank>(Board.Parse("Th 3c Ks 2c 3h 9s 3d"), HandRank.ThreeOfAKind);
            }
        }

        [Test]
        public void TestEvaluatedRank([ValueSource("Hands")] Tuple<Board, HandRank> data) {
            HandEvaluation evaluation = HandEvaluator.Evaluate(data.Item1);
            Assert.AreEqual(data.Item2, evaluation.Rank);
        }

    }
}