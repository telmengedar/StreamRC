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

        [Test]
        public void TestQuadsKickerEvaluation() {
            HandEvaluation first = HandEvaluator.Evaluate(Board.Parse("4d4s4h4c2s8sJc"));
            HandEvaluation second = HandEvaluator.Evaluate(Board.Parse("4d4s4h4c2sKc6s"));
            Assert.That(second > first);
        }

        [Test]
        public void TestTripsKickerEvaluation() {
            HandEvaluation first = HandEvaluator.Evaluate(Board.Parse("Qd7d9dQcQs8sJc"));
            HandEvaluation second = HandEvaluator.Evaluate(Board.Parse("Qd7d9dQcQsKc6s"));
            Assert.That(second > first);
        }

        [Test]
        public void TestTwoPairKickerEvaluation()
        {
            HandEvaluation first = HandEvaluator.Evaluate(Board.Parse("4d4s3h3c2s8sJc"));
            HandEvaluation second = HandEvaluator.Evaluate(Board.Parse("4d4s3h3c2sKc6s"));
            Assert.That(second > first);
        }

        [Test]
        public void TestPairKickerEvaluation() {
            HandEvaluation first = HandEvaluator.Evaluate(Board.Parse("4d4s3h5c2s8sJc"));
            HandEvaluation second = HandEvaluator.Evaluate(Board.Parse("4d4s3h5c2sKc6s"));
            Assert.That(second > first);
        }
    }
}