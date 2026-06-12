using UnityEngine;
using UnityEngine.UI;
using TMPro; // 使用 TextMeshPro
using System.Collections.Generic;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("音效設定")]
    [Tooltip("請將專案中的 AudioMixer 拖入此處")]
    public AudioMixer mainMixer;
    // Mixer 中的參數名稱 (Exposed Parameters)
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    private Resolution[] _resolutions;
    private List<Resolution> _filteredResolutions;

    private void Start()
    {
        InitializeDisplaySettings();
        InitializeAudioSettings();
    }

    private void InitializeDisplaySettings()
    {
        // 1. 設置顯示模式 Dropdown
        displayModeDropdown.ClearOptions();
        List<string> displayOptions = new List<string> { "全螢幕 (Fullscreen)", "視窗化 (Windowed)", "無邊框視窗 (Borderless)" };
        displayModeDropdown.AddOptions(displayOptions);
        // 設定當前狀態
        displayModeDropdown.value = Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0;
        displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);

        // 2. 設置解析度 Dropdown
        _resolutions = Screen.resolutions;
        _filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        double currentRefreshRate = Screen.currentResolution.refreshRateRatio.value;
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            // 過濾掉重新整理頻率不同的選項 (避免清單過長)
            // 注意：Unity 6 使用 refreshRateRatio
            if (Mathf.Abs((float)(_resolutions[i].refreshRateRatio.value - currentRefreshRate)) < 1f)
            {
                _filteredResolutions.Add(_resolutions[i]);
                string option = _resolutions[i].width + " x " + _resolutions[i].height;
                options.Add(option);

                if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = _filteredResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void InitializeAudioSettings()
    {
        // 初始化 Slider 數值
        float currentMusic, currentSFX;
        mainMixer.GetFloat(MIXER_MUSIC, out currentMusic);
        mainMixer.GetFloat(MIXER_SFX, out currentSFX);

        // 轉換 dB 到 Slider (假設 Slider 是 -80 到 0 或 0 到 1 線性轉換，這裡示範 0-1)
        // 這裡簡化處理：直接假設 Slider 設定為 -80(Min) 到 0(Max)
        musicSlider.value = currentMusic;
        sfxSlider.value = currentSFX;

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetDisplayMode(int index)
    {
        switch (index)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: Screen.fullScreenMode = FullScreenMode.Windowed; break;
            case 2: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
        }
    }

    public void SetResolution(int index)
    {
        Resolution resolution = _filteredResolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    public void SetMusicVolume(float volume)
    {
        // 假設 Slider 直接對應 dB (-80 ~ 0)
        mainMixer.SetFloat(MIXER_MUSIC, volume);
    }

    public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat(MIXER_SFX, volume);
    }
}