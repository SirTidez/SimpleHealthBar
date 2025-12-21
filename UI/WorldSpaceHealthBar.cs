#if MONO
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
#endif
using SimpleHealthBar;
using SimpleHealthBar.Helpers;
using UnityEngine;
using UnityEngine.UI;

#if !MONO
using Il2CppTMPro;
#else
using TMPro;
#endif

namespace SimpleHealthBar.UI
{
    /// <summary>
    /// World-space health bar that appears above an NPC's head.
    /// Implements billboard behavior to always face the camera.
    /// </summary>
    public class WorldSpaceHealthBar
    {
        private GameObject CanvasObject;
        private Canvas Canvas;
        private GameObject HealthBarObject;
        private Slider HealthSlider;
        private Image FillImage;
        private RectTransform HealthTextDisplay;
        private TextMeshProUGUI HealthText;
        private NPC TargetNPC;
        private float CurrentFill;
        private float LastHealthUpdateTime;
        private float FadeSpeed = 2f;
        private float FadeDelay = 4f;
        private float FontSize = 8f;
        private Camera MainCamera;

        /// <summary>
        /// Creates a new world-space health bar for the specified NPC.
        /// </summary>
        /// <param name="npc">The NPC to attach the health bar to.</param>
        public WorldSpaceHealthBar(NPC npc)
        {
            if (npc == null || npc.transform == null)
            {
                ModLogger.Error("Cannot create WorldSpaceHealthBar: NPC or transform is null");
                return;
            }

            TargetNPC = npc;
            MainCamera = Camera.main;

            // Create world-space canvas
            CanvasObject = new GameObject($"WorldSpaceHealthBar_{npc.fullName}");
            CanvasObject.transform.SetParent(npc.transform, false);
            
            // Position above NPC head
            float headOffset = Preferences.NPCHeadOffset.Value;
            CanvasObject.transform.localPosition = new Vector3(0f, headOffset, 0f);
            
            // Set scale for world-space UI (base scale 0.005 multiplied by user preference)
            const float BaseScale = 0.005f;
            float scaleMultiplier = Preferences.NPCHealthBarScale.Value;
            float scale = BaseScale * scaleMultiplier;
            CanvasObject.transform.localScale = new Vector3(scale, scale, scale);

            // Add Canvas component with world-space render mode
            Canvas = CanvasObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.WorldSpace;
            Canvas.sortingOrder = 100; // Ensure it renders above other objects
            
            // Add CanvasScaler for proper UI scaling
            CanvasScaler scaler = CanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            
            // Add GraphicRaycaster (required for Canvas)
            CanvasObject.AddComponent<GraphicRaycaster>();

            // Create health bar base
            HealthBarObject = new GameObject("HealthBarBase");
            HealthBarObject.transform.SetParent(CanvasObject.transform, false);
            RectTransform barBase = HealthBarObject.AddComponent<RectTransform>();
            barBase.sizeDelta = new Vector2(120f, 6f);
            barBase.anchoredPosition = Vector2.zero;
            barBase.localPosition = Vector3.zero;
            barBase.localRotation = Quaternion.identity;
            barBase.localScale = Vector3.one;

            // Add slider
            HealthSlider = HealthBarObject.AddComponent<Slider>();
            HealthSlider.minValue = 0f;
            HealthSlider.maxValue = 100f;
            HealthSlider.value = 100f;

            // Create fill
            GameObject fill = new GameObject("HealthBarFill");
            fill.transform.SetParent(HealthBarObject.transform, false);
            RectTransform fillBar = fill.AddComponent<RectTransform>();
            FillImage = fill.AddComponent<Image>();
            FillImage.color = Color.red;
            fillBar.anchorMin = Vector2.zero;
            fillBar.anchorMax = Vector2.one;
            fillBar.offsetMin = Vector2.zero;
            fillBar.offsetMax = Vector2.zero;
            HealthSlider.fillRect = fillBar;

            // Create text group
            GameObject healthTextGroup = new GameObject("HealthTextGroup");
            healthTextGroup.transform.SetParent(HealthBarObject.transform, false);
            HealthTextDisplay = healthTextGroup.AddComponent<RectTransform>();
            HealthTextDisplay.anchorMin = new Vector2(0.5f, 0.5f);
            HealthTextDisplay.anchorMax = new Vector2(0.5f, 0.5f);
            HealthTextDisplay.pivot = new Vector2(0.5f, 0.5f);
            HealthTextDisplay.sizeDelta = new Vector2(250f, 20f);
            HealthTextDisplay.anchoredPosition = new Vector2(0f, 12f);
            
            HealthText = healthTextGroup.AddComponent<TextMeshProUGUI>();
            HealthText.alignment = TextAlignmentOptions.Center;
            HealthText.fontSize = FontSize;
            HealthText.text = "";

            // Add text shadow
            Shadow textShadow = healthTextGroup.AddComponent<Shadow>();
            textShadow.effectColor = Color.black;
            textShadow.effectDistance = new Vector2(1, -1);

            // Initialize health
            if (TargetNPC.Health != null)
            {
                CurrentFill = TargetNPC.Health.Health;
                UpdateText(TargetNPC.fullName);
            }

            LastHealthUpdateTime = Time.time;
            ModLogger.Debug($"Created world-space health bar for {npc.fullName}");
        }

