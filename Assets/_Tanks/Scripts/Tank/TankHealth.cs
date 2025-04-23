using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour
    {
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
        public Image m_FillImage;                           // The image component of the slider.
        public Color m_FullHealthColor = Color.green;    // The color the health bar will be when on full health.
        public Color m_ZeroHealthColor = Color.red;      // The color the health bar will be when on no health.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        [HideInInspector] public bool m_HasShield;          // Has the tank picked up a shield power up?
        
        [SerializeField]
        private List<MeshRenderer> m_TankMeshRenderers = new ();         // The mesh renderer of the tank to apply the dissolve effect.
        
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private float m_CurrentHealth;                      // How much health the tank currently has.
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
        private float m_ShieldValue;                        // Percentage of reduced damage when the tank has a shield.
        private bool m_IsInvincible;                        // Is the tank invincible in this moment?
        
        public event Action OnTakeDamage = null; // Event to notify when the tank takes damage. 

        private void Awake ()
        {
            // Set the slider max value to the max health the tank can have
            m_Slider.maxValue = m_StartingHealth;
        }

        private void OnDestroy()
        {
        }

        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;
            ResetCutoffHeightRenderers();
            // Update the health slider's value and color.
            SetHealthUI();
        }

        private void ResetCutoffHeightRenderers()
        {
            // Reset the cutoff height of the tank mesh renderers to 0 (not dissolved)
            foreach (var meshRenderer in m_TankMeshRenderers)
            {
                Material tankMaterial = meshRenderer.material;
                tankMaterial.SetFloat("_CutoffHeight", 1f); // Set the dissolve amount to 0 (not dissolved)
            }
        }

        public void TakeDamage (float amount)
        {
            // Check if the tank is not invincible
            if (!m_IsInvincible)
            {
                // Reduce current health by the amount of damage done.
                m_CurrentHealth -= amount * (1 - m_ShieldValue);

                // Change the UI elements appropriately.
                SetHealthUI ();
                // Call the OnTakeDamage event to notify subscribers that the tank has taken damage.
                OnTakeDamage?.Invoke();

                // If the current health is at or below zero and it has not yet been registered, call OnDeath.
                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath ();
                }
            }
        }


        public void IncreaseHealth(float amount)
        {
            // Check if adding the amount would keep the health within the maximum limit
            if (m_CurrentHealth + amount <= m_StartingHealth)
            {
                // If the new health value is within the limit, add the amount
                m_CurrentHealth += amount;
            }
            else
            {
                // If the new health exceeds the starting health, set it at the maximum
                m_CurrentHealth = m_StartingHealth;
            }

            // Change the UI elements appropriately.
            SetHealthUI();
        }


        public void ToggleShield (float shieldAmount)
        {
            // Inverts the value of has shield.
            m_HasShield = !m_HasShield;

            // Stablish the amount of damage that will be reduced by the shield
            if (m_HasShield)
            {
                m_ShieldValue = shieldAmount;
            }
            else
            {
                m_ShieldValue = 0;
            }
        }

        public void ToggleInvincibility()
        {
            m_IsInvincible = !m_IsInvincible;
        }


        private void SetHealthUI ()
        {
            // Set the slider's value appropriately.
            m_Slider.value = m_CurrentHealth;

            // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
            m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }


        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            
            
            ApplyDissolveEffect();
            
            // Turn the tank off.
        }

        private void ApplyDissolveEffect()
        {
            var sequence = DOTween.Sequence();
            
            // Get the material of the tank mesh renderer
            foreach (var meshRenderer in m_TankMeshRenderers)
            {
                Material tankMaterial = meshRenderer.material;
                //tankMaterial.SetFloat("_CutoffHeight", 0.5f); // Set the dissolve amount to 1 (fully dissolved)
                var doFloat = tankMaterial.DOFloat(0f, "_CutoffHeight", 1f).SetEase(Ease.OutCubic); // Animate the dissolve effect
                sequence.Join(doFloat);
            }

            sequence.OnComplete(() =>
            {
                m_Dead = true;
                gameObject.SetActive(false);

            });


            // Set the dissolve effect parameters
        }
    }
}