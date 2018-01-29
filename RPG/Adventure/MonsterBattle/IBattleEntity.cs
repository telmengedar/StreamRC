using System.Collections.Generic;
using StreamRC.RPG.Effects;

namespace StreamRC.RPG.Adventure.MonsterBattle {
    public interface IBattleEntity {
        Adventure Adventure { get; }
        MonsterBattleLogic BattleLogic { get; }

        string Image { get; }
        string Name { get; }
        int Level { get; }
        int HP { get; }
        int MaxHP { get; }
        int MP { get; }
        int Power { get; }
        int Defense { get; }
        int Dexterity { get; }
        int Luck { get; }
        int WeaponOptimum { get; }
        int ArmorOptimum { get; }

        /// <summary>
        /// adds an effect to the entity
        /// </summary>
        /// <param name="effect">effect to be added</param>
        void AddEffect(ITemporaryEffect effect);

        /// <summary>
        /// refreshes battle statistics to reflect current state
        /// </summary>
        void Refresh();

        void Hit(int damage);

        int Heal(int healing);

        BattleReward Reward(IBattleEntity victim);

        void CleanUp();

        IEnumerable<ITemporaryEffect> Effects { get; }
    }
}