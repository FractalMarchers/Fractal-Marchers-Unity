using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaymarchingMaster : MonoBehaviour
{
    public ComputeShader RaymarchingShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight;
    private RenderTexture _target;
    private ComputeBuffer _sphereBuffer;

    private Camera _camera;
    [SerializeField]
    private GameObject ground;
    [SerializeField]
    private GameObject sphere;
    [SerializeField]
    private float sphereRadius;
    [SerializeField]
    private GameObject box;
    [SerializeField]
    private GameObject prism;
    [SerializeField]
    private GameObject torus;
    [Range(1, 10)]
    public int numberOfReflections = 8;
    [SerializeField]
    private bool smoothBlend = false;
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float blendCoefficient = 0.01f;
    [SerializeField]
    private Color color;

    struct Shape
    {
        Vector3 position;
        Vector3 size;
        Vector3 colour;
        Vector3 albedo;
    };

    private void OnEnable()
    {
        //color = Random.ColorHSV();
    }
    private void OnDisable()
    {

    }

    private void Awake()
    {
        _camera = this.GetComponent<Camera>();
    }

    private void SetShaderParameters()
    {
        RaymarchingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);   // _CameraToWorld declared in compute shader
        RaymarchingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse); // _CameraInverseProjection declared in compute shader
        RaymarchingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        Vector3 l = DirectionalLight.transform.forward;
        RaymarchingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RaymarchingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RaymarchingShader.SetVector("_Albedo", new Vector3(color.r, color.g, color.b));
        RaymarchingShader.SetInt("_NumberOfReflections", numberOfReflections);
        RaymarchingShader.SetBool("_SmoothBlend", smoothBlend);
        RaymarchingShader.SetFloat("_BlendCoefficient", blendCoefficient);
        RaymarchingShader.SetVector("_Ground", ground.transform.position);
        RaymarchingShader.SetVector("_GroundScale", ground.transform.localScale);
        RaymarchingShader.SetVector("_Sphere", new Vector4(sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z, sphereRadius));
        RaymarchingShader.SetVector("_Box", box.transform.position);
        RaymarchingShader.SetVector("_BoxScale", box.transform.localScale);
        RaymarchingShader.SetVector("_Prism", prism.transform.position);
        RaymarchingShader.SetVector("_PrismSize", new Vector2(prism.transform.localScale.x, prism.transform.localScale.y));
        RaymarchingShader.SetVector("_Torus", torus.transform.position);
        RaymarchingShader.SetVector("_TorusSize", new Vector2(torus.transform.localScale.x, torus.transform.localScale.y));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //_camera = Camera.current;
        //DirectionalLight = FindObjectOfType<Light>();

        SetShaderParameters();
        Render(destination);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        RaymarchingShader.SetTexture(0, "Result", _target);

        /* 
        Each thread group consists of a number of threads which is set in the shader itself. 
        The size and number of thread groups can be specified in up to three dimensions, which makes it easy to apply compute shaders to problems of either dimensionality. 
        In our case, we want to spawn one thread per pixel of the render target.
        The default thread group size as defined in the Unity compute shader template is [numthreads(8,8,1)]
        */
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RaymarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Finally, we write our result to the screen using Graphics.Blit.
        Graphics.Blit(_target, destination);
    }
}
