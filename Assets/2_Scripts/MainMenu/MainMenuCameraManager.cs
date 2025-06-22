using System;
using KBCore.Refs;
using Unity.Cinemachine;
using UnityEngine;

public class MainMenuCameraManager : MonoBehaviour
{

    
    [Header("References")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField, Child (Flag.Optional)] private CinemachineCamera defaultCamera;


    private void OnValidate()
    {
        if (!mainMenuController)
        {
            mainMenuController = FindFirstObjectByType<MainMenuController>();
        }
        
        this.ValidateRefs();
    }

    private void OnEnable()
    {
        if (mainMenuController)
        {
            mainMenuController.OnElementSelected += OnElementSelected;
            mainMenuController.OnElementDeselected += OnElementDeselected;
            mainMenuController.OnElementInteracted += OnElementInteracted;
            mainMenuController.OnElementFinishedInteraction += OnElementFinishedInteraction;
        }
    }

    private void OnDisable()
    {
        if (mainMenuController)
        {
            mainMenuController.OnElementSelected -= OnElementSelected;
            mainMenuController.OnElementDeselected -= OnElementDeselected;
            mainMenuController.OnElementInteracted -= OnElementInteracted;
            mainMenuController.OnElementFinishedInteraction -= OnElementFinishedInteraction;
        }
    }

    private void Start()
    {
        defaultCamera.LookAt = mainMenuController.DefaultCameraLookAtPoint;
    }


    private void OnElementDeselected(MenuElement element)
    {
        if (!element) return;
        
        defaultCamera.LookAt = mainMenuController.DefaultCameraLookAtPoint;
    }

    private void OnElementSelected(MenuElement element)
    {
        if (!element) return;

        defaultCamera.LookAt = element.CameraLookAtPoint;
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
}
