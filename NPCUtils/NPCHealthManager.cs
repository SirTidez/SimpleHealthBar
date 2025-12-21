#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.NPCs;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
#endif
using System.Collections.Generic;
using MelonLoader;
using SimpleHealthBar.Helpers;
using SimpleHealthBar.UI;
using UnityEngine;

namespace SimpleHealthBar.NPCUtils
{
    public static class NPCHealthManager
    {
        private static NPCManager NPCManager;
        private static Dictionary<NPC, WorldSpaceHealthBar> HealthBars;
        private static bool IsInitialized = false;

        /// <summary>
        /// Initializes the NPC healthbar manager, setting up the logger and preparing for world-space healthbars.
        /// </summary>
        /// <param name="logger">The logger instance to use for output.</param>
        public static void Init(MelonLogger.Instance logger)
        {
            IsInitialized = NPCManager.InstanceExists;
            if (IsInitialized)
            {
                NPCManager = NPCManager.Instance;
                HealthBars = new Dictionary<NPC, WorldSpaceHealthBar>();
                ModLogger.Info("NPC Healthbar Manager Initialized!");
            }
            else
                ModLogger.Error("NPCManager not found, aborting NPC health reporting!");
        }

        /// <summary>
        /// Gets the squared distance from the player to an NPC.
        /// </summary>
        /// <param name="npc">The NPC to measure distance to.</param>
        /// <returns>The squared distance, or float.MaxValue if invalid.</returns>
        private static float GetSquaredDistanceFromPlayer(NPC npc)
        {
            if (npc == null || Player.Local == null)
                return float.MaxValue;

            // Check if NPC has required components (S1API custom NPCs might not have all components)
            if (npc.Movement == null || npc.Health == null)
                return float.MaxValue;

            Vector3 npcPosition;
#if MONO
            // Mono workaround - exclusively use transform position to avoid MissingMethodException
            npcPosition = npc.transform.position;
#else
            // IL2CPP - use the standard approach
            npcPosition = npc.Movement.FootPosition;
#endif

            return (npcPosition - Player.Local.CameraPosition).sqrMagnitude;
        }

        /// <summary>
        /// Updates all NPC healthbars each frame, creating new ones for NPCs within range and removing ones outside range.
        /// </summary>
        public static void OnUpdate()
        {
            if (!IsInitialized || HealthBars == null || NPCManager == null)
                return;

            if (!Preferences.NPCHealthBarEnabled.Value)
            {
                // If disabled, destroy all healthbars
                CleanupAllHealthbars();
                return;
            }

            float radiusSquared = Preferences.NPCWorldSpaceRadius.Value;
            radiusSquared *= radiusSquared; // Square the radius for comparison

            // Track NPCs that should have healthbars
            HashSet<NPC> npcsInRange = new HashSet<NPC>();

            // Process all NPCs in registry
            foreach (NPC npc in NPCManager.NPCRegistry)
            {
                if (npc == null)
                    continue;

                // Check if NPC has required components
                if (npc.Health == null || npc.transform == null)
                    continue;

                // Check distance
                float sqrDist = GetSquaredDistanceFromPlayer(npc);
                if (sqrDist <= radiusSquared)
                {
                    npcsInRange.Add(npc);

                    // Create healthbar if it doesn't exist
                    if (!HealthBars.ContainsKey(npc))
                    {
                        try
                        {
                            WorldSpaceHealthBar healthBar = new WorldSpaceHealthBar(npc);
                            HealthBars[npc] = healthBar;
                            ModLogger.Debug($"Created health bar for {npc.fullName}");
                        }
                        catch (System.Exception ex)
                        {
                            ModLogger.Error($"Failed to create health bar for {npc.fullName}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Update existing healthbar
                        WorldSpaceHealthBar healthBar = HealthBars[npc];
                        if (healthBar != null && healthBar.IsValid())
                        {
                            healthBar.Update();
                        }
                        else
                        {
                            // Healthbar is invalid, remove it
                            HealthBars.Remove(npc);
                            if (healthBar != null)
                                healthBar.Destroy();
                        }
                    }
                }
            }

            // Remove healthbars for NPCs that are no longer in range or have been destroyed
            List<NPC> toRemove = new List<NPC>();
            foreach (var kvp in HealthBars)
            {
                NPC npc = kvp.Key;
                WorldSpaceHealthBar healthBar = kvp.Value;

                // Remove if NPC is null, destroyed, or out of range
                if (npc == null || npc.transform == null || !npcsInRange.Contains(npc))
                {
                    toRemove.Add(npc);
                    if (healthBar != null)
                        healthBar.Destroy();
                }
            }

            // Clean up removed healthbars
            foreach (NPC npc in toRemove)
            {
                HealthBars.Remove(npc);
            }
        }

        /// <summary>
        /// Unloads the NPC healthbar manager, destroying all healthbars and clearing references.
        /// </summary>
        public static void Unload()
        {
            CleanupAllHealthbars();
            IsInitialized = false;
            HealthBars = null;
        }

        /// <summary>
        /// Destroys all active healthbars.
        /// </summary>
        private static void CleanupAllHealthbars()
        {
            if (HealthBars == null)
                return;

            foreach (var kvp in HealthBars)
            {
                if (kvp.Value != null)
                    kvp.Value.Destroy();
            }

            HealthBars.Clear();
        }
        /// <summary>
        /// Returns whether the NPC healthbar manager has been initialized.
        /// </summary>
        /// <returns>True if initialized, otherwise false.</returns>
        public static bool Initialized() { return IsInitialized; }
    }
}
