namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger
{
    // Ports the `mesp` text-speed variable, which in the original persists
    // across the whole messenger session (halved each time the player rejects
    // Donald and the scripted dialogue loops back to *messtart). Threaded
    // through SceneTransition payloads rather than a shared global.
    public sealed class MessengerState
    {
        public float MespMilliseconds = 500f;
    }
}
