using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Objects;

public class CustomMessage
{
    private readonly LobbyNotificationMessage notification;
    private readonly TMP_Text text;

    public CustomMessage(string message, float duration)
    {
        notification = MiraAPI.Utilities.Helpers.CreateAndShowNotification(message, Color.yellow);

        text = notification.GetComponentInChildren<TMP_Text>();

        HudManager.Instance.StartCoroutine(BlinkRoutine(message, duration).WrapToIl2Cpp());
    }

    private IEnumerator BlinkRoutine(string message, float duration)
    {
        float elapsed = 0f;
        const float interval = 0.25f;

        while (elapsed < duration)
        {
            var even = (int)(elapsed / interval) % 2 == 0;
            var prefix = even ? "<color=#FCBA03FF>" : "<color=#FF0000FF>";

            if (text != null)
            {
                text.text = prefix + message + "</color>";
                text.color = even ? Color.yellow : Color.red;
            }

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        if (notification != null && notification.gameObject != null)
        {
            Object.Destroy(notification.gameObject);
        }
    }
}