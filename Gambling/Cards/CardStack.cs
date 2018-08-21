using System;
using System.Collections.Generic;
using NightlyCode.Core.Collections;
using NightlyCode.Core.Randoms;

namespace StreamRC.Gambling.Cards
{

    /// <summary>
    /// stack of cards
    /// </summary>
    public class CardStack
    {
        Queue<Card> stack = new Queue<Card>();

        /// <summary>
        /// creates a new empty <see cref="CardStack"/>
        /// </summary>
        public CardStack()
        {
        }

        /// <summary>
        /// creates a fresh unshuffled stack of cards
        /// </summary>
        /// <returns>a new unshuffled stack of cards</returns>
        public static CardStack Fresh() {
            CardStack stack=new CardStack();
            for (byte i = 0; i < 52; ++i)
                stack.Push(new Card(i));
            return stack;
        }

        /// <summary>
        /// creates a new <see cref="CardStack"/> by using a card mask
        /// </summary>
        /// <param name="mask">mask defining cards in the stack</param>
        public static void FromMask(ulong mask) {
            CardStack stack = new CardStack();
            byte k = 0;
            for(ulong i = mask; i != 0; i >>= 1, ++k)
                if((i & 1) > 0)
                    stack.Push(new Card(k));
        }

        /// <summary>
        /// number of cards in the stack
        /// </summary>
        public int Count {
            get { return stack.Count; }
        }

        /// <summary>
        /// pushes a card on the stack
        /// </summary>
        /// <param name="card">card to be pushed</param>
        public void Push(Card card) {
            stack.Enqueue(card);
        }

        /// <summary>
        /// pops a card from the stack
        /// </summary>
        /// <returns>next card</returns>
        public Card Pop()
        {
            return stack.Dequeue();
        }

        /// <summary>
        /// shuffles the deck
        /// </summary>
        public void Shuffle() {
            stack = new Queue<Card>(stack.Shuffle(RNG.XORShift64));
        }
    }
}
