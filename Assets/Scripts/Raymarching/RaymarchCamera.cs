﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class RaymarchCamera : SceneViewFilter
{
    [SerializeField]
    private Shader _shader;
    private Material _raymarchMat;
    private Camera _cam;

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

    [Header("Colors")]
    public Color _mainColor;
    public Color _secondaryColor;
    public Color _skyColor;
    public Color _lightColor;

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
    public Dropdown shapeDropdown;

    private void Start()
    {
        Application.targetFrameRate = 60;
        this._scaleFactor = scaleFactorSlider.value;

        _shape = shapeDropdown.value;
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

        _raymarchMaterial.SetInt("_shape", _shape);

        // Test
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
        // Test


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

    public void ChangeShape()
    {
        this._shape = shapeDropdown.value;
    }

    private void Update()
    {
        if (Time.unscaledTime > _timer)
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            fps_text.text = "FPS: " + fps.ToString();
            _timer = Time.unscaledTime + _fpsRefreshRate;
        }

        //current = 1f / Time.unscaledDeltaTime;
        //avg_frame_rate = (int)current;
        //fps_text.text = "FPS: " + avg_frame_rate.ToString();
    }
}
