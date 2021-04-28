using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class RaymarchCamera : SceneViewFilter
{
    [SerializeField]
    private Shader _shader;
    private Material _raymarchMat;
    private Camera _cam;
    public GameObject marble;
    private bool firstTime = true;

    public Transform _directionalLight;
    public Slider scaleFactorSlider;
    public Slider smoothFactorSlider;

    [Header("Raymarching parameters")]
    public float EPSILON;
    public int MAX_ITERATIONS;
    public float _maxDistance;
    public Vector3 _modInterval;

    [Header("Shading parameters")]
    [Range(0, 1)]
    public float _lightIntensity;
    [Range(0, 1)]
    public float _shadowIntensity;
    [Range(0, 1)]
    public float _aoIntensity;
    public Vector2 _shadowDistance;
    [Range(2, 40)]
    public int Ks = 20;
    public bool _specular = true;

    [Header("Marble Parameters")]
    public bool _renderMarble;
    public Color _marbleColor;
    public bool _marbleReflection = false;
    public bool _marbleRefraction = true;
    public float _marbleRadius = 2.0f;

    [Header("Colors")]
    public Color _mainColor;
    public Color _secondaryColor;
    public Color _skyColor;
    public Color _lightColor;

    public Color[] Maincolors = new Color[5];
    public Color[] Secondarycolors = new Color[5];
    private int colorIndex;
    private float t = 0.0f;
    public bool shuffleColors = false;

    [HideInInspector]
    public Matrix4x4 _globalTransform;
    public Vector3 _GlobalRotation;
    [Header("transform Settings")]
    public Vector3 _globalPosition;
    [HideInInspector]
    public Matrix4x4 _iterationTransform;
    public Vector3 _iterationOffsetPos;
    public Vector3 _iterationOffsetRot;
    public float _GlobalScale;
    public int sponge_iterations;
    public bool _pause = false;
    public float _scaleFactor = 1;
    public float _smoothRadius = 0;
    public Vector3 _modOffsetPos;
    public bool _infinite;
    public bool _useShadow = false;

    private int _shape = 0;

    private float lerp = 0f;
    private Color _originalMainColor, _originalSecondaryColor;

    [Header("UI Parameters")]
    public int _fpsRefreshRate = 1;
    private float _timer = 0;
    public Text fps_text;
    public GameObject reflectionToggle;
    public GameObject specularToggle;
    public Slider KsSlider;

    private bool reverseSmoothnessDirection = false;
    private bool reverseScaleDirection = false;

    private Vector3 _marbleDirection = Vector3.forward + Vector3.left;

    private IEnumerator smoothCoroutine, scaleCoroutine;

    private void Start()
    {
        Application.targetFrameRate = 60;
        this._scaleFactor = scaleFactorSlider.value;
        
        smoothCoroutine = ChangeSmoothness();
        StartCoroutine(smoothCoroutine);

        scaleCoroutine = ChangeScale();
        StartCoroutine(scaleCoroutine);

        reflectionToggle.SetActive(_renderMarble);
        specularToggle.SetActive(_renderMarble);
        KsSlider.transform.gameObject.SetActive(_renderMarble);
    }

    public Material _raymarchMaterial
    {
        get
        {
            if (!_raymarchMat && _shader)
            {
                _raymarchMat = new Material(_shader);
                _raymarchMat.hideFlags = HideFlags.HideAndDontSave;
            }
            return _raymarchMat;
        }
    }

    public Camera _camera
    {
        get
        {
            if (!_cam)
            {
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_raymarchMaterial)
        {
            Graphics.Blit(source, destination);
            return;
        }

        _raymarchMat.SetVector("_marblePos", marble.transform.position);
        _raymarchMat.SetMatrix("_CamFrustum", CamFrustum(_camera));
        _raymarchMat.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _raymarchMat.SetFloat("_maxDistance", _maxDistance);
        _raymarchMat.SetFloat("EPSILON", EPSILON);
        _raymarchMat.SetInt("MAX_ITERATIONS", MAX_ITERATIONS);
        if (_directionalLight)
            _raymarchMat.SetVector("_directionalLight", _directionalLight.forward);
        else
            _raymarchMat.SetVector("_directionalLight", Vector3.down);
        _raymarchMat.SetVector("_modInterval", _modInterval);

        _raymarchMat.SetColor("_mainColor", _mainColor);
        _originalMainColor = _mainColor;
        _raymarchMat.SetColor("_secondaryColor", _secondaryColor);
        _raymarchMat.SetColor("_skyColor", _skyColor);
        _originalSecondaryColor = _secondaryColor;
        _raymarchMat.SetColor("_lightColor", _lightColor);

        if (_renderMarble)
        {
            _raymarchMat.SetInt("_renderMarble", 1);
        }
        else
        {
            _raymarchMat.SetInt("_renderMarble", 0);
        }

        _raymarchMat.SetColor("_marbleColor", _marbleColor);
        _raymarchMat.SetFloat("_lightIntensity", _lightIntensity);
        _raymarchMat.SetFloat("_shadowIntensity", _shadowIntensity);
        _raymarchMat.SetFloat("_aoIntensity", _aoIntensity);
        _raymarchMat.SetVector("_shadowDistance", _shadowDistance);
        _raymarchMaterial.SetFloat("_GlobalScale", _GlobalScale);
        _raymarchMaterial.SetInt("sponge_iterations", sponge_iterations);
        _raymarchMaterial.SetVector("_modOffsetPos", _modOffsetPos);
        _raymarchMaterial.SetFloat("_scaleFactor", _scaleFactor);
        _raymarchMaterial.SetFloat("_smoothRadius", _smoothRadius);
        if (_infinite)
            _raymarchMaterial.SetInt("_infinite", 1);
        else
            _raymarchMaterial.SetInt("_infinite", 0);
        if (_useShadow)
            _raymarchMaterial.SetInt("_useShadow", 1);
        else
            _raymarchMaterial.SetInt("_useShadow", 0);
        if (_specular)
        {
            _raymarchMaterial.SetInt("_specular", 1);
        }
        else
        {
            _raymarchMaterial.SetInt("_specular", 0);
        }
        _raymarchMaterial.SetInt("_Ks", Ks);

        if (_marbleReflection)
        {
            _raymarchMaterial.SetInt("_marbleReflection", 1);
        }
        else
        {
            _raymarchMaterial.SetInt("_marbleReflection", 0);
        }

        if (_marbleRefraction)
        {
            _raymarchMat.SetInt("_marbleRefraction", 1);
        }
        else
        {
            _raymarchMat.SetInt("_marbleRefraction", 0);
        }

        if (firstTime)
        {
            _raymarchMat.SetFloat("_marbleRadius", _marbleRadius);
            //firstTime = false;
        }


        _raymarchMaterial.SetInt("_shape", _shape);
        
        // Construct a Model Matrix for the global transform
        _globalTransform = Matrix4x4.TRS(
        _globalPosition,
        Quaternion.identity,
        Vector3.one);
        _globalTransform *= Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(_GlobalRotation),
            Vector3.one);
        // Send the matrix to our shader
        _raymarchMaterial.SetMatrix("_globalTransform", _globalTransform.inverse);


        // Construct a Model Matrix for the iteration transform
        _iterationTransform = Matrix4x4.TRS(
        _iterationOffsetPos,
        Quaternion.identity,
        Vector3.one);
        _iterationTransform *= Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(_iterationOffsetRot),
            Vector3.one);
        // Send the matrix to our shader
        _raymarchMaterial.SetMatrix("_iterationTransform", _iterationTransform.inverse);


        RenderTexture.active = destination;
        _raymarchMat.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        _raymarchMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f);

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f);

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 CamFrustum(Camera cam)
    {
        Matrix4x4 frustum = Matrix4x4.identity;
        float fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);
        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 TL = (-Vector3.forward - goRight + goUp);
        Vector3 TR = (-Vector3.forward + goRight + goUp);
        Vector3 BR = (-Vector3.forward + goRight - goUp);
        Vector3 BL = (-Vector3.forward - goRight - goUp);

        frustum.SetRow(0, TL);
        frustum.SetRow(1, TR);
        frustum.SetRow(2, BR);
        frustum.SetRow(3, BL);

        return frustum;
    }

    public void ToggleMarble()
    {
        _renderMarble = !_renderMarble;
        reflectionToggle.SetActive(_renderMarble);
        specularToggle.SetActive(_renderMarble);
        KsSlider.transform.gameObject.SetActive(_renderMarble && _specular);
    }

    public void ToggleInfiniteRender()
    {
        _infinite = !_infinite;
    }
    public void ToggleShadowRender()
    {
        _useShadow = !_useShadow;
    }

    public void SetScaleFactor()
    {
        this._scaleFactor = scaleFactorSlider.value;
    }
    public void SetSmoothFactor()
    {
        this._smoothRadius = smoothFactorSlider.value;
    }

    public void SetKs()
    {
        this.Ks = (int)KsSlider.value;
    }

    public void ToggleMarbleReflection()
    {
        _marbleReflection = !_marbleReflection;
    }

    public void ToggleSpecularReflection()
    {
        _specular = !_specular;
        KsSlider.transform.gameObject.SetActive(_specular);
    }

    public void ToggleShuffleColors()
    {
        shuffleColors = !shuffleColors;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (shuffleColors)
        {
            _mainColor = Color.Lerp(_mainColor, Maincolors[colorIndex], 0.5f * Time.deltaTime);
            _secondaryColor = Color.Lerp(_secondaryColor, Secondarycolors[colorIndex], 0.5f * Time.deltaTime);
            t = Mathf.Lerp(t, 1f, 0.5f * Time.deltaTime);
            if (t > 0.9f)
            {
                t = 0.0f;
                colorIndex += 1;
                colorIndex = (colorIndex >= Maincolors.Length) ? 0 : colorIndex;
            }
        }

        if (!_pause && _smoothRadius >= smoothFactorSlider.maxValue)
        {
            reverseSmoothnessDirection = true;
            StartCoroutine(ChangeSmoothness());
        }
        else if (!_pause && _smoothRadius <= smoothFactorSlider.minValue)
        {
            reverseSmoothnessDirection = false;
            StartCoroutine(ChangeSmoothness());
        }

        if (!_pause && _scaleFactor >= scaleFactorSlider.maxValue)
        {
            reverseScaleDirection = true;
            StartCoroutine(ChangeScale());
        }
        else if (!_pause && _scaleFactor <= scaleFactorSlider.minValue)
        {
            reverseScaleDirection = false;
            StartCoroutine(ChangeScale());
        }

        

        if (Time.unscaledTime > _timer)
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            fps_text.text = "FPS: " + fps.ToString();
            _timer = Time.unscaledTime + _fpsRefreshRate;
        }
    }

    private IEnumerator ChangeSmoothness()
    {
        float v_start = smoothFactorSlider.minValue;
        float v_end = smoothFactorSlider.maxValue;
        if (reverseSmoothnessDirection)
        {
            v_start = smoothFactorSlider.maxValue;
            v_end = smoothFactorSlider.minValue;
        }
        float duration = 10;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            _smoothRadius = Mathf.Lerp(v_start, v_end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _smoothRadius = v_end;
    }

    private IEnumerator ChangeScale()
    {
        float v_start = scaleFactorSlider.minValue;
        float v_end = scaleFactorSlider.maxValue;
        if (reverseScaleDirection)
        {
            v_start = scaleFactorSlider.maxValue;
            v_end = scaleFactorSlider.minValue;
        }
        float duration = 10;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            _scaleFactor = Mathf.Lerp(v_start, v_end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _scaleFactor = v_end;
    }

    private IEnumerator Delay(float delay)
    {
        yield return new WaitForSeconds(delay);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
