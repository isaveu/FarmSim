﻿using System;
using System.Collections.Generic;
using Assets.Components;
using Assets.FarmSim;
using Assets.FarmSim.I3D;
using UnityEngine;
using UnityEngine.Collections;

namespace Assets
{
    public class Entity : MonoBehaviour
    {
        public Shape Shape;

        public enum ShapeType
        {
            Camera, Light, TerrainTransformGroup, Layers,
            Layer, LayerCombiner, LayerPair, InfoLayer,
            FoliageMultiLayer, FoliageSubLayer, DetailLayer, TransformGroup,
            Shape, i3D, NavigationMesh, AudioSource,
            Dynamic
        }

        [ReadOnly]
        public ShapeType Type;
        [HideInInspector]
        public Texture2D Tex;
        
        private bool _visible;
        public bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                GetComponent<MeshRenderer>().enabled = value;
            }
        }

        private bool VisibleInnerCheck(GameObject part)
        {
            while (true)
            {
                I3DTransform i3Dtransform = part.GetComponent<I3DTransform>();
                if (!i3Dtransform)
                    return true;

                if (i3Dtransform.Visibility == false)
                    return false;

                if (part.transform.parent == null)
                    return true;

                part = part.transform.parent.gameObject;
            }
        }

        private bool DeepIsVisible()
        {
            if (!Shape || Shape.NonRenderable)
                return false;

            if (gameObject.transform.parent == null)
                return true;

            return VisibleInnerCheck(gameObject.transform.parent.gameObject);
        }

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        public static void ChangeRenderMode(Material mat, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 2450;
                    break;
                case BlendMode.Fade:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    break;
                case BlendMode.Transparent:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    break;
            }
        }

        public readonly HashSet<int> CutoutShaders = new HashSet<int>
        {
            136, 164, 176
        };

        public void Setup()
        {
            //Assign material
            if (Shape != null)
            {
                foreach (I3DMaterial shapeMaterial in Shape.Materials)
                {
                    if (shapeMaterial.TextureFile == null)
                        continue;

                    Material mat = GetComponent<Renderer>().material;
                    mat.mainTextureScale = new Vector2(1, -1);
                    try
                    {
                        mat.mainTexture = TextureLoader.GetTexture(shapeMaterial.TextureFile.AbsolutePath);
                    }
                    catch (UnityException e)
                    {
                        Debug.LogError($"Failed to parse texture {shapeMaterial.TextureFile.AbsolutePath}\n{e.Message}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse texture {shapeMaterial.TextureFile.AbsolutePath}\n{e.Message}");
                    }

                    if (shapeMaterial.NormalMapFile != null)
                        mat.SetTexture("_BumpMap", TextureLoader.GetTexture(shapeMaterial.NormalMapFile.AbsolutePath));

                    if (shapeMaterial.AlphaBlending)
                    {
                        ChangeRenderMode(mat, BlendMode.Transparent);
                    }

                    if (CutoutShaders.Contains(shapeMaterial.CustomShaderId))
                    {
                        ChangeRenderMode(mat, BlendMode.Cutout);
                    }
                }
            }

            //Assign name
            name = GetComponent<I3DTransform>().Name;

            //Check visibility
            Visible = DeepIsVisible();
            //Visible = !Scene.NonRenderable;
            //Visible = true;
        }
    }
}
