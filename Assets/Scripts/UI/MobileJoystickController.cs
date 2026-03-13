using UnityEngine;
using CookMoiCa.Network;

/// <summary>
/// Cache le joystick mobile si le joueur n'est pas un client distant.
/// À attacher sur le MobileHUD (ou le parent du joystick).
/// </summary>
public class MobileJoystickController : MonoBehaviour
{
    [SerializeField] private GameObject mobileHUD;

    private void Start()
    {
        bool isClient = NetworkManager.Instance != null &&
                        NetworkManager.Instance.Role == NetworkRole.Client;

        if (mobileHUD != null)
            mobileHUD.SetActive(isClient);
    }
}
