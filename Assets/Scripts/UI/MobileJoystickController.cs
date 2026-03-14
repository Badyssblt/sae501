using UnityEngine;
using CookMoiCa.Network;

/// <summary>
/// Cache le joystick mobile si le joueur n'est pas un client distant.
/// À attacher sur le MobileHUD (ou le parent du joystick).
/// </summary>
public class MobileJoystickController : MonoBehaviour
{
    [SerializeField] private GameObject mobileHUD;
    [SerializeField] private GameObject interactButton;

    private void Start()
    {
        if (mobileHUD != null)
            mobileHUD.SetActive(true);
        if (interactButton != null)
            interactButton.SetActive(true);
    }
}
