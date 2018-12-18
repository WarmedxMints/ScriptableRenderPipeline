using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Utilities for <see cref="CameraSettings"/>.</summary>
    public static class CameraSettingsUtilities
    {
        /// <summary>Applies <paramref name="settings"/> to <paramref name="cam"/>.</summary>
        /// <param name="cam">Camera to update.</param>
        /// <param name="settings">Settings to apply.</param>
        public static void ApplySettings(this Camera cam, CameraSettings settings)
        {
            if (settings.frameSettings == null)
                throw new InvalidOperationException("'frameSettings' must not be null.");

            var add = cam.GetComponent<HDAdditionalCameraData>()
                ?? cam.gameObject.AddComponent<HDAdditionalCameraData>();

            add.SetPersistentFrameSettings(settings.frameSettings);
            // Frustum
            cam.nearClipPlane = settings.frustum.nearClipPlane;
            cam.farClipPlane = settings.frustum.farClipPlane;
            cam.fieldOfView = settings.frustum.fieldOfView;
            cam.aspect = settings.frustum.aspect;
            cam.projectionMatrix = settings.frustum.GetUsedProjectionMatrix();
            // Culling
            cam.useOcclusionCulling = settings.culling.useOcclusionCulling;
            cam.cullingMask = settings.culling.cullingMask;
            // Buffer clearing
            add.clearColorMode = settings.bufferClearing.clearColorMode;
            add.backgroundColorHDR = settings.bufferClearing.backgroundColorHDR;
            add.clearDepth = settings.bufferClearing.clearDepth;
            // Volumes
            add.volumeLayerMask = settings.volumes.layerMask;
            add.volumeAnchorOverride = settings.volumes.anchorOverride;
            // HD Specific
            add.customRenderingSettings = settings.customRenderingSettings;

            add.OnAfterDeserialize();
        }

        /// <summary>Applies <paramref name="settings"/> to <paramref name="cam"/>.</summary>
        /// <param name="cam">Camera to update.</param>
        /// <param name="settings">Settings to apply.</param>
        public static void ApplySettings(this Camera cam, CameraPositionSettings settings)
        {
            // Position
            cam.transform.position = settings.position;
            cam.transform.rotation = settings.rotation;
            cam.worldToCameraMatrix = settings.GetUsedWorldToCameraMatrix();
        }
    }
}
