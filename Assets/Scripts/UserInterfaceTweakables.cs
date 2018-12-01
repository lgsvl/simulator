using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceTweakables : MonoBehaviour
{
    //public RectTransform Panel;

    public GameObject FloatSlider;
    public GameObject CheckBox;

    public GameObject CameraView;

    public GameObject InputField;

    public List<GameObject> Elements { get; private set; }

    public List<GameObject> CameraElements {get; private set; }

    public UserInterfaceTweakables()
    {
        Elements = new List<GameObject>();
        CameraElements = new List<GameObject>();
    }

    public Slider AddFloatSlider(string name, string label, float min, float max, float init)
    {
        var ui = Instantiate(FloatSlider);
        ui.name = name;
        var text = ui.transform.Find("Text").GetComponent<Text>();
        var slider = ui.transform.Find("Slider").GetComponent<Slider>();
        var value = ui.transform.Find("Value").GetComponent<Text>();

        text.text = label;
        slider.minValue = min;
        slider.maxValue = max;

        slider.onValueChanged.AddListener(x =>
        {
            value.text = string.Format("{0:F1}", x);
        });

        slider.value = init;
        slider.onValueChanged.Invoke(init);

        Elements.Add(ui);

        return slider;
    }    

    public InputField AddTextbox(string name, string label, string initial)
    {
        var ui = Instantiate(InputField);
        ui.name = name;
        var text = ui.transform.Find("Label").GetComponent<Text>();
        var input = ui.transform.Find("Input").GetComponent<InputField>();
        text.text = label;
        Elements.Add(ui);
        return input;
    }

    public Toggle AddCheckbox(string name, string label, bool init)
    {
        var ui = Instantiate(CheckBox);
        ui.name = name;
        var text = ui.transform.Find("Label").GetComponent<Text>();
        var toggle = ui.GetComponent<Toggle>();
        text.text = label;
        toggle.isOn = init;
        Elements.Add(ui);
        return toggle;
    }

    public RenderTextureDisplayer AddCameraPreview(string name, string label, Camera camera)
    {
        var ui = Instantiate(CameraView);
        ui.name = name;
        ui.GetComponentInChildren<Text>().text = name;
        var cameraPreview = ui.transform.GetComponent<RenderTextureDisplayer>();
        cameraPreview.renderCamera = camera;
        cameraPreview.gameObject.SetActive(false);
        CameraElements.Add(ui);
        return cameraPreview;
    }
}
