using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(PostProcessingBehaviour))]
public class PostProcessingWeatherController : DayNightEventListener {
    private PostProcessingBehaviour postBehavior;
    
    public PostProcessingProfile sunriseProfile;
    public PostProcessingProfile dayProfile;
    public PostProcessingProfile nightProfile;
    public PostProcessingProfile sunsetProfile;

    void Start()
    {
        postBehavior = GetComponent<PostProcessingBehaviour>();
        if (postBehavior.profile != null)
        {
            postBehavior.profile = Instantiate<PostProcessingProfile>(postBehavior.profile);
        }
        else
        {
            postBehavior.profile = Instantiate<PostProcessingProfile>(sunriseProfile);
        }
    }

    protected override void OnSunRise()
    {
        CopyProfile(sunriseProfile);
    }

    protected override void OnDay()
    {
        CopyProfile(dayProfile);
    }

    protected override void OnNight()
    {
        CopyProfile(nightProfile);
    }

    protected override void OnSunSet()
    {
        CopyProfile(sunsetProfile);
    }

    private void CopyProfile(PostProcessingProfile profile)
    {
        if (profile != null)
        {
            postBehavior.profile.name = (profile.name + "(Clone)");
            postBehavior.profile.ambientOcclusion.settings = profile.ambientOcclusion.settings;
            postBehavior.profile.antialiasing.settings = profile.antialiasing.settings;
            postBehavior.profile.bloom.settings = profile.bloom.settings;
            postBehavior.profile.chromaticAberration.settings = profile.chromaticAberration.settings;
            postBehavior.profile.colorGrading.settings = profile.colorGrading.settings;
            postBehavior.profile.debugViews.settings = profile.debugViews.settings;
            postBehavior.profile.depthOfField.settings = profile.depthOfField.settings;
            postBehavior.profile.eyeAdaptation.settings = profile.eyeAdaptation.settings;
            postBehavior.profile.grain.settings = profile.grain.settings;
            postBehavior.profile.motionBlur.settings = profile.motionBlur.settings;
            postBehavior.profile.screenSpaceReflection.settings = profile.screenSpaceReflection.settings;
            postBehavior.profile.userLut.settings = profile.userLut.settings;
            postBehavior.profile.vignette.settings = profile.vignette.settings;
        }
    }
}
