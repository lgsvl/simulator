using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceTweakable : MonoBehaviour
{
    public RectTransform Panel;

    public Text DisplayText;
    public InputField InputBox;
    public GameObject FloatSlider;
    public GameObject CheckBox;

    public static UserInterfaceTweakable Instance;

    void Awake()
    {
        Instance = this;
        
    }

    void OnDestroy()
    {
        Instance = null;
    }

    void Update()
    {
        Panel.gameObject.SetActive(True);
    }

    public Text AddText(string label)
    {
        var ui = Instantiate(Text, Panel);
        var text = ui.transform.Find("Text").GetComponent<Text>();
        text.text = label;
    }

    public Slider AddFloatSlider(string title, float min, float max, float init)
    {
        var ui = Instantiate(FloatSlider, Panel);
        var text = ui.transform.Find("Text").GetComponent<Text>();
        var slider = ui.transform.Find("Slider").GetComponent<Slider>();
        var value = ui.transform.Find("Value").GetComponent<Text>();

        text.text = title;
        slider.minValue = min;
        slider.maxValue = max;

        slider.onValueChanged.AddListener(x =>
        {
            value.text = string.Format("{0:F1}", x);
        });

        slider.value = init;
        slider.onValueChanged.Invoke(init);

        return slider;
    }    

    public Toggle AddCheckbox(string title, bool init)
    {
        var ui = Instantiate(CheckBox, Panel);
        var text = ui.transform.Find("Text").GetComponent<Text>();
        var toggle = ui.transform.Find("Toggle").GetComponent<Toggle>();

        text.text = title;
        toggle.isOn = init;

        return toggle;
    }
}
