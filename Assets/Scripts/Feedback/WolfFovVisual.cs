using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WolfFovVisual : MonoBehaviour
{
    [SerializeField] private float _radius = 10f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private int _segments = 24;
    [SerializeField] private Color _color = new(0.95f, 0.30f, 0.25f, 0.16f);

    private Mesh _mesh;
    private Material _material;

    private void Awake()
    {
        Build();
    }

    public void Configure(float radius, float angle)
    {
        _radius = Mathf.Max(0.5f, radius);
        _angle = Mathf.Clamp(angle, 5f, 360f);

        if (_mesh != null) Build();
    }

    private void Build()
    {
        if (_segments < 3) _segments = 3;

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "WolfFov" };
            filter.mesh = _mesh;
        }

        if (_material == null)
        {
            _material = CreateMaterial();
            renderer.sharedMaterial = _material;
        }

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();
        float halfAngle = _angle * 0.5f;

        for (int i = 0; i <= _segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / _segments);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            vertices.Add(direction * _radius);
        }

        for (int i = 1; i <= _segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private Material CreateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new(shader) { name = "WolfFov" };

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", _color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", _color);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;

        return material;
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
        if (_mesh != null) Destroy(_mesh);
    }
}
