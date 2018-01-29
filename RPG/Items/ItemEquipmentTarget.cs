namespace StreamRC.RPG.Items {
    /// <summary>
    /// target slot where item is to be equipped
    /// </summary>
    public enum ItemEquipmentTarget {

        /// <summary>
        /// none (doesn't apply)
        /// </summary>
        None = 0x00000000,

        Head = 0x00000001,

        Neck = 0x00000002,

        Shoulder = 0x00000003,

        Back = 0x00000004,

        Body = 0x00000005,

        Legs = 0x00000006,

        Foot = 0x00000007,

        Arm = 0x00000008,

        Hands = 0x00000009,

        Finger = 0x0000000A
    }
}