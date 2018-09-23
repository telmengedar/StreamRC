using System;

namespace NightlyCode.StreamRC.Gangolf.Dictionary {

    /// <summary>
    /// meta information for <see cref="Word"/>
    /// </summary>
    [Flags]
    public enum WordAttribute {
        None=0,
        Product=1,
        Tool=2,
        Insultive=4,
        Romantic=8,
        Producer=16,
        Color=32,
        Political=128,
        Descriptive=256,
        Object=512
    }
}