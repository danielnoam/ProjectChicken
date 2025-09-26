
using KBCore.Refs;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

[SelectionBase]
public class MainMenuCameraManager : MonoBehaviour
{

    public static MainMenuCameraManager Instance { get; private set; }
    
    [Header("Main Camera Settings")]
    [SerializeField] private float lookAtDuration = 0.5f;
    [SerializeField] private Ease lookAtEase = Ease.Linear;
    
    [Header("References")]
    [SerializeField] private Transform cameraLookAtTarget;
    [SerializeField, Scene(Flag.EditableAnywhere)] private MainMenuController mainMenuController;
    [SerializeField, Child (Flag.Optional), HideInInspector] private CinemachineCamera defaultCamera;
    [SerializeField, Child (Flag.Optional), HideInInspector] private CinemachineRotationComposer defaultCameraRotationComposer;



    private Tween _screenPositionTween;
    private Sequence _lookAtSequence;
    private Vector3 _defaultTargetOffset;


    private void OnValidate()
    {
        if (!mainMenuController)
        {
            mainMenuController = FindFirstObjectByType<MainMenuController>();
        }
        else
        {
            _defaultTargetOffset = defaultCameraRotationComposer.TargetOffset;
            cameraLookAtTarget.position = mainMenuController.DefaultCameraLookAtPoint.position;
            defaultCamera.Target.TrackingTarget = mainMenuController.DefaultCameraPosition;
        }
        

        
        this.ValidateRefs();
    }
    
    
    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        
        _defaultTargetOffset = defaultCameraRotationComposer.TargetOffset;
        cameraLookAtTarget.position = mainMenuController.DefaultCameraLookAtPoint.position;
        defaultCamera.Target.TrackingTarget = mainMenuController.DefaultCameraPosition;
    }

    private void OnEnable()
    {
        if (mainMenuController)
        {
            mainMenuController.onElementSelected += OnElementSelected;
            mainMenuController.onElementDeselected += OnElementDeselected;
            mainMenuController.onElementInteracted += OnElementInteracted;
            mainMenuController.onElementFinishedInteraction += OnElementFinishedInteraction;
        }
    }

    private void OnDisable()
    {
        if (mainMenuController)
        {
            mainMenuController.onElementSelected -= OnElementSelected;
            mainMenuController.onElementDeselected -= OnElementDeselected;
            mainMenuController.onElementInteracted -= OnElementInteracted;
            mainMenuController.onElementFinishedInteraction -= OnElementFinishedInteraction;
        }
    }


    private void OnElementDeselected(MainMenuElement element)
    {
        if (!element) return;
        UpdateCameraTarget(mainMenuController.DefaultCameraLookAtPoint.position, _defaultTargetOffset);
    }

    private void OnElementSelected(MainMenuElement element)
    {
        if (!element) return;
        
        UpdateCameraTarget(element.CameraLookAtPoint.position,element.CameraLookAtOffset);
    }
    
    private void OnElementInteracted(MainMenuElement element)
    {
        if (!element || !element.InteractionCamera) return;
        
        element.InteractionCamera.Priority = 10;
        defaultCamera.Priority = 0;
    }
    
    private void OnElementFinishedInteraction(MainMenuElement element)
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
    



    
    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {

        if (cameraLookAtTarget)
        {
            
            Gizmos.DrawSphere(cameraLookAtTarget.position + defaultCameraRotationComposer.TargetOffset, 0.1f);
        }

    }
#endif


}
