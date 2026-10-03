using System;
using UnityEngine;

namespace TheOtherRoles.Draft;

/// <summary>
/// Owns the draft's per frame work: the host's turn clock and both screens' timers. It has to be a
/// real component - the lobby HUD is switched off while the draft is up, so no vanilla Update is
/// guaranteed to run for us.
/// </summary>
public class DraftEngineBehaviour : MonoBehaviour
{
    private static DraftEngineBehaviour instance;

    static DraftEngineBehaviour()
    {
        ClassInjector.RegisterTypeInIl2Cpp<DraftEngineBehaviour>();
    }

    public DraftEngineBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    public static void Ensure()
    {
        if (instance) return;
        var go = new GameObject("TorDraftEngine");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DraftEngineBehaviour>();
    }

    private void Update()
    {
        DraftEngine.Tick();
        DraftStatusOverlay.Tick();
        DraftSelectionScreen.Tick();
        DraftChat.Tick();
    }
}
