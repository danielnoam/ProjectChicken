using UnityEngine;
using Unity.Cinemachine;


namespace DNExtensions
{
    [AddComponentMenu("Cinemachine/Extensions/Rotation Offset Extension")]
        [SaveDuringPlay]
        [DisallowMultipleComponent]
        [CameraPipeline(CinemachineCore.Stage.Aim)] // Changed from Finalize to Aim

    public class CinemachineRotationOffsetExtension : CinemachineExtension
    {
        [Tooltip("Additional rotation offset to apply (in euler angles)")]
        public Vector3 RotationOffset = Vector3.zero;

        /// <summary>
        /// Set the rotation offset from external code
        /// </summary>
        /// <param name="offset">Euler angles for the offset</param>
        public void SetRotationOffset(Vector3 offset)
        {
            RotationOffset = offset;
        }

        /// <summary>
        /// Set the rotation offset from external code using a quaternion
        /// </summary>
        /// <param name="offset">Quaternion offset</param>
        public void SetRotationOffset(Quaternion offset)
        {
            RotationOffset = offset.eulerAngles;
        }

        /// <summary>
        /// Add to the current rotation offset
        /// </summary>
        /// <param name="additionalOffset">Additional euler angles to add</param>
        public void AddRotationOffset(Vector3 additionalOffset)
        {
            RotationOffset += additionalOffset;
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Apply the rotation offset at the Aim stage (before blending)
            if (stage == CinemachineCore.Stage.Aim)
            {
                Quaternion rotationOffset = Quaternion.Euler(RotationOffset);
                state.RawOrientation = state.RawOrientation * rotationOffset;
            }
        }
    }
}