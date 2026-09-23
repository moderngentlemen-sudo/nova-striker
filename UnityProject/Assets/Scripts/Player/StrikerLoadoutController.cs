using NovaStriker.Data;
using NovaStriker.Input;
using UnityEngine;

namespace NovaStriker.Player
{
    /// <summary>
    /// Per-player weapon and Guardian loadout. Selection state is independent
    /// for each of the four local Striker slots.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikerLoadoutController : MonoBehaviour
    {
        [SerializeField] private NovaCombatController combat;
        [SerializeField] private WeaponDefinition[] weapons;
        [SerializeField] private GuardianDefinition[] guardians;

        private int weaponIndex;
        private int guardianIndex;

        private bool weaponCyclePressed;
        private bool guardianCyclePressed;
        private bool guardianActivatePressed;

        public WeaponDefinition CurrentWeapon =>
            weapons != null && weapons.Length > 0
                ? weapons[
                    Mathf.Clamp(
                        weaponIndex,
                        0,
                        weapons.Length - 1
                    )
                ]
                : null;

        public GuardianDefinition CurrentGuardian =>
            guardians != null && guardians.Length > 0
                ? guardians[
                    Mathf.Clamp(
                        guardianIndex,
                        0,
                        guardians.Length - 1
                    )
                ]
                : null;

        public bool GuardianActivatePressedThisStep { get; private set; }

        private void Reset()
        {
            combat = GetComponent<NovaCombatController>();
        }

        private void Awake()
        {
            if (!combat)
                combat = GetComponent<NovaCombatController>();

            ApplyWeapon();
        }

        public void Configure(
            WeaponDefinition[] weaponSet,
            GuardianDefinition[] guardianSet)
        {
            weapons = weaponSet;
            guardians = guardianSet;
            weaponIndex = 0;
            guardianIndex = 0;
            ApplyWeapon();
        }

        public void SetInput(PlayerInputState input)
        {
            weaponCyclePressed |= input.WeaponCyclePressed;
            guardianCyclePressed |= input.GuardianCyclePressed;
            guardianActivatePressed |= input.GuardianActivatePressed;
        }

        private void FixedUpdate()
        {
            GuardianActivatePressedThisStep = false;

            if (weaponCyclePressed)
                CycleWeapon();

            if (guardianCyclePressed)
                CycleGuardian();

            if (guardianActivatePressed)
                GuardianActivatePressedThisStep = true;

            weaponCyclePressed = false;
            guardianCyclePressed = false;
            guardianActivatePressed = false;
        }

        public void CycleWeapon()
        {
            if (weapons == null || weapons.Length == 0)
                return;

            weaponIndex =
                (weaponIndex + 1) %
                weapons.Length;

            ApplyWeapon();
        }

        public void CycleGuardian()
        {
            if (guardians == null || guardians.Length == 0)
                return;

            guardianIndex =
                (guardianIndex + 1) %
                guardians.Length;
        }

        public bool SelectWeaponById(string id)
        {
            if (
                string.IsNullOrEmpty(id) ||
                weapons == null
            )
            {
                return false;
            }

            for (int i = 0; i < weapons.Length; i++)
            {
                if (
                    weapons[i] &&
                    weapons[i].Id == id
                )
                {
                    weaponIndex = i;
                    ApplyWeapon();
                    return true;
                }
            }

            return false;
        }

        public bool SelectGuardian(GuardianId id)
        {
            if (guardians == null)
                return false;

            for (int i = 0; i < guardians.Length; i++)
            {
                if (
                    guardians[i] &&
                    guardians[i].Id == id
                )
                {
                    guardianIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void ApplyWeapon()
        {
            if (combat && CurrentWeapon)
                combat.SetWeapon(CurrentWeapon);
        }
    }
}
