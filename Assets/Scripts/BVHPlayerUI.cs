using UnityEngine;
using UnityEngine.UI;

public class BVHPlayerUI : MonoBehaviour
{
    public BVHAnimator animator;
    public Button btnPlayPause;
    public Button btnReset;
    public Slider sliderSpeed;
    public Slider sliderTime;
    public Text txtTime;

    private bool draggingTimeSlider;

    void Start()
    {
        sliderSpeed.minValue = 0;
        sliderSpeed.maxValue = 3f;
        sliderSpeed.value = 1f;
        sliderSpeed.onValueChanged.AddListener(v => Time.timeScale = v);

        sliderTime.onValueChanged.AddListener(v =>
        {
            if (draggingTimeSlider)
            {
                animator.SetTime(v);
            }
        });
    }

    void Update()
    {
        if (animator == null || !animator.HasData()) return;

        sliderTime.maxValue = animator.GetTotalTime();
        if (!draggingTimeSlider)
        {
            sliderTime.value = animator.GetTime();
        }
        txtTime.text = $"{animator.GetTime():F2} / {animator.GetTotalTime():F2} s";
    }

    public void TogglePlay() => animator.TogglePlay();
    public void ResetAnim() => animator.ResetAnim();
    public void OnTimeSliderDown() => draggingTimeSlider = true;
    public void OnTimeSliderUp() => draggingTimeSlider = false;
}
