using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StreamRC.Gambling.Cards;

namespace Gambling.Tests {

    /// <summary>
    /// tests assumptions about card data
    /// </summary>
    [TestFixture]
    public class CardTests {

        IEnumerable<Tuple<Card, byte>> Codes
        {
            get
            {
                yield return new Tuple<Card, byte>(new Card(CardRank.Deuce, CardSuit.Clubs), 0);
                yield return new Tuple<Card, byte>(new Card(CardRank.Deuce, CardSuit.Diamonds), 1);
                yield return new Tuple<Card, byte>(new Card(CardRank.Deuce, CardSuit.Hearts), 2);
                yield return new Tuple<Card, byte>(new Card(CardRank.Deuce, CardSuit.Spades), 3);
                yield return new Tuple<Card, byte>(new Card(CardRank.Trey, CardSuit.Clubs), 4);
                yield return new Tuple<Card, byte>(new Card(CardRank.Trey, CardSuit.Diamonds), 5);
                yield return new Tuple<Card, byte>(new Card(CardRank.Trey, CardSuit.Hearts), 6);
                yield return new Tuple<Card, byte>(new Card(CardRank.Trey, CardSuit.Spades), 7);
                yield return new Tuple<Card, byte>(new Card(CardRank.Four, CardSuit.Clubs), 8);
                yield return new Tuple<Card, byte>(new Card(CardRank.Four, CardSuit.Diamonds), 9);
                yield return new Tuple<Card, byte>(new Card(CardRank.Four, CardSuit.Hearts), 10);
                yield return new Tuple<Card, byte>(new Card(CardRank.Four, CardSuit.Spades), 11);
                yield return new Tuple<Card, byte>(new Card(CardRank.Five, CardSuit.Clubs), 12);
                yield return new Tuple<Card, byte>(new Card(CardRank.Five, CardSuit.Diamonds), 13);
                yield return new Tuple<Card, byte>(new Card(CardRank.Five, CardSuit.Hearts), 14);
                yield return new Tuple<Card, byte>(new Card(CardRank.Five, CardSuit.Spades), 15);
                yield return new Tuple<Card, byte>(new Card(CardRank.Six, CardSuit.Clubs), 16);
                yield return new Tuple<Card, byte>(new Card(CardRank.Six, CardSuit.Diamonds), 17);
                yield return new Tuple<Card, byte>(new Card(CardRank.Six, CardSuit.Hearts), 18);
                yield return new Tuple<Card, byte>(new Card(CardRank.Six, CardSuit.Spades), 19);
                yield return new Tuple<Card, byte>(new Card(CardRank.Seven, CardSuit.Clubs), 20);
                yield return new Tuple<Card, byte>(new Card(CardRank.Seven, CardSuit.Diamonds), 21);
                yield return new Tuple<Card, byte>(new Card(CardRank.Seven, CardSuit.Hearts), 22);
                yield return new Tuple<Card, byte>(new Card(CardRank.Seven, CardSuit.Spades), 23);
                yield return new Tuple<Card, byte>(new Card(CardRank.Eight, CardSuit.Clubs), 24);
                yield return new Tuple<Card, byte>(new Card(CardRank.Eight, CardSuit.Diamonds), 25);
                yield return new Tuple<Card, byte>(new Card(CardRank.Eight, CardSuit.Hearts), 26);
                yield return new Tuple<Card, byte>(new Card(CardRank.Eight, CardSuit.Spades), 27);
                yield return new Tuple<Card, byte>(new Card(CardRank.Nine, CardSuit.Clubs), 28);
                yield return new Tuple<Card, byte>(new Card(CardRank.Nine, CardSuit.Diamonds), 29);
                yield return new Tuple<Card, byte>(new Card(CardRank.Nine, CardSuit.Hearts), 30);
                yield return new Tuple<Card, byte>(new Card(CardRank.Nine, CardSuit.Spades), 31);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ten, CardSuit.Clubs), 32);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ten, CardSuit.Diamonds), 33);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ten, CardSuit.Hearts), 34);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ten, CardSuit.Spades), 35);
                yield return new Tuple<Card, byte>(new Card(CardRank.Jack, CardSuit.Clubs), 36);
                yield return new Tuple<Card, byte>(new Card(CardRank.Jack, CardSuit.Diamonds), 37);
                yield return new Tuple<Card, byte>(new Card(CardRank.Jack, CardSuit.Hearts), 38);
                yield return new Tuple<Card, byte>(new Card(CardRank.Jack, CardSuit.Spades), 39);
                yield return new Tuple<Card, byte>(new Card(CardRank.Queen, CardSuit.Clubs), 40);
                yield return new Tuple<Card, byte>(new Card(CardRank.Queen, CardSuit.Diamonds), 41);
                yield return new Tuple<Card, byte>(new Card(CardRank.Queen, CardSuit.Hearts), 42);
                yield return new Tuple<Card, byte>(new Card(CardRank.Queen, CardSuit.Spades), 43);
                yield return new Tuple<Card, byte>(new Card(CardRank.King, CardSuit.Clubs), 44);
                yield return new Tuple<Card, byte>(new Card(CardRank.King, CardSuit.Diamonds), 45);
                yield return new Tuple<Card, byte>(new Card(CardRank.King, CardSuit.Hearts), 46);
                yield return new Tuple<Card, byte>(new Card(CardRank.King, CardSuit.Spades), 47);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ace, CardSuit.Clubs), 48);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ace, CardSuit.Diamonds), 49);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ace, CardSuit.Hearts), 50);
                yield return new Tuple<Card, byte>(new Card(CardRank.Ace, CardSuit.Spades), 51);
            }
        }

        [Test, Description("Tests whether assumed card codes are correct")]
        public void TestCardCode([ValueSource("Codes")] Tuple<Card, byte> data) {
            Assert.AreEqual(data.Item2, data.Item1.Code, "Card code does not match.");
        }

        [Test]
        public void TestCardUrl([ValueSource("Codes")] Tuple<Card, byte> data) {
            CardImageModule module = new CardImageModule(null);
            string url = module.GetCardUrl(data.Item1);
            Match match = Regex.Match(url, "code=(?<code>[0-9]+)$");
            Assert.That(match.Success);
            Assert.AreEqual(data.Item2, byte.Parse(match.Groups["code"].Value));
        }

        [Test]
        public void TestResourcePath([ValueSource("Codes")] Tuple<Card, byte> data) {
            CardResourceHttpService service = new CardResourceHttpService();
            string path = service.GetResourcePath(data.Item2);
            Match match = Regex.Match(path, "Card(?<code>[0-9]+)\\.png$");
            Assert.That(match.Success);
            Assert.AreEqual(data.Item2 + 1, byte.Parse(match.Groups["code"].Value));
        }
    }
}