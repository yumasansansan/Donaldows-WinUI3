namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg
{
    public sealed class RpgBattleState
    {
        public float Tairyoku = 10000f;
        public float Sennou;
        public float HonoFade;
        public float CRed;

        // *enemydata is a single hardcoded record in the original (the `ene`
        // indexed if-branch looks like a stub for more enemies that was never
        // filled in) — kept as fields rather than a full table for the same reason.
        public string EnemyName = "ポップアップドナルド";
        public int EnemyHp = 2000;

        public int Hit;
        public string HitMes = "";
        public float TairyokuLoss;
        public float SennouGain;
    }
}
