using UnityEngine;

/// <summary>
/// Se agrega automaticamente a cada "fantasma" generado por ObjectGhostTrail.
/// Baja la opacidad con el tiempo hasta destruir el objeto.
/// </summary>
public class GhostFader : MonoBehaviour
{
    private float _lifetime;
    private float _elapsed;
    private Color _color;
    private float _startAlpha;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propertyBlock;

    public void Init(float lifetime, Color color, float startAlpha)
    {
        _lifetime = lifetime;
        _color = color;
        _startAlpha = startAlpha;
        _meshRenderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _lifetime);
        float alpha = Mathf.Lerp(_startAlpha, 0f, t);

        Color currentColor = _color;
        currentColor.a = alpha;

        _propertyBlock.SetColor("_Color", currentColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock);

        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }
}
