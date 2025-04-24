using System;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tanks.Complete
{
    public class CameraControl : MonoBehaviour
    {
        public const string CORNER_TOP_RIGHT_COLOR_KEY = "_ColorPlayer1";
        public const string CORNER_BOTTOM_RIGHT_COLOR_KEY = "_ColorPlayer2";
        public const string CORNER_TOP_LEFT_COLOR_KEY = "_ColorPlayer3";
        public const string CORNER_BOTTOM_LEFT_COLOR_KEY = "_ColorPlayer4";
        
        public float m_DampTime = 0.2f;                 // Approximate time for the camera to refocus.
        public float m_ScreenEdgeBuffer = 4f;           // Space between the top/bottom most target and the screen edge.
        public float m_MinSize = 6.5f;                  // The smallest orthographic size the camera can be.
        public Transform[] m_Targets;                   // All the targets the camera needs to encompass.

        [SerializeField]
        private Camera m_Camera;      // Used for referencing the camera.
        [SerializeField]
        private CinemachineVirtualCamera m_VirtualCamera; // Used for referencing the virtual camera.achineVirtualCamera
        
        private float m_ZoomSpeed;                      // Reference speed for the smooth damping of the orthographic size.
        private Vector3 m_MoveVelocity;                 // Reference velocity for the smooth damping of the position.
        private Vector3 m_DesiredPosition;              // The position the camera is moving towards.

        private Vector3 m_AimToRig;                     // The offset to apply to the position so the child camera aim at the desired point 
        private Material m_Material;                   // The material used to display the screen corners colors
        private Vector2[] m_coins = new Vector2[4]; // The screen corners in pixel coordinates
        private Color[] m_colors = new Color[4]; // The colors to use for each corner

        #region UnityLifecycle
        private void Awake ()
        {
            // plane in which the camera rig is in
            Plane p = new Plane(Vector3.up, transform.position);
            Ray r = new Ray(m_Camera.transform.position, m_Camera.transform.forward);
            p.Raycast(r, out float d );

            // This is where the camera aim on the rig plane
            var aimTArget = r.GetPoint(d);

            // User can set the camera in random position and rotation as a child of this object, so it won't aim at the
            // center of this, meaning placing this object at the desired position won't make the camera aim at that desired position.
            // This offset correct that so the camera actually aim at the desired position
            m_AimToRig = transform.position - aimTArget;
            SetupForPostProcessControl();
        }
        
        private void FixedUpdate ()
        {
            // Move the camera towards a desired position.
            Move ();

            // Change the size of the camera based.
            Zoom ();
        }
        #endregion

        #region PostProcess

        private void SetupForPostProcessControl()
        {
            m_Material = GetURPMaterial();
            m_coins[0] = new Vector2(1920, 1080); // Coin supérieur droit
            m_coins[1]  = new Vector2(1920, 0); // Coin inférieur droit
            m_coins[2] = new Vector2(0, 1080);  // Coin supérieur gauche
            m_coins[3]  = new Vector2(0, 0); // Coin inférieur gauche
            m_colors[0] = m_Material.GetColor(CORNER_TOP_RIGHT_COLOR_KEY); // Coin supérieur droit
            m_colors[1] = m_Material.GetColor(CORNER_BOTTOM_RIGHT_COLOR_KEY); // Coin supérieur droit
            m_colors[2] = m_Material.GetColor(CORNER_TOP_LEFT_COLOR_KEY); // Coin supérieur droit
            m_colors[3] = m_Material.GetColor(CORNER_BOTTOM_LEFT_COLOR_KEY); // Coin supérieur droit
        }
        private Material GetURPMaterial()
        {
            if (!m_Camera.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
            {
                Debug.LogError("Caméra sans UniversalAdditionalCameraData.");
                return null;
            }
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("Pas d'URP pipeline asset actif.");
                return null;
            }

            // Récupère le champ m_RendererDataList (non public)
            var rendererListField = typeof(UniversalRenderPipelineAsset).GetField(
                "m_RendererDataList",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            var rendererDataList = rendererListField?.GetValue(urpAsset) as ScriptableRendererData[];
            if (rendererDataList == null || rendererDataList.Length == 0)
            {
                Debug.LogError("RendererDataList introuvable ou vide.");
                return null;
            }
            // Récupération de l'index du renderer via reflection
            FieldInfo rendererIndexField = typeof(UniversalAdditionalCameraData)
                .GetField("m_RendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);

            if (rendererIndexField == null)
            {
                Debug.LogError("Impossible de trouver le champ m_RendererIndex.");
                return null;
            }

            int rendererIndex = (int)rendererIndexField.GetValue(camData);
            if (rendererIndex < 0 || rendererIndex >= rendererDataList.Length)
            {
                Debug.LogWarning("Index de renderer invalide, on prend l'index 0.");
                rendererIndex = 0;
            }

            var rendererData = rendererDataList[rendererIndex] as UniversalRendererData;
            if (rendererData == null)
            {
                Debug.LogError("Le renderer n'est pas un ForwardRendererData.");
                return null;
            }

            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is FullScreenPassRendererFeature fullScreenFeature)
                {
                    var matField = typeof(FullScreenPassRendererFeature)
                        .GetField("m_Material", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    var mat = fullScreenFeature.passMaterial;
                    if (mat != null)
                    {
                        Debug.Log($"Matériel trouvé : {mat.name}");
                        return mat;
                    }
                    else
                    {
                        Debug.LogWarning("Material est null.");
                        return null;
                    }
                }
            }

            return null;
        }
        
        public void AssociateTankPointWithScreenCorner(Vector3[] a_points, Color[] a_colors)
        {
            Vector2[] points = new Vector2[a_points.Length];
            for (int i = 0; i < a_points.Length; i++)
            {
                points[i] = m_Camera.WorldToScreenPoint(a_points[i]);
            }
            
            
            Color[] newColors = TankCornerAssociator.GetOrderedColorArray(points, a_colors, m_coins);
            SetNewColors(newColors);
        }
        
        private void SetNewColors(Color[] a_colors)
        {
            // Set the new colors for the corners
            for (int i = 0; i < m_coins.Length; i++)
            {
                string colorKey = string.Empty;
                switch (i)
                {
                    case 0:
                        colorKey = CORNER_TOP_RIGHT_COLOR_KEY;
                        break;
                    case 1:
                        colorKey = CORNER_BOTTOM_RIGHT_COLOR_KEY;
                        break;
                    case 2:
                        colorKey = CORNER_TOP_LEFT_COLOR_KEY;
                        break;
                    case 3:
                        colorKey = CORNER_BOTTOM_LEFT_COLOR_KEY;
                        break;
                    default:
                        break;
                }
                if (a_colors[i] != m_colors[i] && colorKey != String.Empty)
                {
                    m_Material.DOColor(a_colors[i], colorKey, 0.5f);
                    m_colors[i] = a_colors[i];
                }
            }
            
        }

        #endregion

        #region Camera Movements
        private void Move ()
        {
            // Find the average position of the targets.
            FindAveragePosition ();

            
            // Smoothly transition to that position.
            transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition + m_AimToRig, ref m_MoveVelocity, m_DampTime);
        }
        
        private void FindAveragePosition ()
        {
            Vector3 averagePos = new Vector3 ();
            int numTargets = 0;

            // Go through all the targets and add their positions together.
            for (int i = 0; i < m_Targets.Length; i++)
            {
                // If the target isn't active, go on to the next one.
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;

                // Add to the average and increment the number of targets in the average.
                averagePos += m_Targets[i].position;
                numTargets++;
            }

            // If there are targets divide the sum of the positions by the number of them to find the average.
            if (numTargets > 0)
                averagePos /= numTargets;

            // Keep the same y value.
            averagePos.y = transform.position.y;
            
            m_DesiredPosition = averagePos;
        }
        
        private void Zoom ()
        {
            // Find the required size based on the desired position and smoothly transition to that size.
            float requiredSize = FindRequiredSize();
            //m_Camera.orthographicSize = Mathf.SmoothDamp (m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
            m_VirtualCamera.m_Lens.OrthographicSize = Mathf.SmoothDamp (m_VirtualCamera.m_Lens.OrthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
        }
        
        private float FindRequiredSize ()
        {
            // Find the position the camera rig is moving towards in its local space.
            Vector3 desiredLocalPos = m_Camera.transform.InverseTransformPoint(m_DesiredPosition);

            // Start the camera's size calculation at zero.
            float size = 0f;

            // Go through all the targets...
            for (int i = 0; i < m_Targets.Length; i++)
            {
                // ... and if they aren't active continue on to the next target.
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;

                // Otherwise, find the position of the target in the camera's local space.
                Vector3 targetLocalPos = m_Camera.transform.InverseTransformPoint(m_Targets[i].position);

                // Find the position of the target from the desired position of the camera's local space.
                Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

                // Choose the largest out of the current size and the distance of the tank 'up' or 'down' from the camera.
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

                // Choose the largest out of the current size and the calculated size based on the tank being to the left or right of the camera.
                size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);
            }

            // Add the edge buffer to the size.
            size += m_ScreenEdgeBuffer;

            // Make sure the camera's size isn't below the minimum.
            size = Mathf.Max (size, m_MinSize);

            return size;
        }
        
        public void SetStartPositionAndSize ()
        {
            // Find the desired position.
            FindAveragePosition ();

            // Set the camera's position to the desired position without damping.
            transform.position = m_DesiredPosition;

            // Find and set the required size of the camera.
            //m_Camera.orthographicSize = FindRequiredSize ();
            m_VirtualCamera.m_Lens.OrthographicSize = FindRequiredSize ();
        }
        
        public void OnHitTank()
        {
            var multiChannelPerlin = m_VirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            DOTween.To(x => multiChannelPerlin.m_AmplitudeGain = x, multiChannelPerlin.m_AmplitudeGain, 1f, 0.2f)
                .OnComplete(() =>
                {
                    DOTween.To(x => multiChannelPerlin.m_AmplitudeGain = x, multiChannelPerlin.m_AmplitudeGain, 0f, 1f);
                });
        }
        
        #endregion

    }
}