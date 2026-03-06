using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class SceneReloader : MonoBehaviour
{
    [Tooltip("Choisir le contrôleur à écouter (par défaut : main droite).")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;

    [Tooltip("Afficher des logs pour debug.")]
    [SerializeField] private bool debugLogs = false;

    private InputDevice _device;
    private bool _previousAnyButtonState = false;

    private void Start()
    {
        TryInitDevice();
    }

    private void TryInitDevice()
    {
        _device = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (debugLogs) Debug.Log($"[ReloadSceneOnBPress] Trying to init device for {controllerNode}: valid={_device.isValid}");
    }

    private void Update()
    {
        // Re-tenter l'initialisation si le device est invalide (ex : connexion tardive)
        if (!_device.isValid)
        {
            TryInitDevice();
        }

        bool primaryPressed = false;
        bool secondaryPressed = false;

        // On teste primary et secondary pour couvrir différentes mappings (A/B ou X/Y)
        if (_device.isValid)
        {
            _device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed);
            _device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryPressed);
        }

        bool anyPressed = primaryPressed || secondaryPressed;

        // détection front montant (rising edge) pour éviter reload multiple sur maintien
        if (anyPressed && !_previousAnyButtonState)
        {
            if (debugLogs) Debug.Log("[ReloadSceneOnBPress] Button press detected -> reloading scene.");
            ReloadActiveScene();
        }

        _previousAnyButtonState = anyPressed;
    }

    private void ReloadActiveScene()
    {
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }
}
