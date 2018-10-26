using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class DoorAnimatorInfo
{
    public Animator animator;
    public AnimatorStateInfo prevAnimatorStateInfo;
    public CarDoorType doorType;
    public bool moving;
}

public class VehicleAnimationManager : MonoBehaviour
{
    public DoorAnimatorInfo FrontLDoorAnimInfo;
    public DoorAnimatorInfo FrontRDoorAnimInfo;
    public DoorAnimatorInfo RearLDoorAnimInfo;
    public DoorAnimatorInfo RearRDoorAnimInfo;
    public DoorAnimatorInfo BackDoorAnimInfo;

    public List<Animator> WindshieldWiperAnims;
    private AnimatorStateInfo prevWiperStateInfo;

    VehicleController currentVehicleController;
    List<DoorAnimatorInfo> ManagedAnimatorInfos = new List<DoorAnimatorInfo>();

    void Start()
    {
        currentVehicleController = gameObject.GetComponent<VehicleController>();

        FieldInfo[] fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(DoorAnimatorInfo))
            {
                ManagedAnimatorInfos.Add((DoorAnimatorInfo)field.GetValue(this));
            }
        }
    }

    //void Update()
    //{
    //    UpdateAnimatorInfos();
    //}

    //private void UpdateAnimatorInfos()
    //{
    //    foreach (var DoorAnimInfo in ManagedAnimatorInfos)
    //    {
    //        var previous = DoorAnimInfo.prevAnimatorStateInfo;
    //        var current = DoorAnimInfo.animator.GetCurrentAnimatorStateInfo(0);

    //        if (previous.shortNameHash != current.shortNameHash || (previous.shortNameHash == current.shortNameHash && previous.normalizedTime >= 1.0f && current.normalizedTime < 1.0f))
    //        {
    //            DoorAnimInfo.moving = true;

    //            if (current.IsName("Open"))
    //                OnStartDoorOpen(DoorAnimInfo.doorType);

    //            else if (current.IsName("Close"))
    //                OnStartDoorClose(DoorAnimInfo.doorType);
    //        }
    //        else if (previous.shortNameHash != current.shortNameHash || (previous.shortNameHash == current.shortNameHash && previous.normalizedTime < 1.0f && current.normalizedTime >= 1.0f))
    //        {
    //            DoorAnimInfo.moving = false;

    //            if (previous.IsName("Open"))
    //                OnFinishDoorOpen(DoorAnimInfo.doorType);

    //            else if (previous.IsName("Close"))
    //                OnFinishDoorClose(DoorAnimInfo.doorType);
    //        }

    //        DoorAnimInfo.prevAnimatorStateInfo = current;
    //    }
    //}

    //public void OnStartDoorOpen(CarDoorType doorType)
    //{
    //    switch (doorType)
    //    {
    //        case CarDoorType.FrontL:
    //            currentVehicleController.doorFrontLOpen = true;
    //            break;
    //        case CarDoorType.FrontR:
    //            currentVehicleController.doorFrontROpen = true;
    //            break;
    //        case CarDoorType.RearL:
    //            currentVehicleController.doorRearLOpen = true;
    //            break;
    //        case CarDoorType.RearR:
    //            currentVehicleController.doorRearROpen = true;
    //            break;
    //        case CarDoorType.Back:
    //            currentVehicleController.doorBackOpen = true;
    //            break;
    //        default:
    //            break;
    //    }
    //    currentVehicleController.EnsureDoorState(true);
    //}

    //public void OnStartDoorClose(CarDoorType doorType)
    //{
    //    switch (doorType)
    //    {
    //        case CarDoorType.FrontL:
    //            break;
    //        case CarDoorType.FrontR:
    //            break;
    //        case CarDoorType.RearL:
    //            break;
    //        case CarDoorType.RearR:
    //            break;
    //        case CarDoorType.Back:
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //public void OnFinishDoorOpen(CarDoorType doorType)
    //{
    //    switch (doorType)
    //    {
    //        case CarDoorType.FrontL:
    //            break;
    //        case CarDoorType.FrontR:
    //            break;
    //        case CarDoorType.RearL:
    //            break;
    //        case CarDoorType.RearR:
    //            break;
    //        case CarDoorType.Back:
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //public void OnFinishDoorClose(CarDoorType doorType)
    //{
    //    switch (doorType)
    //    {
    //        case CarDoorType.FrontL:
    //            currentVehicleController.doorFrontLOpen = false;
    //            break;
    //        case CarDoorType.FrontR:
    //            currentVehicleController.doorFrontROpen = false;
    //            break;
    //        case CarDoorType.RearL:
    //            currentVehicleController.doorRearLOpen = false;
    //            break;
    //        case CarDoorType.RearR:
    //            currentVehicleController.doorRearROpen = false;
    //            break;
    //        case CarDoorType.Back:
    //            currentVehicleController.doorBackOpen = false;
    //            break;
    //        default:
    //            break;
    //    }
    //    currentVehicleController.EnsureDoorState(false);
    //}

    //public void PlayFrontLDoorAnim(bool open)
    //{
    //    var animInfo = FrontLDoorAnimInfo;
    //    if (animInfo.moving) return;
    //    animInfo.animator.Play(open ? "Close" : "Open", 0);
    //}

    //public void PlayFrontRDoorAnim(bool open)
    //{
    //    var animInfo = FrontRDoorAnimInfo;
    //    if (animInfo.moving) return;
    //    animInfo.animator.Play(open ? "Close" : "Open", 0);
    //}

    //public void PlayRearLDoorAnim(bool open)
    //{
    //    var animInfo = RearLDoorAnimInfo;
    //    if (animInfo.moving) return;
    //    animInfo.animator.Play(open ? "Close" : "Open", 0);
    //}

    //public void PlayRearRDoorAnim(bool open)
    //{
    //    var animInfo = RearRDoorAnimInfo;
    //    if (animInfo.moving) return;
    //    animInfo.animator.Play(open ? "Close" : "Open", 0);
    //}

    //public void PlayBackDoorAnim(bool open)
    //{
    //    var animInfo = BackDoorAnimInfo;
    //    if (animInfo.moving) return;
    //    animInfo.animator.Play(open ? "Close" : "Open", 0);
    //}

    public void ToggleRearRightCarDoor(bool state)
    {
        if (RearRDoorAnimInfo.animator == null) return;

        string triggerName = "Init";
        triggerName = state ? "OpenDoor" : "CloseDoor";
        AnimatorControllerParameter[] tempACP = RearRDoorAnimInfo.animator.parameters;
        for (int i = 0; i < tempACP.Length; i++)
        {
            if (triggerName == tempACP[i].name)
                RearRDoorAnimInfo.animator.SetTrigger(triggerName);
        }
    }

    public bool CanWiperSwitchLevel()
    {
        var curAnimState = WindshieldWiperAnims[0].GetCurrentAnimatorStateInfo(0);
        if (curAnimState.IsName("Default"))
        {
            return true;
        }
        else if (curAnimState.normalizedTime >= 1.0f)
        {
            foreach (var animator in WindshieldWiperAnims)
            {
                animator.Play(curAnimState.shortNameHash, 0, 0.0f);
            }
            return true;
        }
        return false;
    }

    public void PlayWiperAnim(int level)
    {
        string stateName = "Default";
        switch (level)
        {
            case 1:
                stateName = "Low";
                break;
            case 2:
                stateName = "Low";
                break;
            case 3:
                stateName = "Mid";
                break;
            case 4:
                stateName = "High";
                break;
        }

        foreach (var animator in WindshieldWiperAnims)
        {
            if (!prevWiperStateInfo.IsName(stateName))
            {
                animator.Play(stateName, 0);
            }
        }
    }

    public void StopWiperAnim()
    {
        foreach (var animator in WindshieldWiperAnims)
        {
            animator.Play("Default", 0);
        }
    }
}
