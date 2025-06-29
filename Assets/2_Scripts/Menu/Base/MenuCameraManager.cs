using System;
using KBCore.Refs;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

[SelectionBase]
public class MenuCameraManager : MonoBehaviour
{

    [Header("Main Camera Settings")]
    [SerializeField] private float lookAtDuration = 1f;
    [SerializeField] private Ease lookAtEase = Ease.Linear;
    
    [Header("References")]
    [SerializeField] private MenuController menuController;
    [SerializeField] private Transform cameraLookAtTarget;
    [SerializeField, Child (Flag.Optional)] private CinemachineCamera defaultCamera;
    [SerializeField, Child (Flag.Optional)] private CinemachineRotationComposer defaultCameraRotationComposer;



    private Tween _screenPositionTween;
    private Sequence _lookAtSequence;
    private Vector3 _defaultTargetOffset;


    private void OnValidate()
    {
        if (!menuController)
        {
            menuController = FindFirstObjectByType<MenuController>();
        }
        

        
        this.ValidateRefs();
    }
    
    
    private void Awake()
    {
        _defaultTargetOffset = defaultCameraRotationComposer.TargetOffset;
        cameraLookAtTarget.position = menuController.DefaultCameraLookAtPoint.position;

    }

    private void OnEnable()
    {
        if (menuController)
        {
            menuController.OnElementSelected += OnElementSelected;
            menuController.OnElementDeselected += OnElementDeselected;
            menuController.OnElementInteracted += OnElementInteracted;
            menuController.OnElementFinishedInteraction += OnElementFinishedInteraction;
        }
    }

    private void OnDisable()
    {
        if (menuController)
        {
            menuController.OnElementSelected -= OnElementSelected;
            menuController.OnElementDeselected -= OnElementDeselected;
            menuController.OnElementInteracted -= OnElementInteracted;
            menuController.OnElementFinishedInteraction -= OnElementFinishedInteraction;
        }
    }


    private void OnElementDeselected(MenuElement element)
    {
        if (!element) return;
        UpdateCameraTarget(menuController.DefaultCameraLookAtPoint.position, _defaultTargetOffset);
    }

    private void OnElementSelected(MenuElement element)
    {
        if (!element) return;
        
        UpdateCameraTarget(element.CameraLookAtPoint.position,element.TargetOffset);
    }
    
    private void OnElementInteracted(MenuElement element)
    {
        if (!element || !element.InteractionCamera) return;
        
        element.InteractionCamera.Priority = 10;
        defaultCamera.Priority = 0;
    }
    
    private void OnElementFinishedInteraction(MenuElement element)
    {
        if (!element || !element.InteractionCamera) return;

        element.InteractionCamera.Priority = 0;
        defaultCamera.Priority = 10;
    }

    private void UpdateCameraTarget(Vector3 targetPosition, Vector3 screenPosition)
    {
        if (_lookAtSequence.isAlive)
        {
            _lookAtSequence.Stop();
        }

        Vector3 positionStartValue = cameraLookAtTarget.transform.position;
        Vector3 targetOffsetStartValue = defaultCameraRotationComposer.TargetOffset;

        _lookAtSequence = Sequence.Create()
                .Group(Tween.Position(
                    cameraLookAtTarget.transform,
                    startValue: positionStartValue, 
                    endValue: targetPosition, 
                    duration: lookAtDuration,
                    ease: lookAtEase))
                .Group(Tween.Custom(
                    startValue: targetOffsetStartValue, 
                    endValue: screenPosition, 
                    duration: lookAtDuration,
                    onValueChange: vector3 => defaultCameraRotationComposer.TargetOffset = vector3,
                    ease: lookAtEase))

            ;
    }


    private void OnDrawGizmos()
    {

        if (cameraLookAtTarget)
        {
            
            Gizmos.DrawSphere(cameraLookAtTarget.position + defaultCameraRotationComposer.TargetOffset, 0.1f);
        }

    }
}
