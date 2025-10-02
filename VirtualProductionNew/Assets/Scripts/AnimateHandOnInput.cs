using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty PinchAnimationAction;
    public InputActionProperty GripAnimationAction;

    public Animator HandAnimator;
    void Start()
    {
        
    }

    
    void Update()
    {
        float TriggerValue = PinchAnimationAction.action.ReadValue<float>();
        HandAnimator.SetFloat("Trigger", TriggerValue);

        float GripValue = GripAnimationAction.action.ReadValue<float>();
        HandAnimator.SetFloat("Grip", GripValue);
    }
}
