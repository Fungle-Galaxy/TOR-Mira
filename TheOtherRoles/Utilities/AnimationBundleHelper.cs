using System.IO;
using System.Reflection;
using UnityEngine;

namespace TheOtherRoles.Utilities;

public static class AnimationBundleHelper
{
    private static AssetBundle _animBundle;
    private static bool _attemptedLoad;

    public static AssetBundle GetAnimBundle()
    {
        if (_animBundle == null && !_attemptedLoad)
        {
            _attemptedLoad = true;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceBundle = assembly.GetManifestResourceStream("TheOtherRoles.Resources.Animation.animation");
                if (resourceBundle == null) return null;
                using var ms = new MemoryStream();
                resourceBundle.CopyTo(ms);
                _animBundle = AssetBundle.LoadFromMemory(ms.ToArray());
            }
            catch
            {
            }
        }

        return _animBundle;
    }
}