        /// <summary>
        /// Updates the health bar each frame. Handles billboard behavior and health updates.
        /// </summary>
        public void Update()
        {
            if (CanvasObject == null || TargetNPC == null)
                return;

            // Update billboard to face camera
            UpdateBillboard();

            // Update health if changed
            if (TargetNPC.Health != null)
            {
                float currentHealth = TargetNPC.Health.Health;
                if (currentHealth != CurrentFill)
                {
                    CurrentFill = currentHealth;
                    UpdateText(TargetNPC.fullName);
                    LastHealthUpdateTime = Time.time;
                }
            }

            // Update slider and fading
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the billboard to face the camera.
        /// </summary>
        private void UpdateBillboard()
        {
            if (CanvasObject == null)
                return;

            // Get camera position
            Vector3 cameraPosition;
            if (Player.Local != null)
            {
                cameraPosition = Player.Local.CameraPosition;
            }
            else if (MainCamera != null)
            {
                cameraPosition = MainCamera.transform.position;
            }
            else
            {
                MainCamera = Camera.main;
                if (MainCamera != null)
                    cameraPosition = MainCamera.transform.position;
                else
                    return;
            }

            // Make health bar face camera
            Vector3 direction = cameraPosition - CanvasObject.transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                CanvasObject.transform.rotation = Quaternion.LookRotation(-direction);
            }
        }

        /// <summary>
        /// Updates the visual elements (slider, fading, etc.)
        /// NPC world-space healthbars fade in/out based on NPC state (dead, knocked out, in building).
        /// </summary>
        private void UpdateVisuals()
        {
            bool fillExists = FillImage != null;
            bool textExists = HealthText != null;

            if (fillExists && textExists)
            {
                // Update slider value
                float newSliderValue = Mathf.Lerp(HealthSlider.value, CurrentFill, Time.deltaTime * FadeDelay);
                
#if !MONO
                HealthSlider.Set(newSliderValue);
#else
                HealthSlider.value = newSliderValue;
#endif

                // Determine if healthbar should be visible based on NPC state
                bool barVisible = true;
                if (TargetNPC != null && TargetNPC.Health != null)
                {
                    barVisible = !(TargetNPC.Health.IsDead || TargetNPC.Health.IsKnockedOut || TargetNPC.isInBuilding);
                }
                
                // Calculate target alpha (1.0 if visible, 0.0 if hidden)
                float targetAlpha = barVisible ? 1f : 0f;
                
                // Smoothly fade in/out the health bar fill
                if (fillExists)
                {
                    Color color = FillImage.color;
                    color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * FadeSpeed);
                    FillImage.color = color;
                }

                // Smoothly fade in/out the health text
                if (textExists)
                {
                    HealthText.alpha = Mathf.Lerp(HealthText.alpha, targetAlpha, Time.deltaTime * FadeSpeed);
                }
            }
        }

        /// <summary>
        /// Updates the health text display.
        /// </summary>
        /// <param name="name">The NPC's name.</param>
        public void UpdateText(string name)
        {
            if (HealthText == null)
                return;

            if (CurrentFill <= 0f)
            {
                HealthText.text = "";
                return;
            }

            var healthText = $"{name} - {Mathf.FloorToInt(CurrentFill)} / 100 HP";
            if (HealthText.text != healthText)
            {
                HealthText.text = healthText;
                LastHealthUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Sets the current health value.
        /// </summary>
        /// <param name="health">The health value to set.</param>
        public void SetCurrentHealth(float health)
        {
            CurrentFill = health;
        }

        /// <summary>
        /// Gets the current health value.
        /// </summary>
        /// <returns>The current health value.</returns>
        public float GetCurrentHealth()
        {
            return CurrentFill;
        }

        /// <summary>
        /// Shows the health bar.
        /// </summary>
        public void Show()
        {
            if (!Preferences.NPCHealthBarEnabled.Value)
                return;

            LastHealthUpdateTime = Time.time;
            
            if (FillImage != null)
            {
                Color color = FillImage.color;
                color.a = 1f;
                FillImage.color = Color.Lerp(FillImage.color, color, FadeSpeed);
            }
            
            if (HealthText != null)
            {
                HealthText.alpha = Mathf.Lerp(HealthText.alpha, 1f, FadeSpeed);
            }
        }

        /// <summary>
        /// Hides the health bar.
        /// </summary>
        public void Hide()
        {
            if (FillImage != null)
            {
                Color color = FillImage.color;
                color.a = 0f;
                FillImage.color = Color.Lerp(FillImage.color, color, Time.deltaTime * FadeSpeed);
            }

            if (HealthText != null)
                HealthText.alpha = Mathf.Lerp(HealthText.alpha, 0f, Time.deltaTime * FadeSpeed);
        }

        /// <summary>
        /// Destroys the health bar GameObject.
        /// </summary>
        public void Destroy()
        {
            if (CanvasObject != null)
            {
                UnityEngine.Object.Destroy(CanvasObject);
                CanvasObject = null;
            }
        }

        /// <summary>
        /// Checks if the health bar is still valid (NPC and GameObject exist).
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return CanvasObject != null && TargetNPC != null && TargetNPC.transform != null;
        }

        /// <summary>
        /// Gets the target NPC.
        /// </summary>
        /// <returns>The target NPC.</returns>
        public NPC GetNPC()
        {
            return TargetNPC;
        }
    }
}

