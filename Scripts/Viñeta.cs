using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hace un raycast desde la camara (centro de la mirada) y va oscureciendo
/// los bordes de la pantalla (Vignette) cuando el objeto mirado es "enfocable".
/// Requiere URP con un Global Volume que tenga el override de Vignette.
/// </summary>
public class GazeFocusVignette : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Camara desde donde sale el raycast (normalmente Camera.main / la camara del XR Origin)")]
    [SerializeField] private Camera gazeCamera;

    [Tooltip("Volume que contiene el override de Vignette a controlar")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Deteccion")]
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private LayerMask focusableLayer;
    [Tooltip("Opcional: si lo dejas vacio, solo se usa la Layer de arriba")]
    [SerializeField] private string focusableTag = "Focusable";

    [Header("Vignette")]
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 0.45f;
    [SerializeField] private float fadeSpeed = 3f; // que tan rapido crece/decrece el efecto

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;

    private Vignette _vignette;
    private float _targetIntensity;
    private float _currentIntensity;

    private void Awake()
    {
        if (gazeCamera == null)
            gazeCamera = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;

        if (postProcessVolume == null)
        {
            Debug.LogError("[GazeFocusVignette] Falta asignar el Volume en el inspector.");
            enabled = false;
            return;
        }

        if (!postProcessVolume.profile.TryGet(out _vignette))
        {
            Debug.LogError("[GazeFocusVignette] El Volume Profile no tiene un override de Vignette agregado.");
            enabled = false;
        }
    }

    private void Update()
    {
        bool lookingAtTarget = CheckGaze();

        _targetIntensity = lookingAtTarget ? maxIntensity : minIntensity;
        _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * fadeSpeed);

        _vignette.intensity.Override(_currentIntensity);
    }

    private bool CheckGaze()
    {
        if (gazeCamera == null) return false;

        Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, focusableLayer))
        {
            if (string.IsNullOrEmpty(focusableTag) || hit.collider.CompareTag(focusableTag))
                return true;
        }

        return false;
    }
}