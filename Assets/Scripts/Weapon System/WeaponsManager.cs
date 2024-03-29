﻿using AvalonStudios.Additions.Attributes;

using UnityEngine;

using IsProperty = Enderlook.Unity.Attributes.IsPropertyAttribute;

namespace Asteroids.WeaponSystem
{
    public sealed class WeaponsManager : MonoBehaviour
    {
#pragma warning disable CS0649
        [field: StyledHeader("Setup")]
        [field: SerializeField, IsProperty, Tooltip("Cast position")]
        public Transform CastPoint { get; private set; }

        [SerializeField, Tooltip("Weapon package.")]
        private WeaponsPack weaponPack;

        [field: SerializeField, Tooltip("Fire input."), IsProperty]
        public KeyCode FireInput { get; private set; } = KeyCode.Space;

        [field: SerializeField, Tooltip("Input to change weapon."), IsProperty]
        public KeyCode ChangeWeaponInput { get; private set; }
#pragma warning restore CS0649

        public Rigidbody2D Rigidbody2D { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void Awake()
        {
            Rigidbody2D = GetComponent<Rigidbody2D>();
            weaponPack?.Initialize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void Update() => weaponPack?.Update();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void OnDrawGizmos() => weaponPack?.OnDrawGizmos();
    }
}
