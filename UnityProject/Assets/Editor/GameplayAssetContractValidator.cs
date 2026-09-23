using System;
using System.Collections.Generic;
using NovaStriker.Commerce;
using NovaStriker.Core;
using NovaStriker.Data;
using NovaStriker.Platform;
using NovaStriker.Presentation;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools
{
    /// <summary>
    /// Validates pre-Blender data/presentation contracts that are not covered
    /// by scene-independent campaign validation. Generated greybox assets are
    /// checked when present; absence is reported as a warning so source-only
    /// CI can still run before the Mechanics Lab is regenerated.
    /// </summary>
    public static class GameplayAssetContractValidator
    {
        private const string GeneratedRoot = "Assets/Greybox/Generated";
        private const string CommerceCatalogPath =
            GeneratedRoot + "/CommerceCatalog.asset";

        [MenuItem(
            "Nova Striker/Validation/Run Asset + Presentation Contract Validation",
            priority = 22)]
        public static void RunInteractive()
        {
            Validate();
        }

        public static void RunForCommandLine()
        {
            bool passed = Validate();

            if (Application.isBatchMode)
                EditorApplication.Exit(passed ? 0 : 1);
        }

        private static bool Validate()
        {
            int errors = 0;
            int warnings = 0;

            ValidateWeaponContracts(ref errors, ref warnings);
            ValidateGuardianContracts(ref errors, ref warnings);
            ValidateCommerceContracts(ref errors, ref warnings);
            ValidatePresentationContracts(ref errors);
            ValidateScalabilityContracts(ref errors);

            string summary =
                "Nova Striker asset/presentation contract validation: " +
                errors + " error(s), " +
                warnings + " warning(s).";

            if (errors > 0)
                Debug.LogError(summary);
            else if (warnings > 0)
                Debug.LogWarning(summary);
            else
                Debug.Log(summary);

            return errors == 0;
        }

        private static void ValidateWeaponContracts(
            ref int errors,
            ref int warnings)
        {
            Array behaviors = Enum.GetValues(typeof(WeaponBehavior));

            if (behaviors.Length != 12)
            {
                Error(
                    ref errors,
                    "WeaponBehavior must expose exactly 12 baseline weapon behaviors."
                );
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:WeaponDefinition",
                new[] { GeneratedRoot }
            );

            if (guids.Length == 0)
            {
                Warning(
                    ref warnings,
                    "Generated weapon assets are absent. Rebuild the Mechanics Lab before Unity Play Mode QA."
                );
                return;
            }

            if (guids.Length != 12)
            {
                Error(
                    ref errors,
                    "Expected 12 generated weapon assets, found " +
                    guids.Length + "."
                );
            }

            HashSet<string> ids = new();
            HashSet<WeaponBehavior> coveredBehaviors = new();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                WeaponDefinition weapon =
                    AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);

                if (!weapon)
                {
                    Error(ref errors, "Unable to load weapon asset at " + path + ".");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(weapon.Id))
                    Error(ref errors, path + " has an empty weapon id.");
                else if (!ids.Add(weapon.Id))
                    Error(ref errors, "Duplicate weapon id: " + weapon.Id + ".");

                if (string.IsNullOrWhiteSpace(weapon.DisplayName))
                    Error(ref errors, weapon.Id + " has an empty display name.");

                if (weapon.ProjectileSpeed <= 0f)
                    Error(ref errors, weapon.Id + " has a non-positive projectile speed.");

                if (
                    weapon.UnchargedDamage < 0f ||
                    weapon.Tier1Damage < weapon.UnchargedDamage ||
                    weapon.Tier2Damage < weapon.Tier1Damage ||
                    weapon.Tier3Damage < weapon.Tier2Damage
                )
                {
                    Error(
                        ref errors,
                        weapon.Id + " has an invalid charge-tier damage progression."
                    );
                }

                if (
                    weapon.UnchargedRadius <= 0f ||
                    weapon.Tier1Radius < weapon.UnchargedRadius ||
                    weapon.Tier2Radius < weapon.Tier1Radius ||
                    weapon.Tier3Radius < weapon.Tier2Radius
                )
                {
                    Error(
                        ref errors,
                        weapon.Id + " has an invalid charge-tier radius progression."
                    );
                }

                coveredBehaviors.Add(weapon.Behavior);
            }

            if (coveredBehaviors.Count != behaviors.Length)
            {
                Error(
                    ref errors,
                    "Generated weapons cover " + coveredBehaviors.Count + "/" +
                    behaviors.Length + " baseline weapon behaviors."
                );
            }
        }

        private static void ValidateGuardianContracts(
            ref int errors,
            ref int warnings)
        {
            Array ids = Enum.GetValues(typeof(GuardianId));

            if (ids.Length != 6)
            {
                Error(
                    ref errors,
                    "GuardianId must expose exactly six baseline Guardians."
                );
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:GuardianDefinition",
                new[] { GeneratedRoot }
            );

            if (guids.Length == 0)
            {
                Warning(
                    ref warnings,
                    "Generated Guardian assets are absent. Rebuild the Mechanics Lab before Unity Play Mode QA."
                );
                return;
            }

            if (guids.Length != 6)
            {
                Error(
                    ref errors,
                    "Expected 6 generated Guardian assets, found " +
                    guids.Length + "."
                );
            }

            HashSet<GuardianId> seen = new();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GuardianDefinition guardian =
                    AssetDatabase.LoadAssetAtPath<GuardianDefinition>(path);

                if (!guardian)
                {
                    Error(ref errors, "Unable to load Guardian asset at " + path + ".");
                    continue;
                }

                if (!seen.Add(guardian.Id))
                    Error(ref errors, "Duplicate Guardian id: " + guardian.Id + ".");

                if (string.IsNullOrWhiteSpace(guardian.DisplayName))
                    Error(ref errors, guardian.Id + " has an empty display name.");

                if (guardian.CooldownSeconds < 0f)
                    Error(ref errors, guardian.Id + " has a negative cooldown.");

                if (string.IsNullOrWhiteSpace(guardian.GameplayDescription))
                {
                    Error(
                        ref errors,
                        guardian.Id + " has no gameplay description."
                    );
                }
            }

            if (seen.Count != ids.Length)
            {
                Error(
                    ref errors,
                    "Generated Guardian definitions cover " + seen.Count + "/" +
                    ids.Length + " Guardian ids."
                );
            }
        }

        private static void ValidateCommerceContracts(
            ref int errors,
            ref int warnings)
        {
            CommerceCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CommerceCatalog>(CommerceCatalogPath);

            if (!catalog)
            {
                Warning(
                    ref warnings,
                    "Generated commerce catalog is absent. Rebuild the Mechanics Lab before entitlement QA."
                );
                return;
            }

            HashSet<string> productIds = new();
            HashSet<string> entitlementIds = new();

            for (int i = 0; i < catalog.Products.Count; i++)
            {
                CommerceProductDefinition product = catalog.Products[i];

                if (product == null)
                {
                    Error(ref errors, "Commerce catalog contains a null product entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.ProductId))
                    Error(ref errors, "Commerce product has an empty product id.");
                else if (!productIds.Add(product.ProductId))
                    Error(ref errors, "Duplicate commerce product id: " + product.ProductId + ".");

                if (string.IsNullOrWhiteSpace(product.EntitlementId))
                {
                    Error(
                        ref errors,
                        product.ProductId + " has an empty entitlement id."
                    );
                }
                else if (!entitlementIds.Add(product.EntitlementId))
                {
                    Error(
                        ref errors,
                        "Duplicate commerce entitlement id: " +
                        product.EntitlementId + "."
                    );
                }

                if (string.IsNullOrWhiteSpace(product.DisplayName))
                    Error(ref errors, product.ProductId + " has an empty display name.");

                if (product.GameplayStatPurchase)
                {
                    Warning(
                        ref warnings,
                        product.ProductId +
                        " modifies gameplay stats; verify this is an intentional exception to the cosmetic/content-first commerce policy."
                    );
                }
            }
        }

        private static void ValidatePresentationContracts(ref int errors)
        {
            if (string.IsNullOrWhiteSpace(GameplayPresentationContract.Version))
                Error(ref errors, "Presentation contract version is empty.");

            ValidateUniqueNames(
                GameplayPresentationContract.AnimatorParameters.AllNames,
                "Animator parameter",
                ref errors
            );

            ValidateUniqueNames(
                GameplayPresentationContract.Sockets.AllNames,
                "production socket",
                ref errors
            );

            Array cues = Enum.GetValues(typeof(GameplayCueType));
            HashSet<string> eventIds = new();
            HashSet<string> vfxIds = new();
            HashSet<string> audioIds = new();

            for (int i = 0; i < cues.Length; i++)
            {
                GameplayCueType cue = (GameplayCueType)cues.GetValue(i);
                string eventId = GameplayPresentationContract.EventId(cue);
                string vfxId = GameplayPresentationContract.VfxId(cue);
                string audioId = GameplayPresentationContract.AudioId(cue);

                if (
                    string.IsNullOrWhiteSpace(eventId) ||
                    !eventIds.Add(eventId)
                )
                {
                    Error(ref errors, cue + " has an invalid/duplicate presentation event id.");
                }

                if (
                    string.IsNullOrWhiteSpace(vfxId) ||
                    !vfxIds.Add(vfxId)
                )
                {
                    Error(ref errors, cue + " has an invalid/duplicate VFX id.");
                }

                if (
                    string.IsNullOrWhiteSpace(audioId) ||
                    !audioIds.Add(audioId)
                )
                {
                    Error(ref errors, cue + " has an invalid/duplicate audio id.");
                }
            }
        }

        private static void ValidateScalabilityContracts(ref int errors)
        {
            Array tiers = Enum.GetValues(typeof(RuntimeQualityTier));

            if (tiers.Length != 4)
            {
                Error(
                    ref errors,
                    "Runtime scalability must retain exactly four presentation tiers: Low/Medium/High/Ultra."
                );
            }
        }

        private static void ValidateUniqueNames(
            string[] values,
            string label,
            ref int errors)
        {
            HashSet<string> seen = new();

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];

                if (string.IsNullOrWhiteSpace(value))
                {
                    Error(ref errors, label + " contract contains an empty name.");
                    continue;
                }

                if (!seen.Add(value))
                    Error(ref errors, "Duplicate " + label + " name: " + value + ".");
            }
        }

        private static void Error(ref int count, string message)
        {
            count++;
            Debug.LogError("[Asset Contract Validation] " + message);
        }

        private static void Warning(ref int count, string message)
        {
            count++;
            Debug.LogWarning("[Asset Contract Validation] " + message);
        }
    }
}
