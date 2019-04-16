/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuzzerSensor : MonoBehaviour, Comm.BridgeClient
{
    public enum BuzzerModeTypes
    {
        BuzzerOff,
        BuzzerOne,
        BuzzerTwo,
        BuzzerThree
    };
    //private BuzzerModeTypes currentBuzzerMode = BuzzerModeTypes.BuzzerOff;

    public string buzzerTopicName = "/central_controller/buzzer";
    private Comm.Bridge Bridge;
    private bool isEnabled = false;
    //private bool isFirstEnabled = true;

    public AudioClip buzzerSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        audioSource.clip = buzzerSound;
        AddUIElement();
    }

    private void SetBuzzerMode(BuzzerModeTypes mode)
    {
        audioSource.loop = false;
        StopAllCoroutines();
        switch (mode)
        {
            case BuzzerModeTypes.BuzzerOff:
                audioSource.Stop();
                break;
            case BuzzerModeTypes.BuzzerOne:
                StartCoroutine(TimedBuzzer(1f));
                break;
            case BuzzerModeTypes.BuzzerTwo:
                StartCoroutine(TimedBuzzer(3f));
                break;
            case BuzzerModeTypes.BuzzerThree:
                audioSource.loop = true;
                audioSource.Play();
                break;
            default:
                break;
        }
    }

    private IEnumerator TimedBuzzer(float time)
    {
        while (true)
        {
            audioSource.loop = true;
            audioSource.Play();
            yield return new WaitForSecondsRealtime(time);
            audioSource.loop = false;
            audioSource.Stop();
            yield return new WaitForSecondsRealtime(1f);
        }
        
    }

    private void ParseMsg(int msg)
    {
        isEnabled = msg == 0 ? false : true;
    }

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            Bridge.AddService<Ros.Srv.SetEffect, Ros.Srv.SetEffectResponse>(buzzerTopicName, msg =>
            {
                if (msg.data == 0)
                {
                    SetBuzzerMode(BuzzerModeTypes.BuzzerOff);
                }
                else if (msg.data == 1)
                {
                    SetBuzzerMode(BuzzerModeTypes.BuzzerOne);
                }
                else if (msg.data == 2)
                {
                    SetBuzzerMode(BuzzerModeTypes.BuzzerTwo);
                }
                else if (msg.data == 3)
                {
                    SetBuzzerMode(BuzzerModeTypes.BuzzerThree);
                }
                return new Ros.Srv.SetEffectResponse() { success = true, message = "message" };
            });
        };
    }

    public void OnRosConnected()
    {
        
    }

    private void AddUIElement() // TODO combine with tweakables prefab for all sensors issues on start though
    {
        var ledModeDropdown = GetComponentInParent<UserInterfaceTweakables>().AddDropdown("BuzzerMode", "Buzzer Mode: ", System.Enum.GetNames(typeof(BuzzerModeTypes)).ToList());
        ledModeDropdown.onValueChanged.AddListener(x => SetBuzzerMode((BuzzerModeTypes)x));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
