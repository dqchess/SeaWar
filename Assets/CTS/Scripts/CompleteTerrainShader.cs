using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace CTS
{
    /// <summary>
    /// The complete terrain shader editor manager - installed per terrain object - does shader control on a terrain.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    [ExecuteInEditMode]
    [System.Serializable]
    public class CompleteTerrainShader : MonoBehaviour
    {
        /// <summary>
        /// The path where CTS is located
        /// </summary>
        public string m_ctsDirectory = "Assets/CTS/";

        /// <summary>
        /// The terrain profile
        /// </summary>
        public CTSProfile Profile
        {
            get { return m_profile; }
            set
            {
                //Update our path
                m_ctsDirectory = GetCTSDirectory();

                //Make sure we have terrain
                if (m_terrain == null)
                {
                    m_terrain = transform.GetComponent<Terrain>();
                }

                if (m_profile == null)
                {
                    m_profilePath = "";
                    m_profile = value;
                    if (m_profile != null)
                    {
                        #if UNITY_EDITOR
                        m_profilePath = AssetDatabase.GetAssetPath(m_profile);
                        #endif
                        if (m_profile.TerrainTextures.Count == 0)
                        {
                            UpdateProfileFromTerrainForced();
                        }
                        else if (TerrainNeedsTextureUpdate())
                        {
                            ReplaceTerrainTexturesFromProfile();
                        }
                    }
                }
                else
                {
                    if (value == null)
                    {
                        m_profile = value;
                        m_profilePath = "";
                    }
                    else
                    {
                        if (m_profile.GetInstanceID() != value.GetInstanceID())
                        {
                            m_profile = value;
                            #if UNITY_EDITOR
                            m_profilePath = AssetDatabase.GetAssetPath(m_profile);
                            #endif
                        }
                        if (m_profile.TerrainTextures.Count == 0)
                        {
                            UpdateProfileFromTerrainForced();
                        }
                        else if (TerrainNeedsTextureUpdate())
                        {
                            ReplaceTerrainTexturesFromProfile();
                        }
                    }
                }
                if (m_profile != null)
                {
                    m_activeShaderType = m_profile.ShaderType;
                    ApplyShaderMaterial();
                }
                SetDirty(this, false, false);
            }
        }
        [SerializeField]
        private CTSProfile m_profile;
        #pragma warning disable 0414
        [SerializeField]
        private string m_profilePath;
        #pragma warning restore 0414

        /// <summary>
        /// Terrain normal map
        /// </summary>
        public Texture2D NormalMap
        {
            get { return m_normalMap; }
            set
            {
                if (value == null)
                {
                    if (m_normalMap != null)
                    {
                        m_normalMap = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_normalMap == null || m_normalMap.GetInstanceID() != value.GetInstanceID())
                    {
                        m_normalMap = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_normalMap;

        /// <summary>
        /// Auto bake the normal map when baking terrain
        /// </summary>
        public bool AutoBakeNormalMap
        {
            get { return m_bakeNormalMap; }
            set { m_bakeNormalMap = value; }
        }
        [SerializeField]
        private bool m_bakeNormalMap = true;

        /// <summary>
        /// Terrain colormap
        /// </summary>
        public Texture2D ColorMap
        {
            get { return m_colorMap; }
            set
            {
                if (value == null)
                {
                    if (m_colorMap != null)
                    {
                        m_colorMap = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_colorMap == null || m_colorMap.GetInstanceID() != value.GetInstanceID())
                    {
                        m_colorMap = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_colorMap;

        /// <summary>
        /// Auto bake the colour map when baking terrain
        /// </summary>
        public bool AutoBakeColorMap
        {
            get { return m_bakeColorMap; }
            set { m_bakeColorMap = value; }
        }
        [SerializeField]
        private bool m_bakeColorMap = false;

        /// <summary>
        /// Autobake grass into color map when baking terrain
        /// </summary>
        public bool AutoBakeGrassIntoColorMap
        {
            get { return m_bakeGrassTextures; }
            set { m_bakeGrassTextures = value; }
        }
        [SerializeField]
        private bool m_bakeGrassTextures = false;

        /// <summary>
        /// Grass mix strength when baking
        /// </summary>
        public float AutoBakeGrassMixStrength
        {
            get { return m_bakeGrassMixStrength; }
            set { m_bakeGrassMixStrength = value; }
        }
        [SerializeField]
        private float m_bakeGrassMixStrength = 0.2f;

        /// <summary>
        /// Grass darken amount when baking
        /// </summary>
        public float AutoBakeGrassDarkenAmount
        {
            get { return m_bakeGrassDarkenAmount; }
            set { m_bakeGrassDarkenAmount = value; }
        }
        [SerializeField]
        private float m_bakeGrassDarkenAmount = 0.2f;

        /// <summary>
        /// Flag to signal usage of a cutout shader
        /// </summary>
        public bool UseCutout
        {
            get { return m_useCutout; }
            set
            {
                if (m_useCutout != value)
                {
                    m_useCutout = value;
                    SetDirty(this, false, false);
                }
            }
        }
        [SerializeField]
        private bool m_useCutout = false;

        /// <summary>
        /// Cutout mask - used with cutout shader - cutout mask is in the (A) channel
        /// </summary>
        public Texture2D CutoutMask
        {
            get { return m_cutoutMask; }
            set
            {
                if (value == null)
                {
                    if (m_cutoutMask != null)
                    {
                        m_cutoutMask = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_cutoutMask == null || m_cutoutMask.GetInstanceID() != value.GetInstanceID())
                    {
                        m_cutoutMask = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_cutoutMask;

        /// <summary>
        /// Height below which the terrain cutout will be applied
        /// </summary>
        public float CutoutHeight
        {
            get { return m_cutoutHeight; }
            set
            {
                if (m_cutoutHeight != value)
                {
                    m_cutoutHeight = value;
                    SetDirty(this, false, false);
                }
            }
        }
        [SerializeField]
        private float m_cutoutHeight = 50f;

        /// <summary>
        /// Splat texture 1 - controls textures 0..3
        /// </summary>
        public Texture2D Splat1
        {
            get { return m_splat1; }
            set
            {
                if (value == null)
                {
                    if (m_splat1 != null)
                    {
                        m_splat1 = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_splat1 == null || m_splat1.GetInstanceID() != value.GetInstanceID())
                    {
                        m_splat1 = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_splat1;

        /// <summary>
        /// Splat texture 2 - controls textures 4..7
        /// </summary>
        public Texture2D Splat2
        {
            get { return m_splat2; }
            set
            {
                if (value == null)
                {
                    if (m_splat2 != null)
                    {
                        m_splat2 = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_splat2 == null || m_splat2.GetInstanceID() != value.GetInstanceID())
                    {
                        m_splat2 = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_splat2;

        /// <summary>
        /// Splat texture 3 - controls textures 8..11
        /// </summary>
        public Texture2D Splat3
        {
            get { return m_splat3; }
            set
            {
                if (value == null)
                {
                    if (m_splat3 != null)
                    {
                        m_splat3 = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_splat3 == null || m_splat3.GetInstanceID() != value.GetInstanceID())
                    {
                        m_splat3 = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_splat3;

        /// <summary>
        /// Splat texture 4 - controls textures 12..15
        /// </summary>
        public Texture2D Splat4
        {
            get { return m_splat4; }
            set
            {
                if (value == null)
                {
                    if (m_splat4 != null)
                    {
                        m_splat4 = value;
                        SetDirty(this, false, false);
                    }
                }
                else
                {
                    if (m_splat4 == null || m_splat4.GetInstanceID() != value.GetInstanceID())
                    {
                        m_splat4 = value;
                        SetDirty(this, false, false);
                    }
                }
            }
        }
        [SerializeField]
        private Texture2D m_splat4;

        /// <summary>
        /// Store a backup of the splats here if there is chance we will use unity shader
        /// </summary>
        private float [,,] m_splatBackupArray;

        /// <summary>
        /// Shader type
        /// </summary>
        [SerializeField]
        private CTSConstants.ShaderType m_activeShaderType = CTSConstants.ShaderType.Unity;

        /// <summary>
        /// Current terrain
        /// </summary>
        [SerializeField]
        private Terrain m_terrain;

        /// <summary>
        /// Current material
        /// </summary>
        [SerializeField]
        private Material m_material;

        /// <summary>
        /// Flags used to decipher terrain changes
        /// </summary>
        [Flags]
        internal enum TerrainChangedFlags
        {
            NoChange = 0,
            Heightmap = 1 << 0,
            TreeInstances = 1 << 1,
            DelayedHeightmapUpdate = 1 << 2,
            FlushEverythingImmediately = 1 << 3,
            RemoveDirtyDetailsImmediately = 1 << 4,
            WillBeDestroyed = 1 << 8,
        }


        private static bool m_IDsInitialized = false;
        private static int _Texture_Array_Albedo;
        private static int _Texture_Array_Normal;
        private static int _Texture_Splat_1;
        private static int _Texture_Splat_2;
        private static int _Texture_Splat_3;
        private static int _Texture_Splat_4;
        private static int _UV_Mix_Power;
        private static int _UV_Mix_Start_Distance;
        private static int _Perlin_Normal_Tiling_Close;
        private static int _Perlin_Normal_Tiling_Far;
        private static int _Perlin_Normal_Power;
        private static int _Perlin_Normal_Power_Close;
        private static int _Terrain_Smoothness;
        private static int _Terrain_Specular;
        private static int _TessValue;
        private static int _TessMin;
        private static int _TessMax;
        private static int _TessPhongStrength;
        private static int _TessDistance;
        private static int _Ambient_Occlusion_Type;

        private static int _Remove_Vert_Height;
        private static int _Texture_Additional_Masks;

        private static int _Use_AO;
        private static int _Use_AO_Texture;
        private static int _Ambient_Occlusion_Power;

        private static int _Texture_Perlin_Normal_Index;
        private static int _Global_Normalmap_Power;
        private static int _Global_Normal_Map;

        private static int _Global_Color_Map_Far_Power;
        private static int _Global_Color_Map_Close_Power;
        private static int _Global_Color_Opacity_Power;
        private static int _Global_Color_Map;

        private static int _Geological_Map_Offset_Close;
        private static int _Geological_Map_Close_Power;
        private static int _Geological_Tiling_Close;
        private static int _Geological_Map_Offset_Far;
        private static int _Geological_Map_Far_Power;
        private static int _Geological_Tiling_Far;
        private static int _Texture_Geological_Map;

        private static int _Texture_Snow_Index;
        private static int _Texture_Snow_Normal_Index;
        private static int _Texture_Snow_H_AO_Index;
        private static int _Texture_Snow_Noise_Index;
        private static int _Snow_Amount;
        private static int _Snow_Maximum_Angle;
        private static int _Snow_Maximum_Angle_Hardness;
        private static int _Snow_Min_Height;
        private static int _Snow_Min_Height_Blending;
        private static int _Snow_Noise_Power;
        private static int _Snow_Noise_Tiling;
        private static int _Snow_Normal_Scale;
        private static int _Snow_Perlin_Power;
        private static int _Snow_Tiling;
        private static int _Snow_Tiling_Far_Multiplier;
        private static int _Snow_Brightness;
        private static int _Snow_Blend_Normal;
        private static int _Snow_Smoothness;
        private static int _Snow_Specular;
        private static int _Snow_Heightblend_Close;
        private static int _Snow_Heightblend_Far;
        private static int _Snow_Height_Contrast;
        private static int _Snow_Heightmap_Depth;
        private static int _Snow_Heightmap_MinHeight;
        private static int _Snow_Heightmap_MaxHeight;
        private static int _Snow_Ambient_Occlusion_Power;
        private static int _Snow_Tesselation_Depth;
        private static int _Snow_Color;
        private static int _Texture_Snow_Average;

        private static int _Texture_Glitter;
        private static int _Gliter_Color_Power;
        private static int _Glitter_Noise_Treshold;
        private static int _Glitter_Specular;
        private static int _Glitter_Smoothness;
        private static int _Glitter_Refreshing_Speed;
        private static int _Glitter_Tiling;

        private static int[] _Texture_X_Albedo_Index = new int[16];
        private static int[] _Texture_X_Normal_Index = new int[16];
        private static int[] _Texture_X_H_AO_Index = new int[16];
        private static int[] _Texture_X_Tiling = new int[16];
        private static int[] _Texture_X_Far_Multiplier = new int[16];
        private static int[] _Texture_X_Perlin_Power = new int[16];
        private static int[] _Texture_X_Snow_Reduction = new int[16];
        private static int[] _Texture_X_Geological_Power = new int[16];
        private static int[] _Texture_X_Heightmap_Depth = new int[16];
        private static int[] _Texture_X_Height_Contrast = new int[16];
        private static int[] _Texture_X_Heightblend_Close = new int[16];
        private static int[] _Texture_X_Heightblend_Far = new int[16];
        private static int[] _Texture_X_Tesselation_Depth = new int[16];
        private static int[] _Texture_X_Heightmap_MinHeight = new int[16];
        private static int[] _Texture_X_Heightmap_MaxHeight = new int[16];
        private static int[] _Texture_X_AO_Power = new int[16];
        private static int[] _Texture_X_Normal_Power = new int[16];
        private static int[] _Texture_X_Triplanar = new int[16];
        private static int[] _Texture_X_Average = new int[16];
        private static int[] _Texture_X_Color = new int[16];

        /// <summary>
        /// Detect terrain changes - use this to request updates if necessary
        /// </summary>
        /// <param name="flags"></param>
        void OnTerrainChanged(int flags)
        {
            //Debug.Log((TerrainChangedFlags)flags);
            if ((TerrainChangedFlags) flags == TerrainChangedFlags.Heightmap ||
                (TerrainChangedFlags) flags == TerrainChangedFlags.DelayedHeightmapUpdate ||
                (TerrainChangedFlags)flags == TerrainChangedFlags.FlushEverythingImmediately)
            {
                //m_shaderNormalsUpdateRequested = true;
            }
        }

        /// <summary>
        /// Initialize the shader ID's for faster shader updates
        /// </summary>
        private void InitializeIDs()
        {
            m_IDsInitialized = true;
            _Texture_Array_Albedo = Shader.PropertyToID("_Texture_Array_Albedo");
            _Texture_Array_Normal = Shader.PropertyToID("_Texture_Array_Normal");
            _Texture_Splat_1 = Shader.PropertyToID("_Texture_Splat_1");
            _Texture_Splat_2 = Shader.PropertyToID("_Texture_Splat_2");
            _Texture_Splat_3 = Shader.PropertyToID("_Texture_Splat_3");
            _Texture_Splat_4 = Shader.PropertyToID("_Texture_Splat_4");
            _UV_Mix_Power = Shader.PropertyToID("_UV_Mix_Power");
            _UV_Mix_Start_Distance = Shader.PropertyToID("_UV_Mix_Start_Distance");
            _Perlin_Normal_Tiling_Close = Shader.PropertyToID("_Perlin_Normal_Tiling_Close");
            _Perlin_Normal_Tiling_Far = Shader.PropertyToID("_Perlin_Normal_Tiling_Far");
            _Perlin_Normal_Power = Shader.PropertyToID("_Perlin_Normal_Power");
            _Perlin_Normal_Power_Close = Shader.PropertyToID("_Perlin_Normal_Power_Close");
            _Terrain_Smoothness = Shader.PropertyToID("_Terrain_Smoothness");
            _Terrain_Specular = Shader.PropertyToID("_Terrain_Specular");
            _TessValue = Shader.PropertyToID("_TessValue");
            _TessMin = Shader.PropertyToID("_TessMin");
            _TessMax = Shader.PropertyToID("_TessMax");
            _TessPhongStrength = Shader.PropertyToID("_TessPhongStrength");
            _TessDistance = Shader.PropertyToID("_TessDistance");
            _Ambient_Occlusion_Type = Shader.PropertyToID("_Ambient_Occlusion_Type");

            _Remove_Vert_Height = Shader.PropertyToID("_Remove_Vert_Height");
            _Texture_Additional_Masks = Shader.PropertyToID("_Texture_Additional_Masks");

            _Use_AO = Shader.PropertyToID("_Use_AO");
            _Use_AO_Texture = Shader.PropertyToID("_Use_AO_Texture");
            _Ambient_Occlusion_Power = Shader.PropertyToID("_Ambient_Occlusion_Power");

            _Texture_Perlin_Normal_Index = Shader.PropertyToID("_Texture_Perlin_Normal_Index");
            _Global_Normalmap_Power = Shader.PropertyToID("_Global_Normalmap_Power");
            _Global_Normal_Map = Shader.PropertyToID("_Global_Normal_Map");

            _Global_Color_Map_Far_Power = Shader.PropertyToID("_Global_Color_Map_Far_Power");
            _Global_Color_Map_Close_Power = Shader.PropertyToID("_Global_Color_Map_Close_Power");
            _Global_Color_Opacity_Power = Shader.PropertyToID("_Global_Color_Opacity_Power");
            _Global_Color_Map = Shader.PropertyToID("_Global_Color_Map");

            _Geological_Map_Offset_Close = Shader.PropertyToID("_Geological_Map_Offset_Close");
            _Geological_Map_Close_Power = Shader.PropertyToID("_Geological_Map_Close_Power");
            _Geological_Tiling_Close = Shader.PropertyToID("_Geological_Tiling_Close");
            _Geological_Map_Offset_Far = Shader.PropertyToID("_Geological_Map_Offset_Far");
            _Geological_Map_Far_Power = Shader.PropertyToID("_Geological_Map_Far_Power");
            _Geological_Tiling_Far = Shader.PropertyToID("_Geological_Tiling_Far");
            _Texture_Geological_Map = Shader.PropertyToID("_Texture_Geological_Map");

            _Texture_Snow_Index = Shader.PropertyToID("_Texture_Snow_Index");
            _Texture_Snow_Normal_Index = Shader.PropertyToID("_Texture_Snow_Normal_Index");
            _Texture_Snow_H_AO_Index = Shader.PropertyToID("_Texture_Snow_H_AO_Index");
            _Texture_Snow_Noise_Index = Shader.PropertyToID("_Texture_Snow_Noise_Index");
            _Snow_Amount = Shader.PropertyToID("_Snow_Amount");
            _Snow_Maximum_Angle = Shader.PropertyToID("_Snow_Maximum_Angle");
            _Snow_Maximum_Angle_Hardness = Shader.PropertyToID("_Snow_Maximum_Angle_Hardness");
            _Snow_Min_Height = Shader.PropertyToID("_Snow_Min_Height");
            _Snow_Min_Height_Blending = Shader.PropertyToID("_Snow_Min_Height_Blending");
            _Snow_Noise_Power = Shader.PropertyToID("_Snow_Noise_Power");
            _Snow_Noise_Tiling = Shader.PropertyToID("_Snow_Noise_Tiling");
            _Snow_Normal_Scale = Shader.PropertyToID("_Snow_Normal_Scale");
            _Snow_Perlin_Power = Shader.PropertyToID("_Snow_Perlin_Power");
            _Snow_Tiling = Shader.PropertyToID("_Snow_Tiling");
            _Snow_Tiling_Far_Multiplier = Shader.PropertyToID("_Snow_Tiling_Far_Multiplier");
            _Snow_Brightness = Shader.PropertyToID("_Snow_Brightness");
            _Snow_Blend_Normal = Shader.PropertyToID("_Snow_Blend_Normal");
            _Snow_Smoothness = Shader.PropertyToID("_Snow_Smoothness");
            _Snow_Specular = Shader.PropertyToID("_Snow_Specular");
            _Snow_Heightblend_Close = Shader.PropertyToID("_Snow_Heightblend_Close");
            _Snow_Heightblend_Far = Shader.PropertyToID("_Snow_Heightblend_Far");
            _Snow_Height_Contrast = Shader.PropertyToID("_Snow_Height_Contrast");
            _Snow_Heightmap_Depth = Shader.PropertyToID("_Snow_Heightmap_Depth");
            _Snow_Heightmap_MinHeight = Shader.PropertyToID("_Snow_Heightmap_MinHeight");
            _Snow_Heightmap_MaxHeight = Shader.PropertyToID("_Snow_Heightmap_MaxHeight");
            _Snow_Ambient_Occlusion_Power = Shader.PropertyToID("_Snow_Ambient_Occlusion_Power");
            _Snow_Tesselation_Depth = Shader.PropertyToID("_Snow_Tesselation_Depth");
            _Snow_Color = Shader.PropertyToID("_Snow_Color");
            _Texture_Snow_Average = Shader.PropertyToID("_Texture_Snow_Average");

            _Texture_Glitter = Shader.PropertyToID("_Texture_Glitter");
            _Gliter_Color_Power = Shader.PropertyToID("_Gliter_Color_Power");
            _Glitter_Noise_Treshold = Shader.PropertyToID("_Glitter_Noise_Treshold");
            _Glitter_Specular = Shader.PropertyToID("_Glitter_Specular");
            _Glitter_Smoothness = Shader.PropertyToID("_Glitter_Smoothness");
            _Glitter_Refreshing_Speed = Shader.PropertyToID("_Glitter_Refreshing_Speed");
            _Glitter_Tiling = Shader.PropertyToID("_Glitter_Tiling");

            _Texture_X_Albedo_Index = new int[16];
            _Texture_X_Normal_Index = new int[16];
            _Texture_X_H_AO_Index = new int[16];
            _Texture_X_Tiling = new int[16];
            _Texture_X_Far_Multiplier = new int[16];
            _Texture_X_Perlin_Power = new int[16];
            _Texture_X_Snow_Reduction = new int[16];
            _Texture_X_Geological_Power = new int[16];
            _Texture_X_Heightmap_Depth = new int[16];
            _Texture_X_Height_Contrast = new int[16];
            _Texture_X_Heightblend_Close = new int[16];
            _Texture_X_Heightblend_Far = new int[16];
            _Texture_X_Tesselation_Depth = new int[16];
            _Texture_X_Heightmap_MinHeight = new int[16];
            _Texture_X_Heightmap_MaxHeight = new int[16];
            _Texture_X_AO_Power = new int[16];
            _Texture_X_Normal_Power = new int[16];
            _Texture_X_Triplanar = new int[16];
            _Texture_X_Average = new int[16];
            _Texture_X_Color = new int[16];
            for (int i = 1; i <= 16; i++)
            {
                _Texture_X_Albedo_Index[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Albedo_Index", i));
                _Texture_X_Normal_Index[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Normal_Index", i));
                _Texture_X_H_AO_Index[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_H_AO_Index", i));
                _Texture_X_Tiling[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Tiling", i));
                _Texture_X_Far_Multiplier[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Far_Multiplier", i));
                _Texture_X_Perlin_Power[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Perlin_Power", i));
                _Texture_X_Snow_Reduction[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Snow_Reduction", i));
                _Texture_X_Geological_Power[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Geological_Power", i));
                _Texture_X_Heightmap_Depth[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Heightmap_Depth", i));
                _Texture_X_Height_Contrast[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Height_Contrast", i));
                _Texture_X_Heightblend_Close[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Heightblend_Close", i));
                _Texture_X_Heightblend_Far[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Heightblend_Far", i));
                _Texture_X_Tesselation_Depth[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Tesselation_Depth", i));
                _Texture_X_Heightmap_MinHeight[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Heightmap_MinHeight", i));
                _Texture_X_Heightmap_MaxHeight[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Heightmap_MaxHeight", i));
                _Texture_X_AO_Power[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_AO_Power", i));
                _Texture_X_Normal_Power[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Normal_Power", i));
                _Texture_X_Triplanar[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Triplanar", i));
                _Texture_X_Average[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Average", i));
                _Texture_X_Color[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Color", i));
            }
        }

        /// <summary>
        /// Called when scene loads
        /// </summary>
        void Awake()
        {
            //Make sure the ID's are initialized
            if (!m_IDsInitialized)
            {
                InitializeIDs();
            }

            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogWarning("CTS needs a terrain, exiting!");
                    return;
                }
            }

            //Work around for a nasty unity bug
            if (m_profile != null)
            {
                m_terrain.materialType = Terrain.MaterialType.BuiltInStandard;
                m_terrain.materialTemplate = null;
                m_material = null;
                m_terrain.Flush();
            }
        }

        /// <summary>
        /// Called when scene starts
        /// </summary>
        void Start()
        {
            //Assign terrain if missing
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogWarning("CTS needs a terrain, exiting!");
                    return;
                }
            }

            //Register this shader
            CTSTerrainManager.Instance.RegisterShader(this);

            //Pick up the splats from the terrain if they are missing
            if (m_splat1 == null && m_terrain.terrainData.alphamapTextures.Length > 0)
            {
                m_splat1 = m_terrain.terrainData.alphamapTextures[0];
            }
            if (m_splat2 == null && m_terrain.terrainData.alphamapTextures.Length > 1)
            {
                m_splat2 = m_terrain.terrainData.alphamapTextures[1];
            }
            if (m_splat3 == null && m_terrain.terrainData.alphamapTextures.Length > 2)
            {
                m_splat3 = m_terrain.terrainData.alphamapTextures[2];
            }
            if (m_splat4 == null && m_terrain.terrainData.alphamapTextures.Length > 3)
            {
                m_splat4 = m_terrain.terrainData.alphamapTextures[3];
            }

            //Setup the materials on the terrain
            if (m_profile != null)
            {
                ApplyShaderMaterial();
            }
        }

        /// <summary>
        /// Called when scene is destroyed
        /// </summary>
        void OnDestroy()
        {
            //Remover shader from management so that it can go out of scope nicely
            if (Application.isPlaying)
            {
                CTSTerrainManager mgr = CTSTerrainManager.Instance;
                if (mgr != null)
                {
                    CTSTerrainManager.Instance.RemoveShader(this);
                }
            }
        }

        /// <summary>
        /// Returns the first asset that matches the file path and name passed. Will try
        /// full path first, then will try just the file name.
        /// </summary>
        /// <param name="fileNameOrPath">File name as standalone or fully pathed</param>
        /// <returns>Object or null if it was not found</returns>
        public static UnityEngine.Object GetAsset(string fileNameOrPath, Type assetType)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(fileNameOrPath))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(fileNameOrPath, assetType);
                if (obj != null)
                {
                    return obj;
                }
                else
                {
                    string path = GetAssetPath(Path.GetFileName(fileNameOrPath));
                    if (!string.IsNullOrEmpty(path))
                    {
                        return AssetDatabase.LoadAssetAtPath(path, assetType);
                    }
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="fileName">File name to search for</param>
        /// <returns></returns>
        public static string GetAssetPath(string fileName)
        {
#if UNITY_EDITOR
            string fName = Path.GetFileNameWithoutExtension(fileName);
            string[] assets = AssetDatabase.FindAssets(fName, null);
            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[idx]);
                if (Path.GetFileName(path) == fileName)
                {
                    return path;
                }
            }
#endif
            return "";
        }

        /// <summary>
        /// Get the specified type if it exists
        /// </summary>
        /// <param name="TypeName">Name of the type to load</param>
        /// <returns>Selected type or null</returns>
        public static Type GetType(string TypeName)
        {

            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType(TypeName);

            // If it worked, then we're done here
            if (type != null)
                return type;

            // If the TypeName is a full name, then we can try loading the defining assembly directly
            if (TypeName.Contains("."))
            {
                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

                // Attempt to load the indicated Assembly
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    if (assembly == null)
                        return null;

                    // Ask that assembly to return the proper Type
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
                catch (Exception)
                {
                    //Debug.Log("Unable to load assemmbly : " + ex.Message);
                }
            }

            // If we still haven't found the proper type, we can enumerate all of the 
            // loaded assemblies and see if any of them define the type
            var currentAssembly = Assembly.GetCallingAssembly();
            {
                // Load the referenced assembly
                if (currentAssembly != null)
                {
                    // See if that assembly defines the named type
                    type = currentAssembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }

            }

            //All loaded assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int asyIdx = 0; asyIdx < assemblies.GetLength(0); asyIdx++)
            {
                type = assemblies[asyIdx].GetType(TypeName);
                if (type != null)
                {
                    return type;
                }
            }

            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                // Load the referenced assembly
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    // See if that assembly defines the named type
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
            }

            // The type just couldn't be found...
            return null;
        }

        /// <summary>
        /// Apply the unity shader
        /// </summary>
        private void ApplyUnityShader()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogError("Unable to locate Terrain, apply unity shader cancelled.");
                    return;
                }
            }

            //Restore splats and textures if possible
            if (Application.isPlaying && m_profile != null)
            {
                if (m_splatBackupArray != null && m_splatBackupArray.GetLength(0) > 0)
                {
                    //Debug.Log("Restoring terrain textures and splatmaps");

                    //First restore textures from profile
                    ReplaceTerrainTexturesFromProfile();

                    //Then restore the splats
                    m_terrain.terrainData.SetAlphamaps(0, 0, m_splatBackupArray);

                    //And signal it
                    m_terrain.Flush();

                    //Mark terrain as dirty
                    SetDirty(m_terrain, false, false);
                }
            }

            //Apply it
            m_terrain.basemapDistance = 5000f;
            m_activeShaderType = CTSConstants.ShaderType.Unity;
            m_terrain.materialType = Terrain.MaterialType.BuiltInStandard;
            m_terrain.materialTemplate = null;
            m_material = null;
            SetDirty(this, false, false);
            SetDirty(m_terrain, false, false);
        }

        /// <summary>
        /// Set up correct material for use with the shader
        /// </summary>
        private void ApplyShaderMaterial()
        {
            //Debug.Log("Apply Shader Material");

            //Make sure we have a terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogWarning("CTS needs terrain to function - exiting!");
                    return;
                }
            }

            //Make sure we have a profile
            if (m_profile == null)
            {
                Debug.LogWarning("CTS needs a profile to function - applying unity shader and exiting!");
                ApplyUnityShader();
                return;
            }

            //Grab terrain splats if missing
            if (m_splat1 == null && m_terrain.terrainData.alphamapTextures.Length > 0)
            {
                m_splat1 = m_terrain.terrainData.alphamapTextures[0];
            }
            if (m_splat2 == null && m_terrain.terrainData.alphamapTextures.Length > 1)
            {
                m_splat2 = m_terrain.terrainData.alphamapTextures[1];
            }
            if (m_splat3 == null && m_terrain.terrainData.alphamapTextures.Length > 2)
            {
                m_splat3 = m_terrain.terrainData.alphamapTextures[2];
            }
            if (m_splat4 == null && m_terrain.terrainData.alphamapTextures.Length > 3)
            {
                m_splat4 = m_terrain.terrainData.alphamapTextures[3];
            }

            Shader shader = null;
            m_activeShaderType = m_profile.ShaderType;
            switch (m_activeShaderType)
            {
                case CTSConstants.ShaderType.Unity:
                    ApplyUnityShader();
                    return;
                case CTSConstants.ShaderType.Lite:
                    shader = Shader.Find(CTSConstants.CTSShaderLiteName);
                    break;
                case CTSConstants.ShaderType.Basic:
                    if (!m_useCutout)
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderBasicName);
                    }
                    else
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderBasicCutoutName);
                    }
                    break;
                case CTSConstants.ShaderType.Advanced:
                    if (!m_useCutout)
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderAdvancedName);
                    }
                    else
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderAdvancedCutoutName);
                    }
                    break;
                case CTSConstants.ShaderType.Tesselation:
                    if (!m_useCutout)
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderTesselatedName);
                    }
                    else
                    {
                        shader = Shader.Find(CTSConstants.CTSShaderTesselatedCutoutName);
                    }
                    break;
            }

            //Exit if necessary
            if (shader == null)
            {
                Debug.LogWarning("CTS could not locate the shader - exiting!");
                return;
            }

            //Make sure the profile has texture arrays
            if (m_profile.AlbedosTextureArray == null)
            {
                Debug.LogWarning("CTS profile needs albedos texture array to function - applying unity shader and exiting!");
                m_profile.m_needsAlbedosArrayUpdate = true;
                ApplyUnityShader();
                return;
            }
            if (m_profile.NormalsTextureArray == null)
            {
                Debug.LogWarning("CTS profile needs normals texture array to function - applying unity shader and exiting!");
                m_profile.m_needsNormalsArrayUpdate = true;
                ApplyUnityShader();
                return;
            }

            //Get material path
            string materialName = string.Format("{0}_{1}", m_terrain.name, Mathf.Abs(m_terrain.GetInstanceID()));
            #if UNITY_EDITOR
            string materialPath = string.Format("{0}Terrains/{1}.mat", m_ctsDirectory, materialName);
            #endif
            if (m_material != null)
            {
                materialName = m_material.name;
                #if UNITY_EDITOR
                string path = AssetDatabase.GetAssetPath(m_material);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    materialPath = path;
                }
                #endif
            }

            //Create a new material - we always do this even when persisting it
            //Debug.Log("Creating new material");
            m_material = new Material(shader);
            m_material.name = materialName;
            m_material.hideFlags = HideFlags.HideInInspector;

            //Set it up
            UpdateShader();

            //Persist if necessary
            #if UNITY_EDITOR
            if (!Application.isPlaying && m_profile.m_persistMaterials)
            {
                //Debug.Log("Persisting materials");

                Directory.CreateDirectory(m_ctsDirectory + "Terrains/");
                if (!File.Exists(materialPath))
                {
                    //Debug.Log("Creating new material");
                    AssetDatabase.CreateAsset(m_material, materialPath);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    //Debug.Log("Updating existing material");
                    var oldMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    EditorUtility.CopySerialized(m_material, oldMaterial);
                    m_material = oldMaterial;
                    AssetDatabase.SaveAssets();
                }
                AssetDatabase.Refresh();
            }
            #endif

            //Apply it to terrain
            m_terrain.materialType = Terrain.MaterialType.Custom;
            m_terrain.materialTemplate = m_material;

            //Replace with optimal textures
            UpdateTerrainSplatsAtRuntime();

            //Set dirty
            SetDirty(this, false, false);
            SetDirty(m_material, false, false);
            SetDirty(m_terrain, false, false);
        }

        /// <summary>
        /// Return true if we need a material update
        /// </summary>
        /// <returns>True if we need a material update</returns>
        private bool NeedsMaterialUpdate()
        {
            //Cant do anything without a profile
            if (m_profile == null)
            {
                return false;
            }

            //Cant do anything without terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    return false;
                }
            }

            //Check active shader type vs profile shader type
            if (m_activeShaderType != m_profile.ShaderType)
            {
                return true;
            }

            //Whats on the terrain
            if (m_activeShaderType == CTSConstants.ShaderType.Unity)
            {
                if (m_material != null)
                {
                    return true;
                }
                if (m_terrain.materialType == Terrain.MaterialType.Custom)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (m_material == null)
                {
                    return true;
                }
                if (m_terrain.materialType != Terrain.MaterialType.Custom)
                {
                    return true;
                }
                if (m_terrain.materialTemplate == null)
                {
                    return true;
                }
                if (m_terrain.materialTemplate.shader == null)
                {
                    return true;
                }
                if (!m_terrain.materialTemplate.shader.name.Contains(CTSConstants.CTSShaderName))
                {
                    return true;
                }
            }

            //Check persistence status
            #if UNITY_EDITOR
            if (m_profile.m_persistMaterials)
            {
                if (!EditorUtility.IsPersistent(m_material))
                {
                    return true;
                }
            }
            else
            {
                if (EditorUtility.IsPersistent(m_material))
                {
                    return true;
                }
            }
            #endif

            //Check what material is on the terrain
            string shaderName = m_terrain.materialTemplate.shader.name;

            //Is the material the right type
            switch (m_activeShaderType)
            {
                case CTSConstants.ShaderType.Lite:
                    return CTSConstants.CTSShaderLiteName != shaderName;
                case CTSConstants.ShaderType.Basic:
                    if (!m_useCutout)
                    {
                        return CTSConstants.CTSShaderBasicName != shaderName;
                    }
                    else
                    {
                        return CTSConstants.CTSShaderBasicCutoutName != shaderName;
                    }
                case CTSConstants.ShaderType.Advanced:
                    if (!m_useCutout)
                    {
                        return CTSConstants.CTSShaderAdvancedName != shaderName;
                    }
                    else
                    {
                        return CTSConstants.CTSShaderAdvancedCutoutName != shaderName;
                    }
                case CTSConstants.ShaderType.Tesselation:
                    if (!m_useCutout)
                    {
                        return CTSConstants.CTSShaderTesselatedName != shaderName;
                    }
                    else
                    {
                        return CTSConstants.CTSShaderTesselatedCutoutName != shaderName;
                    }
            }
            return false;
        }

        /// <summary>
        /// Update the material if necessary and the shader
        /// </summary>
        public void UpdateMaterialAndShader()
        {
            //Debug.Log("Apply Material And Update");
            if (NeedsMaterialUpdate())
            {
                ApplyShaderMaterial();
            }
            else
            {
                UpdateShader();
            }
        }

        /// <summary>
        /// Update the shader settings into the terrain
        /// </summary>
        private void UpdateShader()
        {
            //Debug.Log("Update shader called");

            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogWarning("CTS missing terrain, cannot operate without terrain!");
                    return;
                }
            }

            //Make sure we have profile
            if (m_profile == null)
            {
                return;
            }

            //And albedo tex
            if (m_profile.AlbedosTextureArray == null)
            {
                Debug.LogError("CTS Albedos texture array is missing - rebake textures");
                return;
            }

            //And normal tex
            if (m_profile.NormalsTextureArray == null)
            {
                Debug.LogError("CTS Normals texture array is missing - rebake textures");
                return;
            }

            //And splat tex
            if (m_splat1 == null)
            {
                Debug.LogError("CTS missing splat textures - exiting");
                return;
            }

            //Exit if unity shader
            if (m_activeShaderType == CTSConstants.ShaderType.Unity)
            {
                m_terrain.basemapDistance = 5000f;
                return;
            }

            //Basemap distance
            if (m_terrain.basemapDistance != m_profile.m_globalBasemapDistance)
            {
                m_terrain.basemapDistance = m_profile.m_globalBasemapDistance;
                SetDirty(m_terrain, false, false);
            }

            //Initialize
            if (!m_IDsInitialized)
            {
                InitializeIDs();
            }

            //Albedo's
            m_material.SetTexture(_Texture_Array_Albedo, m_profile.AlbedosTextureArray);

            //Normals
            m_material.SetTexture(_Texture_Array_Normal, m_profile.NormalsTextureArray);

            //Splats
            m_material.SetTexture(_Texture_Splat_1, m_splat1);
            m_material.SetTexture(_Texture_Splat_2, m_splat2);
            m_material.SetTexture(_Texture_Splat_3, m_splat3);
            m_material.SetTexture(_Texture_Splat_4, m_splat4);

            //Global settings
            m_material.SetFloat(_UV_Mix_Power, m_profile.m_globalUvMixPower);
            m_material.SetFloat(_UV_Mix_Start_Distance, m_profile.m_globalUvMixStartDistance + UnityEngine.Random.Range(0.001f, 0.003f));
            m_material.SetFloat(_Perlin_Normal_Tiling_Close, m_profile.m_globalDetailNormalCloseTiling);
            m_material.SetFloat(_Perlin_Normal_Tiling_Far, m_profile.m_globalDetailNormalFarTiling);
            m_material.SetFloat(_Perlin_Normal_Power, m_profile.m_globalDetailNormalFarPower);
            m_material.SetFloat(_Perlin_Normal_Power_Close, m_profile.m_globalDetailNormalClosePower);
            m_material.SetFloat(_Terrain_Smoothness, m_profile.m_globalTerrainSmoothness);
            m_material.SetFloat(_Terrain_Specular, m_profile.m_globalTerrainSpecular);
            m_material.SetFloat(_TessValue, m_profile.m_globalTesselationPower);
            m_material.SetFloat(_TessMin, m_profile.m_globalTesselationMinDistance);
            m_material.SetFloat(_TessMax, m_profile.m_globalTesselationMaxDistance);
            m_material.SetFloat(_TessPhongStrength, m_profile.m_globalTesselationPhongStrength);
            m_material.SetFloat(_TessDistance, m_profile.m_globalTesselationMaxDistance);
            m_material.SetInt(_Ambient_Occlusion_Type, (int)m_profile.m_globalAOType);

            //Cutout
            m_material.SetFloat(_Remove_Vert_Height, m_cutoutHeight);
            m_material.SetTexture(_Texture_Additional_Masks, m_cutoutMask);

            //AO
            if (m_profile.m_globalAOType == CTSConstants.AOType.None)
            {
                m_material.DisableKeyword("_Use_AO_ON");
                m_material.DisableKeyword("_USE_AO_TEXTURE_ON");
                m_material.SetInt(_Use_AO, 0);
                m_material.SetInt(_Use_AO_Texture, 0);
                m_material.SetFloat(_Ambient_Occlusion_Power, 0f);
            }
            else if (m_profile.m_globalAOType == CTSConstants.AOType.NormalMapBased)
            {
                m_material.DisableKeyword("_USE_AO_TEXTURE_ON");
                m_material.SetInt(_Use_AO_Texture, 0);
                if (m_profile.m_globalAOPower > 0)
                {
                    m_material.EnableKeyword("_USE_AO_ON");
                    m_material.SetInt(_Use_AO, 1);
                    m_material.SetFloat(_Ambient_Occlusion_Power, m_profile.m_globalAOPower);
                }
                else
                {
                    m_material.DisableKeyword("_USE_AO_ON");
                    m_material.SetInt(_Use_AO, 0);
                    m_material.SetFloat(_Ambient_Occlusion_Power, 0f);
                }
            }
            else
            {
                if (m_profile.m_globalAOPower > 0)
                {
                    m_material.EnableKeyword("_USE_AO_ON");
                    m_material.EnableKeyword("_USE_AO_TEXTURE_ON");
                    m_material.SetInt(_Use_AO, 1);
                    m_material.SetInt(_Use_AO_Texture, 1);
                    m_material.SetFloat(_Ambient_Occlusion_Power, m_profile.m_globalAOPower);
                }
                else
                {
                    m_material.DisableKeyword("_USE_AO_ON");
                    m_material.DisableKeyword("_USE_AO_TEXTURE_ON");
                    m_material.SetInt(_Use_AO, 0);
                    m_material.SetInt(_Use_AO_Texture, 0);
                    m_material.SetFloat(_Ambient_Occlusion_Power, 0f);
                }
            }

            //Global Detail
            if (m_profile.m_globalDetailNormalClosePower > 0f || m_profile.m_globalDetailNormalFarPower > 0f)
            {
                m_material.SetInt(_Texture_Perlin_Normal_Index, m_profile.m_globalDetailNormalMapIdx);
            }
            else
            {
                m_material.SetInt(_Texture_Perlin_Normal_Index, -1);
            }

            //Global Normal Map
            if (NormalMap != null)
            {
                m_material.SetFloat(_Global_Normalmap_Power, m_profile.m_globalNormalPower);
                if (m_profile.m_globalNormalPower > 0f)
                {
                    m_material.SetTexture(_Global_Normal_Map, NormalMap);
                }
                else
                {
                    m_material.SetTexture(_Global_Normal_Map, null);
                }
            }
            else
            {
                m_material.SetFloat(_Global_Normalmap_Power, 0f);
                m_material.SetTexture(_Global_Normal_Map, null);
            }

            //Colormap settings
            if (ColorMap != null)
            {
                m_material.SetFloat(_Global_Color_Map_Far_Power, m_profile.m_colorMapFarPower);
                m_material.SetFloat(_Global_Color_Map_Close_Power, m_profile.m_colorMapClosePower);
                m_material.SetFloat(_Global_Color_Opacity_Power, m_profile.m_colorMapOpacity);
                if (m_profile.m_colorMapFarPower > 0f || m_profile.m_colorMapClosePower > 0f)
                {
                    m_material.SetTexture(_Global_Color_Map, ColorMap);
                }
                else
                {
                    m_material.SetTexture(_Global_Color_Map, null);
                }
            }
            else
            {
                m_material.SetFloat(_Global_Color_Map_Far_Power, 0f);
                m_material.SetFloat(_Global_Color_Map_Close_Power, 0f);
                m_material.SetFloat(_Global_Color_Opacity_Power, 0f);
                m_material.SetTexture(_Global_Color_Map, null);
            }

            //Geological settings
            if (m_profile.GeoAlbedo != null)
            {
                if (m_profile.m_geoMapClosePower > 0f || m_profile.m_geoMapFarPower > 0f)
                {
                    m_material.SetFloat(_Geological_Map_Offset_Close, m_profile.m_geoMapCloseOffset);
                    m_material.SetFloat(_Geological_Map_Close_Power, m_profile.m_geoMapClosePower);
                    m_material.SetFloat(_Geological_Tiling_Close, m_profile.m_geoMapTilingClose);
                    m_material.SetFloat(_Geological_Map_Offset_Far, m_profile.m_geoMapFarOffset);
                    m_material.SetFloat(_Geological_Map_Far_Power, m_profile.m_geoMapFarPower);
                    m_material.SetFloat(_Geological_Tiling_Far, m_profile.m_geoMapTilingFar);
                    m_material.SetTexture(_Texture_Geological_Map, m_profile.GeoAlbedo);
                }
                else
                {
                    m_material.SetFloat(_Geological_Map_Close_Power, 0f);
                    m_material.SetFloat(_Geological_Map_Far_Power, 0f);
                    m_material.SetTexture(_Texture_Geological_Map, null);
                }
            }
            else
            {
                m_material.SetFloat(_Geological_Map_Close_Power, 0f);
                m_material.SetFloat(_Geological_Map_Far_Power, 0f);
                m_material.SetTexture(_Texture_Geological_Map, null);
            }

            //Snow settings
            m_material.SetFloat(_Snow_Amount, m_profile.m_snowAmount);
            m_material.SetInt(_Texture_Snow_Index, m_profile.m_snowAlbedoTextureIdx);
            m_material.SetInt(_Texture_Snow_Normal_Index, m_profile.m_snowNormalTextureIdx);
            m_material.SetInt(_Texture_Snow_H_AO_Index, m_profile.m_snowHeightTextureIdx != -1 ? m_profile.m_snowHeightTextureIdx : m_profile.m_snowAOTextureIdx);
            m_material.SetInt(_Texture_Snow_Noise_Index, m_profile.m_snowNoiseTextureIdx);

            m_material.SetTexture(_Texture_Glitter, m_profile.SnowGlitter);
            m_material.SetFloat(_Snow_Maximum_Angle, m_profile.m_snowMaxAngle);
            m_material.SetFloat(_Snow_Maximum_Angle_Hardness, m_profile.m_snowMaxAngleHardness);
            m_material.SetFloat(_Snow_Min_Height, m_profile.m_snowMinHeight);
            m_material.SetFloat(_Snow_Min_Height_Blending, m_profile.m_snowMinHeightBlending);
            m_material.SetFloat(_Snow_Noise_Power, m_profile.m_snowNoisePower);
            m_material.SetFloat(_Snow_Noise_Tiling, m_profile.m_snowNoiseTiling);
            m_material.SetFloat(_Snow_Normal_Scale, m_profile.m_snowNormalScale);
            m_material.SetFloat(_Snow_Perlin_Power, m_profile.m_snowDetailPower);
            m_material.SetFloat(_Snow_Tiling, m_profile.m_snowTilingClose);
            m_material.SetFloat(_Snow_Tiling_Far_Multiplier, m_profile.m_snowTilingFar);
            m_material.SetFloat(_Snow_Brightness, m_profile.m_snowBrightness);
            m_material.SetFloat(_Snow_Blend_Normal, m_profile.m_snowBlendNormal);
            m_material.SetFloat(_Snow_Smoothness, m_profile.m_snowSmoothness);
            m_material.SetFloat(_Snow_Specular, m_profile.m_snowSpecular);
            m_material.SetFloat(_Snow_Heightblend_Close, m_profile.m_snowHeightmapBlendClose);
            m_material.SetFloat(_Snow_Heightblend_Far, m_profile.m_snowHeightmapBlendFar);
            m_material.SetFloat(_Snow_Height_Contrast, m_profile.m_snowHeightmapContrast);
            m_material.SetFloat(_Snow_Heightmap_Depth, m_profile.m_snowHeightmapDepth);
            m_material.SetFloat(_Snow_Heightmap_MinHeight, m_profile.m_snowHeightmapMinValue);
            m_material.SetFloat(_Snow_Heightmap_MaxHeight, m_profile.m_snowHeightmapMaxValue);
            m_material.SetFloat(_Snow_Ambient_Occlusion_Power, m_profile.m_snowAOStrength);
            m_material.SetFloat(_Snow_Tesselation_Depth, m_profile.m_snowTesselationDepth);
            m_material.SetVector(_Snow_Color, new Vector4(m_profile.m_snowTint.r * m_profile.m_snowBrightness, m_profile.m_snowTint.g * m_profile.m_snowBrightness, m_profile.m_snowTint.b * m_profile.m_snowBrightness, m_profile.m_snowSmoothness));
            m_material.SetVector(_Texture_Snow_Average, m_profile.m_snowAverage);

            m_material.SetFloat(_Gliter_Color_Power, m_profile.m_snowGlitterColorPower);
            m_material.SetFloat(_Glitter_Noise_Treshold, m_profile.m_snowGlitterNoiseThreshold);
            m_material.SetFloat(_Glitter_Specular, m_profile.m_snowGlitterSpecularPower);
            m_material.SetFloat(_Glitter_Smoothness, m_profile.m_snowGlitterSmoothness);
            m_material.SetFloat(_Glitter_Refreshing_Speed, m_profile.m_snowGlitterRefreshSpeed);
            m_material.SetFloat(_Glitter_Tiling, m_profile.m_snowGlitterTiling);

            //Push per texture based settings
            CTSTerrainTextureDetails td;
            for (int i = 0; i < m_profile.TerrainTextures.Count; i++)
            {
                td = m_profile.TerrainTextures[i];

                m_material.SetInt(_Texture_X_Albedo_Index[i], td.m_albedoIdx);
                m_material.SetInt(_Texture_X_Normal_Index[i], td.m_normalIdx);
                m_material.SetInt(_Texture_X_H_AO_Index[i], td.m_heightIdx != -1 ? td.m_heightIdx : td.m_aoIdx);

                m_material.SetFloat(_Texture_X_Tiling[i], td.m_albedoTilingClose);
                m_material.SetFloat(_Texture_X_Far_Multiplier[i], td.m_albedoTilingFar);
                m_material.SetFloat(_Texture_X_Perlin_Power[i], td.m_detailPower);
                m_material.SetFloat(_Texture_X_Snow_Reduction[i], td.m_snowReductionPower);
                m_material.SetFloat(_Texture_X_Geological_Power[i], td.m_geologicalPower);
                m_material.SetFloat(_Texture_X_Heightmap_Depth[i], td.m_heightDepth);
                m_material.SetFloat(_Texture_X_Height_Contrast[i], td.m_heightContrast);
                m_material.SetFloat(_Texture_X_Heightblend_Close[i], td.m_heightBlendClose);
                m_material.SetFloat(_Texture_X_Heightblend_Far[i], td.m_heightBlendFar);
                m_material.SetFloat(_Texture_X_Tesselation_Depth[i], td.m_heightTesselationDepth);
                m_material.SetFloat(_Texture_X_Heightmap_MinHeight[i], td.m_heightMin);
                m_material.SetFloat(_Texture_X_Heightmap_MaxHeight[i], td.m_heightMax);
                m_material.SetFloat(_Texture_X_AO_Power[i], td.m_aoPower);
                m_material.SetFloat(_Texture_X_Normal_Power[i], td.m_normalStrength);
                m_material.SetFloat(_Texture_X_Triplanar[i], td.m_triplanar ? 1f : 0f);
                m_material.SetVector(_Texture_X_Average[i], td.m_albedoAverage);
                m_material.SetVector(_Texture_X_Color[i], new Vector4(td.m_tint.r * td.m_tintBrightness, td.m_tint.g * td.m_tintBrightness, td.m_tint.b * td.m_tintBrightness, td.m_smoothness));
            }

            //And fill out rest as well
            for (int i = m_profile.TerrainTextures.Count; i < 16; i++)
            {
                m_material.SetInt(_Texture_X_Albedo_Index[i], -1);
                m_material.SetInt(_Texture_X_Normal_Index[i], -1);
                m_material.SetInt(_Texture_X_H_AO_Index[i], -1);
            }

            //Mark the material as dirty
            SetDirty(m_material, false, false);
        }

        /// <summary>
        /// Update profile settings from terrain and refresh shader
        /// </summary>
        public void UpdateProfileFromTerrainForced()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.LogError("CTS is missing terrain, cannot update.");
                    return;
                }
            }

            m_profile.UpdateSettingsFromTerrain(m_terrain, true);
            UpdateMaterialAndShader();
        }

        /// <summary>
        /// Check to see if the profile needs a texture update
        /// </summary>
        /// <returns>True if it does, false otherwise</returns>
        private bool ProfileNeedsTextureUpdate()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }
            if (m_terrain == null)
            {
                Debug.LogWarning("No terrain , unable to check if needs texture update");
                return false;
            }

            if (m_profile == null)
            {
                Debug.LogWarning("No profile, unable to check if needs texture update");
                return false;
            }

            if (m_profile.TerrainTextures.Count == 0)
            {
                return false;
            }

            //Now check the terrain splats against the profile splats
            SplatPrototype[] splats = m_terrain.terrainData.splatPrototypes;
            if (m_profile.TerrainTextures.Count != splats.Length)
            {
                return true;
            }

            //Check each individual texture
            SplatPrototype splatProto;
            CTSTerrainTextureDetails terrainDetail;
            for (int idx = 0; idx < splats.Length; idx++)
            {
                terrainDetail = m_profile.TerrainTextures[idx];
                splatProto = splats[idx];
                if (terrainDetail.Albedo == null)
                {
                    if (splatProto.texture != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (splatProto.texture == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (terrainDetail.Albedo.GetInstanceID() != splatProto.texture.GetInstanceID())
                        {
                            return true;
                        }
                    }
                }

                if (terrainDetail.Normal == null)
                {
                    if (splatProto.normalMap != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (splatProto.normalMap == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (terrainDetail.Normal.GetInstanceID() != splatProto.normalMap.GetInstanceID())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see if the terrain needs a texture update
        /// </summary>
        /// <returns></returns>
        private bool TerrainNeedsTextureUpdate()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }
            if (m_terrain == null)
            {
                Debug.LogWarning("No terrain , unable to check if needs texture update");
                return false;
            }

            if (m_profile == null)
            {
                Debug.LogWarning("No profile, unable to check if needs texture update");
                return false;
            }

            if (m_profile.TerrainTextures.Count == 0)
            {
                return false;
            }

            //Now check the terrain splats against the profile splats
            SplatPrototype[] splats = m_terrain.terrainData.splatPrototypes;
            if (m_profile.TerrainTextures.Count != splats.Length)
            {
                return true;
            }

            //Check each individual texture
            SplatPrototype splatProto;
            CTSTerrainTextureDetails terrainDetail;
            for (int idx = 0; idx < splats.Length; idx++)
            {
                terrainDetail = m_profile.TerrainTextures[idx];
                splatProto = splats[idx];
                if (terrainDetail.Albedo == null)
                {
                    if (splatProto.texture != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (splatProto.texture == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (terrainDetail.Albedo.GetInstanceID() != splatProto.texture.GetInstanceID())
                        {
                            return true;
                        }
                        if (terrainDetail.m_albedoTilingClose != splatProto.tileSize.x)
                        {
                            return true;
                        }
                    }
                }

                if (terrainDetail.Normal == null)
                {
                    if (splatProto.normalMap != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (splatProto.normalMap == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (terrainDetail.Normal.GetInstanceID() != splatProto.normalMap.GetInstanceID())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Replace the existing texture in the terrain with a new one
        /// </summary>
        /// <param name="texture">New texture</param>
        /// <param name="textureIdx">Index of texture</param>
        /// <param name="tiling">Tiling</param>
        public void ReplaceAlbedoInTerrain(Texture2D texture, int textureIdx, float tiling)
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }

            if (m_terrain != null)
            {
                SplatPrototype[] splats = m_terrain.terrainData.splatPrototypes;
                if (textureIdx >= 0 && textureIdx < splats.Length)
                {
                    splats[textureIdx].texture = texture;
                    splats[textureIdx].tileSize = new Vector2(tiling, tiling);
                    m_terrain.terrainData.splatPrototypes = splats;
                    m_terrain.Flush();
                    SetDirty(m_terrain, false, false);
                }
                else
                {
                    Debug.LogWarning("Invalid texture index in replace albedo");
                }
            }
        }

        /// <summary>
        /// Replace the existing normal texture in the terrain with a new one
        /// </summary>
        /// <param name="texture">New texture</param>
        /// <param name="textureIdx">Index of texture</param>
        /// <param name="tiling">Tiling</param>
        public void ReplaceNormalInTerrain(Texture2D texture, int textureIdx, float tiling)
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }

            if (m_terrain != null)
            {
                SplatPrototype[] splats = m_terrain.terrainData.splatPrototypes;
                if (textureIdx >= 0 && textureIdx < splats.Length)
                {
                    splats[textureIdx].normalMap = texture;
                    splats[textureIdx].tileSize = new Vector2(tiling, tiling);
                    m_terrain.terrainData.splatPrototypes = splats;
                    m_terrain.Flush();
                    SetDirty(m_terrain, false, false);
                }
                else
                {
                    Debug.LogWarning("Invalid texture index in replace normal!");
                }
            }
        }

        /// <summary>
        /// Construct normal from terrain
        /// </summary>
        public void BakeTerrainNormals()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }
            if (m_terrain == null)
            {
                Debug.LogWarning("Could not make terrain normal, as terrain object not set.");
                return;
            }

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Generating terrain normals : " + m_terrain.name, 0f);
            #endif

            Color nrmColor;
            Vector3 normal;
            int width = m_terrain.terrainData.heightmapWidth * 2;
            int height = m_terrain.terrainData.heightmapHeight * 2;
            #if UNITY_EDITOR
            float dimensions = width * height;
            #endif
            Texture2D nrmTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            nrmTex.name = m_terrain.name + " Nrm";
            nrmTex.wrapMode = TextureWrapMode.Clamp;
            nrmTex.filterMode = FilterMode.Bilinear;
            nrmTex.anisoLevel = 8;

            for (int x = 0; x < width; x++)
            {
                #if UNITY_EDITOR
                if (x % 250 == 0)
                {
                    EditorUtility.DisplayProgressBar("Baking Textures", "Ingesting terrain normals : " + m_terrain.name + "..", (float)(x * width) / dimensions);
                }
                #endif

                for (int z = 0; z < height; z++)
                {
                    normal = m_terrain.terrainData.GetInterpolatedNormal((float)x / (float)width, (float)z / (float)height);
                    nrmColor = new Color();
                    nrmColor.r = 0f;
                    nrmColor.g = (normal.y * 2f) - 1f; //0..1
                    nrmColor.b = 0f;
                    nrmColor.a = (normal.x * 2f) - 1f; //0..1  
                    nrmTex.SetPixel(x, z, nrmColor);
                }
            }
            nrmTex.Apply();

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Encoding terrain normals : " + m_terrain.name + "..", 0f);

            //Save it
            string normalsPath = AssetDatabase.GetAssetPath(m_material);
            if (string.IsNullOrEmpty(normalsPath))
            {
                Directory.CreateDirectory(m_ctsDirectory + "Terrains/");
                normalsPath = string.Format("{0}Terrains/{1}_Normals.png", m_ctsDirectory, m_material.name);
                normalsPath = normalsPath.Replace(".mat", "");
            }
            else
            {
                normalsPath = normalsPath.Replace(".mat", "_Normals.png");
            }

            byte[] content = nrmTex.EncodeToPNG();
            File.WriteAllBytes(normalsPath, content);
            AssetDatabase.Refresh();

            //Import it back in as a normal map
            var importer = AssetImporter.GetAtPath(normalsPath) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                importer.textureType = TextureImporterType.NormalMap;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.convertToNormalmap = true;
                importer.heightmapScale = 0.1f;
                importer.anisoLevel = 8;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                importer.normalmapFilter = TextureImporterNormalFilter.Standard;
                importer.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.ImportAsset(normalsPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }

            //Load & assign it
            nrmTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalsPath);
            EditorUtility.ClearProgressBar();
            #endif

            NormalMap = nrmTex;
        }

        /// <summary>
        /// Construct base map from terrain
        /// </summary>
        public void BakeTerrainBaseMap()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }
            if (m_terrain == null)
            {
                Debug.LogWarning("Could not make terrain base map, as terrain object not set.");
                return;
            }

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Generating terrain basemap : " + m_terrain.name, 0f);
            #endif

            int width = 2048;
            int height = 2048;
            Texture2D terrainSplat;
            Texture2D[] terrainSplats = m_terrain.terrainData.alphamapTextures;
            SplatPrototype[] terrainSplatPrototypes = m_terrain.terrainData.splatPrototypes;
            if (terrainSplats.Length > 0)
            {
                width = terrainSplats[0].width;
                height = terrainSplats[0].height;
            }
            float dimensions = width * height;

            //Get the average colours of the terrain textures by using the highest mip
            Color splatColor;
            Color[] averageSplatColors = new Color[terrainSplatPrototypes.Length];
            SplatPrototype proto;
            for (int protoIdx = 0; protoIdx < terrainSplatPrototypes.Length; protoIdx++)
            {
                proto = terrainSplatPrototypes[protoIdx];
                Texture2D tmpTerrainTex = CTSProfile.ResizeTexture(proto.texture, TextureFormat.ARGB32, 8, width, height, true, false, false);
                Color[] maxMipColors = tmpTerrainTex.GetPixels(tmpTerrainTex.mipmapCount - 1);
                averageSplatColors[protoIdx] = new Color(maxMipColors[0].r, maxMipColors[0].g, maxMipColors[0].b, maxMipColors[0].a);
            }

//            //Resize / get the alpha map
//            Texture2D alphamap = null;
//            if (m_cutoutMask != null)
//            {
//                alphamap = CTSProfile.ResizeTexture(m_cutoutMask, TextureFormat.ARGB32, 8, width, height, true, false, false);
//            }

            //Create the new texture
            Texture2D colorTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            colorTex.name = m_terrain.name + "_BaseMap";
            colorTex.wrapMode = TextureWrapMode.Repeat;
            colorTex.filterMode = FilterMode.Bilinear;
            colorTex.anisoLevel = 8;

            for (int x = 0; x < width; x++)
            {
                #if UNITY_EDITOR
                if (x % 250 == 0)
                {
                    EditorUtility.DisplayProgressBar("Baking Textures", "Ingesting terrain basemap : " + m_terrain.name + "..", (float)(x * width) / dimensions);
                }
                #endif

                for (int z = 0; z < height; z++)
                {
                    int splatColorIdx = 0;
                    Color mapColor = Color.black;
                    for (int splatIdx = 0; splatIdx < terrainSplats.Length; splatIdx++)
                    {
                        terrainSplat = terrainSplats[splatIdx];
                        splatColor = terrainSplat.GetPixel(x, z);
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.r);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.g);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.b);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.a);
                        }
                        mapColor.a = 0f;
                    }
//                    if (alphamap != null)
//                    {
//                        mapColor.a = alphamap.GetPixel(x, z).grayscale;
//                    }
                    colorTex.SetPixel(x, z, mapColor);
                }
            }
            colorTex.Apply();

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Encoding terrain basemap : " + m_terrain.name + "..", 0f);

            //Save it
            string cmapPath = AssetDatabase.GetAssetPath(m_material);
            if (string.IsNullOrEmpty(cmapPath))
            {
                cmapPath = string.Format("{0}Terrains/{1}_BaseMap.png", m_ctsDirectory, m_material.name);
                cmapPath = cmapPath.Replace(".mat", "");
            }
            else
            {
                cmapPath = cmapPath.Replace(".mat", "_BaseMap.png");
            }

            Directory.CreateDirectory(m_ctsDirectory + "Terrains/");
            byte[] content = colorTex.EncodeToPNG();
            File.WriteAllBytes(cmapPath, content);
            AssetDatabase.Refresh();

            //Load & assign it
            colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(cmapPath);
            EditorUtility.ClearProgressBar();
            #endif

            ColorMap = colorTex;
        }

        /// <summary>
        /// Construct base map from terrain and grass
        /// </summary>
        public void BakeTerrainBaseMapWithGrass()
        {
            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
            }
            if (m_terrain == null)
            {
                Debug.LogWarning("Could not make terrain base map, as terrain object not set.");
                return;
            }

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Generating terrain basemap : " + m_terrain.name, 0f);
            #endif

            int width = 2048;
            int height = 2048;
            Texture2D terrainSplat;
            Texture2D[] terrainSplats = m_terrain.terrainData.alphamapTextures;
            SplatPrototype[] terrainSplatPrototypes = m_terrain.terrainData.splatPrototypes;
            if (terrainSplats.Length > 0)
            {
                width = terrainSplats[0].width;
                height = terrainSplats[0].height;
            }
            float dimensions = width * height;

            //Get the average colours of the terrain textures by using the highest mip
            Color splatColor;
            Color[] averageSplatColors = new Color[terrainSplatPrototypes.Length];
            SplatPrototype proto;
            for (int protoIdx = 0; protoIdx < terrainSplatPrototypes.Length; protoIdx++)
            {
                proto = terrainSplatPrototypes[protoIdx];
                Texture2D tmpTerrainTex = CTSProfile.ResizeTexture(proto.texture, TextureFormat.ARGB32, 8, width, height, true, false, false);
                Color[] maxMipColors = tmpTerrainTex.GetPixels(tmpTerrainTex.mipmapCount - 1);
                averageSplatColors[protoIdx] = new Color(maxMipColors[0].r, maxMipColors[0].g, maxMipColors[0].b, maxMipColors[0].a);
            }

            //Get the average colours of the terrain grasses by using the highest mip
            List<Color> averageGrassColors = new List<Color>();
            DetailPrototype grassProto;
            DetailPrototype[] grassPrototypes = m_terrain.terrainData.detailPrototypes;
            List<CTSHeightMap> grassArrays = new List<CTSHeightMap>();
            int grassArrayWidth = m_terrain.terrainData.detailWidth;
            int grassArrayHeight = m_terrain.terrainData.detailHeight;

            for (int protoIdx = 0; protoIdx < grassPrototypes.Length; protoIdx++)
            {
                grassProto = grassPrototypes[protoIdx];
                if (grassProto.usePrototypeMesh == false && grassProto.prototypeTexture != null)
                {
                    //Get the detail array
                    grassArrays.Add(new CTSHeightMap(m_terrain.terrainData.GetDetailLayer(0, 0, grassArrayWidth, grassArrayHeight, protoIdx)));

                    //Resize it to get around read restrictions
                    Texture2D tmpGrassTex = CTSProfile.ResizeTexture(grassProto.prototypeTexture, TextureFormat.ARGB32, 8, grassArrayWidth, grassArrayHeight, true, false, false);

                    //Get the mip colour
                    Color[] maxMipColors = tmpGrassTex.GetPixels(tmpGrassTex.mipmapCount - 1);
                    Color grassColor = new Color(maxMipColors[0].r, maxMipColors[0].g, maxMipColors[0].b, 1f);

                    //Need to consider the detail colour as well - make an average based on noise spread
                    Color grassTint = Color.Lerp(grassProto.healthyColor, grassProto.dryColor, 0.2f);

                    //And now apply it to grass color
                    grassColor = Color.Lerp(grassColor, grassTint, 0.3f);

                    //And store
                    averageGrassColors.Add(grassColor);
                }
            }

            //Resize / get the alpha map
//            Texture2D alphamap = null;
//            if (m_cutoutMask != null)
//            {
//                alphamap = CTSProfile.ResizeTexture(m_cutoutMask, TextureFormat.ARGB32, 8, width, height, true, false, false);
//            }

            //Create the new texture
            Texture2D colorTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            colorTex.name = m_terrain.name + "_BaseMap";
            colorTex.wrapMode = TextureWrapMode.Repeat;
            colorTex.filterMode = FilterMode.Bilinear;
            colorTex.anisoLevel = 8;

            for (int x = 0; x < width; x++)
            {
                #if UNITY_EDITOR
                if (x % 250 == 0)
                {
                    EditorUtility.DisplayProgressBar("Baking Textures", "Ingesting terrain basemap : " + m_terrain.name + "..", (float)(x * width) / dimensions);
                }
                #endif

                for (int z = 0; z < height; z++)
                {
                    int splatColorIdx = 0;
                    Color mapColor = Color.black;

                    //Make basemap
                    for (int splatIdx = 0; splatIdx < terrainSplats.Length; splatIdx++)
                    {
                        terrainSplat = terrainSplats[splatIdx];
                        splatColor = terrainSplat.GetPixel(x, z);
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.r);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.g);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.b);
                        }
                        if (splatColorIdx < averageSplatColors.Length)
                        {
                            mapColor = Color.Lerp(mapColor, averageSplatColors[splatColorIdx++], splatColor.a);
                        }
                    }

                    //Now add in the grass
                    for (int grassIdx = 0; grassIdx < averageGrassColors.Count; grassIdx++)
                    {
                        CTSHeightMap grassHm = grassArrays[grassIdx];
                        float grassStrength = grassHm[(float) z/(float) height, (float) x/(float) width] * m_bakeGrassMixStrength;
                        mapColor = Color.Lerp(mapColor, Color.Lerp(averageGrassColors[grassIdx], Color.black, m_bakeGrassDarkenAmount), grassStrength);
                    }

                    //Set alpha
                    mapColor.a = 0f;
//                    if (alphamap != null)
//                    {
//                        mapColor.a = alphamap.GetPixel(x, z).grayscale;
//                    }

                    //And keep it
                    colorTex.SetPixel(x, z, mapColor);
                }
            }

            colorTex.Apply();

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Baking Textures", "Encoding terrain basemap : " + m_terrain.name + "..", 0f);

            //Save it
            string cmapPath = AssetDatabase.GetAssetPath(m_material);
            if (string.IsNullOrEmpty(cmapPath))
            {
                cmapPath = string.Format("{0}Terrains/{1}_BaseMap.png", m_ctsDirectory, m_material.name);
                cmapPath = cmapPath.Replace(".mat", "");
            }
            else
            {
                cmapPath = cmapPath.Replace(".mat", "_BaseMap.png");
            }

            Directory.CreateDirectory(m_ctsDirectory + "Terrains/");
            byte[] content = colorTex.EncodeToPNG();
            File.WriteAllBytes(cmapPath, content);
            AssetDatabase.Refresh();

            //Load & assign it
            colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(cmapPath);
            EditorUtility.ClearProgressBar();
            #endif

            ColorMap = colorTex;
        }

        /// <summary>
        /// We are going to replace the terrain with a new in memory one, and rip out all the splats in the new one
        /// </summary>
        private void UpdateTerrainSplatsAtRuntime()
        {
            //Make sure we only do this when application is playing
            if (!Application.isPlaying)
            {
                //Debug.LogWarning("CTS could not replace terrain splats, as this is a runtime only function.");
                return;
            }

            //Make sure we have terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    //Debug.LogWarning("CTS could not replace terrain splats, as terrain object not set.");
                    return;
                }
            }

            //Make sure we have profile and have optimisation selected
            if (m_profile == null)
            {
                //Debug.LogWarning("CTS could not replace terrain splats, as profile not set.");
                return;
            }

            //Make back up the splat maps - but only if we have none already - this should always be true first time thru
            if (m_splatBackupArray == null || m_splatBackupArray.GetLength(0) == 0)
            {
                m_splatBackupArray = m_terrain.terrainData.GetAlphamaps(0, 0, m_terrain.terrainData.alphamapWidth, m_terrain.terrainData.alphamapHeight);
            }

            //Only do this if we are not unity shader
            if (m_terrain.materialType != Terrain.MaterialType.Custom)
            {
                //Debug.LogWarning("CTS will not remove textures if shaded by unity terrain shader, terrain not changed.");
                return;
            }

            //Go no further if not selected for optimisation
            if (!m_profile.m_globalOptimiseAtRuntime)
            {
                //Debug.LogWarning("CTS will not remove textures if optimisation not selected, terrain not changed.");
                return;
            }

            //Then replicate the terrain data minus the splats
            TerrainData terrainData = new TerrainData();
            terrainData.name = m_terrain.terrainData.name + "_copy";
            terrainData.thickness = m_terrain.terrainData.thickness;
            terrainData.alphamapResolution = m_terrain.terrainData.alphamapResolution;
            terrainData.baseMapResolution = m_terrain.terrainData.baseMapResolution;

            //Detail related
            terrainData.SetDetailResolution(m_terrain.terrainData.detailResolution, 8);
            terrainData.detailPrototypes = m_terrain.terrainData.detailPrototypes;
            for (int dtlIdx = 0; dtlIdx < terrainData.detailPrototypes.Length; dtlIdx++)
            {
                terrainData.SetDetailLayer(0, 0, dtlIdx, m_terrain.terrainData.GetDetailLayer(0, 0, terrainData.detailResolution, terrainData.detailResolution, dtlIdx));
            }
            terrainData.wavingGrassAmount = m_terrain.terrainData.wavingGrassAmount;
            terrainData.wavingGrassSpeed = m_terrain.terrainData.wavingGrassSpeed;
            terrainData.wavingGrassStrength = m_terrain.terrainData.wavingGrassStrength;
            terrainData.wavingGrassTint = m_terrain.terrainData.wavingGrassTint;

            //Tree related
            terrainData.treePrototypes = m_terrain.terrainData.treePrototypes;
            terrainData.treeInstances = m_terrain.terrainData.treeInstances;

            //Height related
            terrainData.heightmapResolution = m_terrain.terrainData.heightmapResolution;
            terrainData.SetHeights(0, 0, m_terrain.terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution));

            //Size
            terrainData.size = m_terrain.terrainData.size;

            //Assign to terrain
            m_terrain.terrainData = terrainData;
            m_terrain.Flush();

            //Update the collider
            TerrainCollider collider = m_terrain.gameObject.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrainData;
            }

            //Remove old stuff from memory
            System.GC.Collect();

            //Then create the new terrain
            /*
            Terrain terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
            terrain.gameObject.transform.position = m_terrain.gameObject.transform.position;
            terrain.name = m_terrain.name + "_copy";
            terrain.bakeLightProbesForTrees = m_terrain.bakeLightProbesForTrees;
            terrain.basemapDistance = m_terrain.basemapDistance;
            terrain.castShadows = m_terrain.castShadows;
            terrain.collectDetailPatches = m_terrain.collectDetailPatches;
            terrain.detailObjectDensity = m_terrain.detailObjectDensity;
            terrain.detailObjectDistance = m_terrain.detailObjectDistance;
            terrain.drawHeightmap = m_terrain.drawHeightmap;
            terrain.drawTreesAndFoliage = m_terrain.drawTreesAndFoliage;
            terrain.editorRenderFlags = m_terrain.editorRenderFlags;
            terrain.heightmapMaximumLOD = m_terrain.heightmapMaximumLOD;
            terrain.heightmapPixelError = m_terrain.heightmapPixelError;
            terrain.legacyShininess = m_terrain.legacyShininess;
            terrain.legacySpecular = m_terrain.legacySpecular;
            terrain.lightmapIndex = m_terrain.lightmapIndex;
            terrain.lightmapScaleOffset = m_terrain.lightmapScaleOffset;
            terrain.materialType = m_terrain.materialType;
            terrain.materialTemplate = m_terrain.materialTemplate;
            terrain.realtimeLightmapIndex = m_terrain.realtimeLightmapIndex;
            terrain.realtimeLightmapScaleOffset = m_terrain.realtimeLightmapScaleOffset;
            terrain.reflectionProbeUsage = m_terrain.reflectionProbeUsage;
            terrain.treeBillboardDistance = m_terrain.treeBillboardDistance;
            terrain.treeCrossFadeLength = m_terrain.treeCrossFadeLength;
            terrain.treeDistance = m_terrain.treeDistance;
            terrain.treeLODBiasMultiplier = m_terrain.treeLODBiasMultiplier;
            terrain.treeMaximumFullLODCount = m_terrain.treeMaximumFullLODCount;
            terrain.Flush();
            */
        }

        /// <summary>
        /// Replace the terrain textures dependent on the thing being done
        /// </summary>
        private void ReplaceTerrainTexturesFromProfile()
        {
            //Exit if no terrain
            if (m_terrain == null)
            {
                m_terrain = transform.GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    return;
                }
            }

            //Exit if no profile
            if (m_profile == null)
            {
                Debug.LogWarning("No profile, unable to replace terrain textures");
                return;
            }

            //Exit if no textures
            if (m_profile.TerrainTextures.Count == 0)
            {
                Debug.LogWarning("No profile textures, unable to replace terrain textures");
                return;
            }

            //Create new splats
            SplatPrototype[] splats = new SplatPrototype[m_profile.TerrainTextures.Count];
            for (int idx = 0; idx < splats.Length; idx++)
            {
                splats[idx] = new SplatPrototype();
                splats[idx].texture = m_profile.TerrainTextures[idx].Albedo;
                splats[idx].normalMap = m_profile.TerrainTextures[idx].Normal;
                splats[idx].tileSize = new Vector2(m_profile.TerrainTextures[idx].m_albedoTilingClose, m_profile.TerrainTextures[idx].m_albedoTilingClose);
            }

            //Push splats back to the terrain
            m_terrain.terrainData.splatPrototypes = splats;
            m_terrain.Flush();

            //Mark terrain as dirty
            SetDirty(m_terrain, false, false);
        }

        /// <summary>
        /// Return the CTS directory in the project
        /// </summary>
        /// <returns>If in editor it returns the full cts directory, if in build, returns assets directory.</returns>
        public static string GetCTSDirectory()
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets("CTS_ReadMe", null);
            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[idx]);
                if (Path.GetFileName(path) == "CTS_ReadMe.txt")
                {
                    return Path.GetDirectoryName(path) + "/";
                }
            }
