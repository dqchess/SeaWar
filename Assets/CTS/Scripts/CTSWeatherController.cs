using UnityEngine;

namespace CTS
{
    /// <summary>
    /// Per terrain weather controller for CTS. This applies weather updates into the terrain. To control weather 
    /// globally use the Weather Manager class instead.
    /// </summary>
    public class CTSWeatherController : MonoBehaviour
    {
        /// <summary>
        /// The terrain we are managing
        /// </summary>
        private Terrain m_terrain;

        private static bool m_IDsInitialized = false;
        private static int _Snow_Amount;
        private static int _Snow_Min_Height;
        private static int _Snow_Smoothness;
        private static int [] _Texture_X_Color = new int[16];


        /// <summary>
        /// Initialize the shader ID's for faster shader updates
        /// </summary>
        private void InitializeIDs()
        {
            m_IDsInitialized = true;
            _Snow_Amount = Shader.PropertyToID("_Snow_Amount");
            _Snow_Min_Height = Shader.PropertyToID("_Snow_Min_Height");
            _Snow_Smoothness = Shader.PropertyToID("_Snow_Smoothness");
            _Texture_X_Color = new int[16];
            for (int i = 1; i <= 16; i++)
            {
                _Texture_X_Color[i - 1] = Shader.PropertyToID(string.Format("_Texture_{0}_Color", i));
            }
        }

        /// <summary>
        /// Process a weather update
        /// </summary>
        /// <param name="manager">The manager providing the update</param>
        public void ProcessWeatherUpdate(CTSWeatherManager manager)
        {
            //Make sure we have a terrain
            if (m_terrain == null)
            {
                m_terrain = GetComponent<Terrain>();
                if (m_terrain == null)
                {
                    Debug.Log("CTS Weather Controller must be connected to a terrain to work.");
                    return;
                }
            }

            //Make sure we have a custom controller
            if (m_terrain.materialType != Terrain.MaterialType.Custom)
            {
                Debug.Log("CTS Weather Controller needs a CTS Material to work with.");
                return;
            }

            //Do the update
            Material material = m_terrain.materialTemplate;
            if (material == null)
            {
                Debug.Log("CTS Weather Controller needs a Custom Material to work with.");
                return;
            }

            //Check initialisation of ID's
            if (!m_IDsInitialized)
            {
                InitializeIDs();
            }

            //Now update it
            material.SetFloat(_Snow_Amount, manager.SnowPower*2f);
            material.SetFloat(_Snow_Min_Height, manager.SnowMinHeight);

            float shinyness = manager.RainPower*manager.MaxRainSmoothness;
            material.SetFloat(_Snow_Smoothness, shinyness);

            Color tint = Color.white;
            if (manager.Season < 1f)
            {
                tint = Color.Lerp(manager.WinterTint, manager.SpringTint, manager.Season);
            }
            else if (manager.Season < 2f)
            {
                tint = Color.Lerp(manager.SpringTint, manager.SummerTint, manager.Season - 1f);
            }
            else if (manager.Season < 3f)
            {
                tint = Color.Lerp(manager.SummerTint, manager.AutumnTint, manager.Season - 2f);
            }
            else
            {
                tint = Color.Lerp(manager.AutumnTint, manager.WinterTint, manager.Season - 3f);
            }
            for (int idx = 0; idx < 16; idx++)
            {
                material.SetVector(_Texture_X_Color[idx], new Vector4(tint.r, tint.g, tint.b, shinyness));
            }
        }
    }
}