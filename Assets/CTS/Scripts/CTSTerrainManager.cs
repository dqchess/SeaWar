using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CTS
{
    /// <summary>
    /// Manages communication between weather, terrain profiles and terrain instances. General 
    /// CTS configuration and control should be performed via this class. Local weather control
    /// should be controlled via Weather Manager class.
    /// </summary>
    public class CTSTerrainManager : CTSSingleton<CTSTerrainManager>
    {
        /// <summary>
        /// The shaders in the scene
        /// </summary>
        private List<CompleteTerrainShader> m_shaderList = new List<CompleteTerrainShader>();

        /// <summary>
        /// The last time the shader list was updated
        /// </summary>
        private DateTime m_lastShaderListUpdate = DateTime.MinValue;

        /// <summary>
        /// The controllers in the scene
        /// </summary>
        private List<CTSWeatherController> m_controllerList = new List<CTSWeatherController>();

        /// <summary>
        /// The last time the controller list was updated
        /// </summary>
        private DateTime m_lastControllerListUpdate = DateTime.MinValue;

        /// <summary>
        /// Make sure its only ever a singleton by stopping direct instantiation
        /// </summary>
        protected CTSTerrainManager()
        {
        }

        /// <summary>
        /// Register a specific shader
        /// </summary>
        /// <param name="shader">Shader to register</param>
        public void RegisterShader(CompleteTerrainShader shader)
        {
            if (!m_shaderList.Contains(shader))
            {
                m_shaderList.Add(shader);
                m_lastShaderListUpdate = DateTime.Now;
            }
        }

        /// <summary>
        /// Grab all the shaders in the scene
        /// </summary>
        /// <param name="force">Force an update always</param>
        public void RegisterAllShaders(bool force = false)
        {
            if (Application.isPlaying)
            {
                if (force)
                {
                    m_shaderList.Clear();
                    m_shaderList.AddRange(GameObject.FindObjectsOfType<CompleteTerrainShader>());
                    m_lastShaderListUpdate = DateTime.Now;
                }
                else
                {
                    if (m_shaderList.Count == 0 || (DateTime.Now - m_lastShaderListUpdate).TotalSeconds > 30)
                    {
                        m_shaderList.AddRange(GameObject.FindObjectsOfType<CompleteTerrainShader>());
                        m_lastShaderListUpdate = DateTime.Now;
                    }
                }
            }
            else
            {
                if (force || m_shaderList.Count == 0 || (DateTime.Now - m_lastShaderListUpdate).TotalSeconds > 30)
                {
                    m_shaderList.Clear();
                    m_shaderList.AddRange(GameObject.FindObjectsOfType<CompleteTerrainShader>());
                    m_lastShaderListUpdate = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Remove CTS from shader list so that it can be destroyed nicely
        /// </summary>
        /// <param name="shader">CTS shader to be removed</param>
        public void RemoveShader(CompleteTerrainShader shader)
        {
            if (shader != null && m_shaderList.Contains(shader))
            {
                m_shaderList.Remove(shader);
            }
        }

        /// <summary>
        /// Grab all the controllers in the scene
        /// </summary>
        /// <param name="force">Force an update always</param>
        public void RegisterAllControllers(bool force = false)
        {
            if (Application.isPlaying)
            {
                if (force)
                {
                    m_controllerList.Clear();
                    m_controllerList.AddRange(GameObject.FindObjectsOfType<CTSWeatherController>());
                    m_lastControllerListUpdate = DateTime.Now;
                }
            }
            else
            {
                if (force || m_controllerList.Count == 0 || (DateTime.Now - m_lastControllerListUpdate).TotalSeconds > 30)
                {
                    m_controllerList.Clear();
                    m_controllerList.AddRange(GameObject.FindObjectsOfType<CTSWeatherController>());
                    m_lastControllerListUpdate = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Add CTS to all terrains
        /// </summary>
        public void AddCTSToAllTerrains()
        {
            m_shaderList.Clear();
            foreach (var terrain in Terrain.activeTerrains)
            {
                CompleteTerrainShader shader = terrain.gameObject.GetComponent<CompleteTerrainShader>();
                if (shader == null)
                {
                    shader = terrain.gameObject.AddComponent<CompleteTerrainShader>();
                    CompleteTerrainShader.SetDirty(shader, false, false);
                    CompleteTerrainShader.SetDirty(terrain, false, false);
                }
                m_shaderList.Add(shader);
                m_lastShaderListUpdate = DateTime.Now;
            }
            #if UNITY_EDITOR
            //AssetDatabase.SaveAssets();
            if (!Application.isPlaying)
            {
                if (Terrain.activeTerrain != null)
                {
                    EditorGUIUtility.PingObject(Terrain.activeTerrain);
                }
            }
            #endif
        }

        /// <summary>
        /// Add CTS to a specific terrain
        /// </summary>
        /// <param name="terrain">Terrain to be added to</param>
        public void AddCTSToTerrain(Terrain terrain)
        {
            if (terrain == null)
            {
                return;
            }
            CompleteTerrainShader shader = terrain.gameObject.GetComponent<CompleteTerrainShader>();
            if (shader == null)
            {
                shader = terrain.gameObject.AddComponent<CompleteTerrainShader>();
                CompleteTerrainShader.SetDirty(shader, false, false);
                CompleteTerrainShader.SetDirty(terrain, false, false);
                m_shaderList.Add(shader);
            }
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (Terrain.activeTerrain != null)
                {
                    EditorGUIUtility.PingObject(Terrain.activeTerrain);
                }
            }
            #endif
        }

        /// <summary>
        /// Return true if the profile is actively assigned to a terrain
        /// </summary>
        /// <param name="profile">The profile being checked</param>
        /// <returns>True if its been assiged to a terrain</returns>
        public bool ProfileIsActive(CTSProfile profile)
        {
            //Return rubbish if we have no usage
            if (profile == null)
            {
                return false;
            }

            //Make sure shaders are registered
            RegisterAllShaders();

            CompleteTerrainShader shader = null;
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null && shader.Profile != null)
                {
                    if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Broadcast a message to select this profile on all the CTS terrains in the scene
        /// </summary>
        /// <param name="profile">Profile being selected</param>
        public void BroadcastProfileSelect(CTSProfile profile)
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //Broadcast the select
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                m_shaderList[idx].Profile = profile;
            }
            System.GC.Collect();
        }

        /// <summary>
        /// Broadcast a message to select this profile on all the specific terrain provided
        /// </summary>
        /// <param name="profile">Profile being selected</param>
        /// <param name="terrain">Terrain being selected</param>
        public void BroadcastProfileSelect(CTSProfile profile, Terrain terrain)
        {
            if (profile == null || terrain == null)
            {
                return;
            }
            CompleteTerrainShader shader = terrain.gameObject.GetComponent<CompleteTerrainShader>();
            if (shader != null)
            {
                shader.Profile = profile;
                shader.UpdateMaterialAndShader();
            }
        }

        /// <summary>
        /// Broadcast a profile update to all the shaders using it in the scene
        /// </summary>
        /// <param name="profile">Profile being updated</param>
        public void BroadcastProfileUpdate(CTSProfile profile)
        {
            //Make sure shaders are registered
            RegisterAllShaders();

            //Also make sure weather is registered
            RegisterAllControllers();

            //Can not do this on a null profile
            if (profile == null)
            {
                Debug.LogWarning("Cannot update shader on null profile.");
                return;
            }

            //Broadcast the update
            CompleteTerrainShader shader = null;
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null && shader.Profile != null)
                {
                    if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                    {
                        shader.UpdateMaterialAndShader();
                    }                    
                }
            }
        }

        /// <summary>
        /// Broadcast a shader setup on the selected profile in the scene, otherwise all
        /// </summary>
        /// <param name="profile">Profile being updated, otherwise all</param>
        public void BroadcastShaderSetup(CTSProfile profile)
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //First - check to see if we have one on the currently selected terrain - this will usually be where texture changes have been made
            CompleteTerrainShader shader = null;
            if (Terrain.activeTerrain != null)
            {
                shader = Terrain.activeTerrain.GetComponent<CompleteTerrainShader>();
                if (shader != null && shader.Profile != null)
                {
                    if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                    {
                        shader.UpdateProfileFromTerrainForced();
                        BroadcastProfileUpdate(profile);
                        return;
                    }
                }
            }

            //Otherwise broadcast the setup
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null && shader.Profile != null)
                {
                    if (profile == null)
                    {
                        shader.UpdateProfileFromTerrainForced();
                    }
                    else
                    {
                        //Find the first match and update it
                        if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                        {
                            shader.UpdateProfileFromTerrainForced();
                            BroadcastProfileUpdate(profile);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast bake terrains
        /// </summary>
        public void BroadcastBakeTerrains()
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //Broadcast the setup
            CompleteTerrainShader shader = null;
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null)
                {
                    if (shader.AutoBakeNormalMap)
                    {
                        shader.BakeTerrainNormals();
                    }
                    if (shader.AutoBakeColorMap)
                    {
                        if (!shader.AutoBakeGrassIntoColorMap)
                        {
                            shader.BakeTerrainBaseMap();
                        }
                        else
                        {
                            shader.BakeTerrainBaseMapWithGrass();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast an albedo texture switch
        /// </summary>
        /// <param name="profile">Selected profile - null means all CTS terrains</param>
        /// <param name="texture">New texture</param>
        /// <param name="textureIdx">Index</param>
        /// <param name="tiling">Tiling</param>
        /// <returns></returns>
        public void BroadcastAlbedoTextureSwitch(CTSProfile profile, Texture2D texture, int textureIdx, float tiling)
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //Do the texture switch
            CompleteTerrainShader shader = null;
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null && shader.Profile != null)
                {
                    if (profile == null)
                    {
                        shader.ReplaceAlbedoInTerrain(texture, textureIdx, tiling);
                    }
                    else
                    {
                        if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                        {
                            shader.ReplaceAlbedoInTerrain(texture, textureIdx, tiling);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast a normal texture switch
        /// </summary>
        /// <param name="profile">Selected profile - null means all CTS terrains</param>
        /// <param name="texture">New texture</param>
        /// <param name="textureIdx">Index</param>
        /// <param name="tiling">Tiling</param>
        public void BroadcastNormalTextureSwitch(CTSProfile profile, Texture2D texture, int textureIdx, float tiling)
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //Do the texture switch
            CompleteTerrainShader shader = null;
            for (int idx = 0; idx < m_shaderList.Count; idx++)
            {
                shader = m_shaderList[idx];
                if (shader != null && shader.Profile != null)
                {
                    if (profile == null)
                    {
                        shader.ReplaceNormalInTerrain(texture, textureIdx, tiling);
                    }
                    else
                    {
                        if (shader.Profile.GetInstanceID() == profile.GetInstanceID())
                        {
                            shader.ReplaceNormalInTerrain(texture, textureIdx, tiling);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast a weather update
        /// </summary>
        /// <param name="manager">The manager with the update</param>
        public void BroadcastWeatherUpdate(CTSWeatherManager manager)
        {
            //Periodically update this list
            RegisterAllControllers();

            //And then broadcast to it
            for (int idx = 0; idx < m_controllerList.Count; idx++)
            {
                m_controllerList[idx].ProcessWeatherUpdate(manager);
            }
        }

        /// <summary>
        /// This will remove world seams from loaded terrains - should only be called once for entire terrrain set
        /// </summary>
        public void RemoveWorldSeams()
        {
            //Make sure shaders are registered
            RegisterAllShaders(true);

            //Broadcast the seam
            if (m_shaderList.Count > 0)
            {
                m_shaderList[0].RemoveWorldSeams();
            }
        }
    }
}