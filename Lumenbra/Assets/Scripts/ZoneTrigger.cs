using UnityEngine;

public enum ZoneType { LightZone, ShadowZone }

[RequireComponent(typeof(Collider2D))]
public class ZoneTrigger : MonoBehaviour
{
    public ZoneType zoneType;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerFullController player = other.GetComponent<PlayerFullController>();
        if (player == null) return;

        if ((zoneType == ZoneType.LightZone && player.GetCurrentRole() == PlayerRole.Shadow) ||
            (zoneType == ZoneType.ShadowZone && player.GetCurrentRole() == PlayerRole.Light))
        {
            player.OnEnterWrongZone();
        }
    }
}
