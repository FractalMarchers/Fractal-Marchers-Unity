using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raytracing : MonoBehaviour
{
    public ComputeShader RaytracngShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight;
    private RenderTexture _target;

    private Camera _camera;
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer _sphereBuffer;

    private int _currentSample = 0;
    private Material _addMaterial;
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void Awake()
    {
        _camera = this.GetComponent<Camera>();
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

            // Add the sphere to the list
            spheres.Add(sphere);

        SkipSphere:
                    continue;
        }

        // Assign to compute buffer
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
        // The magic number 40 in new ComputeBuffer(spheres.Count, 40) is the stride of our buffer, i.e. the byte size of one sphere in memory.

        RaytracngShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    // Call SetShaderParameters from OnRenderImage before rendering.
    private void SetShaderParameters()
    {
        RaytracngShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);   // _CameraToWorld declared in compute shader
        RaytracngShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse); // _CameraInverseProjection declared in compute shader
        RaytracngShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        Vector3 l = DirectionalLight.transform.forward;
        RaytracngShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RaytracngShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        RaytracngShader.SetTexture(0, "Result", _target);

        /* 
        Each thread group consists of a number of threads which is set in the shader itself. 
        The size and number of thread groups can be specified in up to three dimensions, which makes it easy to apply compute shaders to problems of either dimensionality. 
        In our case, we want to spawn one thread per pixel of the render target.
        The default thread group size as defined in the Unity compute shader template is [numthreads(8,8,1)]
        */
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RaytracngShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Finally, we write our result to the screen using Graphics.Blit.
        if (_addMaterial == null)
        {
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample += 1;
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _currentSample = 0;
            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }
}
