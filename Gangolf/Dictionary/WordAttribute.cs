using System;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {

    /// <summary>
    /// meta information for <see cref="Word"/>
    /// </summary>
    [Flags]
    public enum WordAttribute {
        None=0,
        Product=0x00000001,
        Tool= 0x00000002,
        Insultive= 0x00000004,
        Romantic= 0x00000008,
        Producer= 0x00000010,
        Color= 0x00000020,

        Political= 0x00000080,
        Descriptive= 0x00000100,
        Object= 0x00000200,
        Greeting= 0x00000400,
        Comparision= 0x00000800,
        Title= 0x00001000,

        /// <summary>
        /// word points to an individual
        /// </summary>
        Subject= 0x00002000,

        Food= 0x00004000,

        /// <summary>
        /// event of some sort (mainly used in combination with other attributes)
        /// </summary>
        Event= 0x00008000,

        /// <summary>
        /// term is a countable instance term
        /// </summary>
        Countable=0x00010000,

        Drink=0x00020000
    }
}