using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class UiManager : MonoBehaviour
{
    [SerializeField]
    internal List<ResultHistory> multiplierObjs = new List<ResultHistory>();
    [SerializeField]
    internal List<float> multiplierObjsYPositions = new List<float>();
    [SerializeField]
    internal  float multiplierObjHeight;
    [SerializeField]
    internal float startPosition;
    [SerializeField]
    private Sprite manualSpriteEnabled, autoSpriteEnabled, manualSpriteDisabled, autoSpriteDisabled;
    [SerializeField]
    internal Button autoBetToggle;
    [SerializeField]
    private Button ManualBetToggle;
    [SerializeField]
    internal Button StopAutoBet;
    [SerializeField]
    private GameObject AutoBetSection;
    [SerializeField]
    private Slider frequencySlider;
    [SerializeField]
    private SocketIOManager socketManager;
        

    [Header("Popus UI")]
    [SerializeField]
    private GameObject MainPopup_Object;
    [SerializeField]
    private GameObject PaytablePopup_Object;

    [Header("Settings Popup")]
    [SerializeField]
    private GameObject SettingsPopup_Object;
    [SerializeField]
    private Button SettingsExit_Button;
    [SerializeField]
    private Button Sound_Button;
    [SerializeField]
    private Button Music_Button;

    [SerializeField]
    private GameObject MusicOn_Object;
    [SerializeField]
    private GameObject MusicOff_Object;
    [SerializeField]
    private GameObject SoundOn_Object;
    [SerializeField]
    private GameObject SoundOff_Object;

    [Header("Disconnection Popup")]
    [SerializeField]
    private Button CloseDisconnect_Button;
    [SerializeField]
    private GameObject DisconnectPopup_Object;

    [Header("AnotherDevice Popup")]
    [SerializeField]
    private Button CloseAD_Button;
    [SerializeField]
    private GameObject ADPopup_Object;

    [Header("Reconnection Popup")]
    [SerializeField]
    private TMP_Text reconnect_Text;
    [SerializeField]
    private GameObject ReconnectPopup_Object;

    [Header("LowBalance Popup")]
    [SerializeField]
    private Button LBExit_Button;
    [SerializeField]
    private GameObject LBPopup_Object;

    [Header("Quit Popup")]
    [SerializeField]
    private GameObject QuitPopup_Object;
    [SerializeField]
    private Button YesQuit_Button;
    [SerializeField]
    private Button NoQuit_Button;
    [SerializeField]
    private Button CrossQuit_Button;

   
    [SerializeField]
    private Button Settings_Button;
    [SerializeField]
    private Button Paytable_Button;
    [SerializeField]
    private Button PaytableExit_Button;
    [SerializeField]
    private Button GameExit_Button;
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private AudioManager audioController;
    bool isExit;
    bool isMusic;
    bool isSound;

    private void Start()
    {


        if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
        if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPopup(PaytablePopup_Object); });

        if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
        if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

        if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
        if (Settings_Button) Settings_Button.onClick.AddListener(delegate { OpenPopup(SettingsPopup_Object); });

        if (SettingsExit_Button) SettingsExit_Button.onClick.RemoveAllListeners();
        if (SettingsExit_Button) SettingsExit_Button.onClick.AddListener(delegate { ClosePopup(SettingsPopup_Object); });

        if (MusicOn_Object) MusicOn_Object.SetActive(true);
        if (MusicOff_Object) MusicOff_Object.SetActive(false);

        if (SoundOn_Object) SoundOn_Object.SetActive(true);
        if (SoundOff_Object) SoundOff_Object.SetActive(false);

        if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
        if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate {
            OpenPopup(QuitPopup_Object);
            Debug.Log("Quit event: pressed Big_X button");

        });

        if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
        if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
                Debug.Log("quit event: pressed NO Button ");
            }
        });

        if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
        if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
                Debug.Log("quit event: pressed Small_X Button ");

            }
        });

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

        if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
        if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate {
            CallOnExitFunction();
            Debug.Log("quit event: pressed YES Button ");

        });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction);

        if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
        if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);

        

        if (audioController) audioController.ToggleMute(false);

        isMusic = true;
        isSound = true;

        if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
        if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

        if (Music_Button) Music_Button.onClick.RemoveAllListeners();
        if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);




        if (autoBetToggle) autoBetToggle.onClick.RemoveAllListeners();
        if (autoBetToggle) autoBetToggle.onClick.AddListener(delegate { toggleBetModes("Auto"); });

        if (ManualBetToggle) ManualBetToggle.onClick.RemoveAllListeners();
        if (ManualBetToggle) ManualBetToggle.onClick.AddListener(delegate { toggleBetModes("Manual"); });

        if(frequencySlider) frequencySlider.onValueChanged.AddListener(UpdateFrequency);
        initializeResultHistoryUI();
    }


    internal void resultHistoryAnimation(string multiplier)
    {
        for (int i = 0; i < multiplierObjs.Count; i++)
        {
            multiplierObjs[i].nextPosition(multiplier);
            
        }
       
    }


    void initializeResultHistoryUI()
    {
        float height = startPosition - multiplierObjHeight;

        for (int i = 0; i < multiplierObjs.Count; i++)
        {
            multiplierObjsYPositions.Add(height);
            multiplierObjs[i].heightQueuePosition = i;
            height -= multiplierObjHeight;
        }
        
    }

    private void UpdateFrequency(float value)
    {
        Mathf.Clamp(value, 0.2f, 2);
        gameManager.autoBetFrequency = 2f - value;
    }

    void toggleBetModes(string mode)
    {
        if (mode == "Auto")
        {
            AutoBetSection.SetActive(true);
            autoBetToggle.image.sprite = autoSpriteDisabled;
            ManualBetToggle.image.sprite = manualSpriteEnabled;
            Debug.Log("rannnnnnn");
        }
        else
        {
            autoBetToggle.image.sprite = autoSpriteEnabled;
            ManualBetToggle.image.sprite = manualSpriteDisabled;
            gameManager.autoBetCurrentCount = 0;
            gameManager.autoBetTotalCount = 0;
            AutoBetSection.SetActive(false);
        }

    }


    internal void LowBalPopup()
    {
        OpenPopup(LBPopup_Object);
    }


    internal void DisconnectionPopup(bool isReconnection)
    {
        if (!isExit)
        {
            OpenPopup(DisconnectPopup_Object);
        }
    }


    

    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object);
    }


    private void CallOnExitFunction()
    {
        isExit = true;
        audioController.PlayButtonAudio();
        
    }

    
    

    private void OpenPopup(GameObject Popup)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    private void ClosePopup(GameObject Popup)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(false);
        if (!DisconnectPopup_Object.activeSelf)
        {
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
        }
    }

    private void ToggleMusic()
    {
        isMusic = !isMusic;
        if (isMusic)
        {
            if (MusicOn_Object) MusicOn_Object.SetActive(true);
            if (MusicOff_Object) MusicOff_Object.SetActive(false);
            audioController.ToggleMute(false, "bg");
        }
        else
        {
            if (MusicOn_Object) MusicOn_Object.SetActive(false);
            if (MusicOff_Object) MusicOff_Object.SetActive(true);
            audioController.ToggleMute(true, "bg");
        }
    }

    private void UrlButtons(string url)
    {
        Application.OpenURL(url);
    }

    private void ToggleSound()
    {
        isSound = !isSound;
        if (isSound)
        {
            if (SoundOn_Object) SoundOn_Object.SetActive(true);
            if (SoundOff_Object) SoundOff_Object.SetActive(false);
            if (audioController) audioController.ToggleMute(false, "button");
            if (audioController) audioController.ToggleMute(false, "wl");
        }
        else
        {
            if (SoundOn_Object) SoundOn_Object.SetActive(false);
            if (SoundOff_Object) SoundOff_Object.SetActive(true);
            if (audioController) audioController.ToggleMute(true, "button");
            if (audioController) audioController.ToggleMute(true, "wl");
        }
    }

   

}
