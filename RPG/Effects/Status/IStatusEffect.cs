namespace StreamRC.RPG.Effects.Status {
    public interface IStatusEffect : ITemporaryEffect {
        void ProcessStatusEffect(double time);
    }
}