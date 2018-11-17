using System;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {

    /// <summary>
    /// class of a word
    /// </summary>
    [Flags]
    public enum WordClass {

        None=0,

        /// <summary>
        /// word that identifies
        /// </summary>
        Noun=1,

        /// <summary>
        /// action event or situation
        /// </summary>
        AdjectiveCont=2,

        /// <summary>
        /// extra information about <see cref="Noun"/>s
        /// </summary>
        Adjective=4,

        /// <summary>
        /// information about a <see cref="AdjectiveCont"/>, <see cref="Adjective"/> or another <see cref="Adverb"/>
        /// </summary>
        Adverb=8,

        /// <summary>
        /// substitution for a <see cref="Noun"/>
        /// </summary>
        Pronoun=16,

        /// <summary>
        /// relationship of <see cref="Noun"/> and other words
        /// </summary>
        Preposition=32,

        /// <summary>
        /// connect phrase
        /// </summary>
        Conjunction=64,

        /// <summary>
        /// introduces a <see cref="Noun"/>
        /// </summary>
        Determiner=128,

        /// <summary>
        /// strong emotion
        /// </summary>
        Exclamation=256,

        Postposition = 512,

        Subject =1024,

        Verb=2048,

        Amplifier=4096,

        /// <summary>
        /// words like mc, van, von
        /// </summary>
        NameConjuction=8192
    }
}