#endif
            return "Assets/CTS/";
        }

        /// <summary>
        /// Marks the object and its scene if possible as dirty. NOTE: This will only work
        /// in the editor. It compiles out of builds.
        /// </summary>
        /// <param name="obj">Object to mark</param>
        /// <param name="recordUndo">Tell Unity we also want and undo option</param>
        /// <param name="isPlayingAllowed">Allow dirty when application is playing as well</param>
        public static void SetDirty(UnityEngine.Object obj, bool recordUndo, bool isPlayingAllowed)
        {
#if UNITY_EDITOR

            //Check to see if we are playing 
            if (!isPlayingAllowed && Application.isPlaying)
            {
                return;
            }

            //Check to see if we are really an object
            if (obj == null)
            {
                Debug.LogWarning("Attempting to set null object dirty");
                return;
            }

            //Undo
            if (recordUndo)
            {
                Undo.RecordObject(obj, "Made changes");
            }

            //Calling this everywhere - because unity doco so obscure
            EditorUtility.SetDirty(obj);

            //Mark the scene as dirty as well for non persisted objects
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
            if (!Application.isPlaying && !EditorUtility.IsPersistent(obj))
                {
                    MonoBehaviour mb = obj as MonoBehaviour;
                    if (mb != null)
                    {
                        EditorSceneManager.MarkSceneDirty(mb.gameObject.scene);
                        return;
                    }
                    GameObject go = obj as GameObject;
                    if (go != null)
                    {
                        EditorSceneManager.MarkSceneDirty(go.scene);
                        return;
                    }
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
#endif
#endif
        }

        /// <summary>
        /// Remove the seams from active terrain tiles
        /// </summary>
        public void RemoveWorldSeams()
        {
            Terrain[] terrains = Terrain.activeTerrains;

            //Check to see if we have terrains
            if (terrains.Length == 0)
            {
                return;
            }

            //Get the terrain bounds
            int terrainX;
            int terrainZ;
            float terrainWidth = terrains[0].terrainData.size.x;
            float terrainHeight = terrains[0].terrainData.size.z;
            float minBoundsX = float.MaxValue;
            float maxBoundsX = float.MinValue;
            float minBoundsZ = float.MaxValue;
            float maxBoundsZ = int.MinValue;
            foreach (var terrain in terrains)
            {
                Vector3 position = terrain.GetPosition();

                if (position.x < minBoundsX)
                {
                    minBoundsX = position.x;
                }
                if (position.z < minBoundsZ)
                {
                    minBoundsZ = position.z;
                }
                if (position.x > maxBoundsX)
                {
                    maxBoundsX = position.x;
                }
                if (position.z > maxBoundsZ)
                {
                    maxBoundsZ = position.z;
                }
            }

            //Put the terrains in right places
            int tilesX = (int)((maxBoundsX - minBoundsX) / terrainWidth) + 1;
            int tilesZ = (int)((maxBoundsZ - minBoundsZ) / terrainHeight) + 1;
            Terrain[,] terrainTiles = new Terrain[tilesX, tilesZ];
            foreach (var terrain in terrains)
            {
                Vector3 position = terrain.GetPosition();
                terrainX = tilesX - (int)((maxBoundsX - position.x) / terrainWidth) - 1;
                terrainZ = tilesZ - (int)((maxBoundsZ - position.z) / terrainHeight) - 1;
                //Debug.Log(string.Format("{0},{1} - {2},{3}", position.x, position.z, terrainX, terrainZ));
                terrainTiles[terrainX, terrainZ] = terrain;
            }

            //Now assign neightbors
            for (int tx = 0; tx < tilesX; tx++)
            {
                for (int tz = 0; tz < tilesZ; tz++)
                {
                    Terrain right = null;
                    Terrain left = null;
                    Terrain bottom = null;
                    Terrain top = null;

                    if (tx > 0) left = terrainTiles[(tx - 1), tz];
                    if (tx < tilesX - 1) right = terrainTiles[(tx + 1), tz];
                    if (tz > 0) bottom = terrainTiles[tx, (tz - 1)];
                    if (tz < tilesZ - 1) top = terrainTiles[tx, (tz + 1)];

                    terrainTiles[tx, tz].SetNeighbors(left, top, right, bottom);
                }
            }
        }
    }
}