﻿using Asteroids.Scene;

using UnityEngine;

namespace Asteroids.Entities
{
    [RequireComponent(typeof(Renderer), typeof(Rigidbody2D))]
    public class ScreenWrapper : MonoBehaviour
    {
        // https://gamedevelopment.tutsplus.com/articles/create-an-asteroids-like-screen-wrapping-effect-with-unity--gamedev-15055

        private new Camera camera;
        private new Rigidbody2D rigidbody;
        private Renderer[] renderers;
        private bool isWrappingX;
        private bool isWrappingY;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void Awake()
        {
            camera = Camera.main;
            rigidbody = GetComponent<Rigidbody2D>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
        private void FixedUpdate()
        {
            if (GlobalMementoManager.IsRewinding)
                return;

            if (IsVisible())
            {
                isWrappingX = false;
                isWrappingY = false;
                return;
            }

            // Prevent objects from getting stuck out of the screen
            rigidbody.velocity = (rigidbody.velocity * (1 - Time.fixedDeltaTime * .3f)) - (rigidbody.position.normalized * Time.fixedDeltaTime);

            if (isWrappingX && isWrappingY)
                return;

            Vector3 viewportPosition = camera.WorldToViewportPoint(rigidbody.position);
            Vector3 newPosition = rigidbody.position;

            if (!isWrappingX && (viewportPosition.x > 1 || viewportPosition.x < 0))
            {
                newPosition.x *= -1;
                isWrappingX = true;
            }

            if (!isWrappingY && (viewportPosition.y > 1 || viewportPosition.y < 0))
            {
                newPosition.y *= -1;
                isWrappingY = true;
            }

            rigidbody.position = newPosition;
        }

        private bool IsVisible()
        {
            foreach (Renderer renderer in renderers)
                if (renderer.isVisible)
                    return true;
            return false;
        }
    }
}