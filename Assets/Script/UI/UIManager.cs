using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PlinkoGame.Data;

namespace PlinkoGame
{
    /// <summary>
    /// Manages all UI elements and popups with animations
    /// History management moved to HistoryManager
    /// Updated with comprehensive audio integration
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Betting UI")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI betAmountText;
        [SerializeField] private Button betButton;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private GameObject betDisabledOverlay;

        [Header("Risk Selection")]
        [SerializeField] private TMP_Dropdown riskDropdown;
        [SerializeField] private GameObject riskDisabledOverlay;

        [Header("Row Selection")]
        [SerializeField] private TMP_Dropdown rowDropdown;
        [SerializeField] private GameObject rowDisabledOverlay;

        [Header("Mode Toggle")]
        [SerializeField] private Button manualModeButton;
        [SerializeField] private Button autoplayModeButton;
        [SerializeField] private GameObject manualModeImage;
        [SerializeField] private GameObject autoplayModeImage;

        [Header("Autoplay Controls")]
        [SerializeField] private GameObject autoplayPanel;
        [SerializeField] private TMP_InputField autoplayRoundsInput;
        [SerializeField] private Button autoplayToggleButton;
        [SerializeField] private Image stopAutoplayImage;
        [SerializeField] private Button increaseRoundsButton;
        [SerializeField] private Button decreaseRoundsButton;
        [SerializeField] private GameObject infinityIcon;
        [SerializeField] private TextMeshProUGUI autoplayCountText;
        [SerializeField] private GameObject autoplayDisabledOverlay;

        [Header("Audio Toggles")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;

        [Header("Popups")]
        [SerializeField] private GameObject winPopupMainPanel;
        [SerializeField] private GameObject winPopupArea;
        [SerializeField] private TextMeshProUGUI winAmountText;
        [SerializeField] private TextMeshProUGUI winMultiplierText;

        [SerializeField] private GameObject errorPopupMainPanel;
        [SerializeField] private GameObject errorPopupArea;
        [SerializeField] private TextMeshProUGUI errorMessageText;
        [SerializeField] private Button errorPopupbtn;

        [SerializeField] private GameObject reconnectionPopupMainPanel;
        [SerializeField] private GameObject reconnectionPopupArea;

        [SerializeField] private GameObject disconnectionPopupMainPanel;
        [SerializeField] private GameObject disconnectionPopupArea;

        [SerializeField] private GameObject exitConfirmPopupMainPanel;
        [SerializeField] private GameObject exitConfirmPopupArea;

        [SerializeField] private GameObject gameInfoPopupMainPanel;
        [SerializeField] private GameObject gameInfoPopupArea;

        [Header("Exit Popup Buttons")]
        [SerializeField] private Button exitYesButton;
        [SerializeField] private Button exitNoButton;

        [Header("Disconnection Popup Button")]
        [SerializeField] private Button disconnectionOkButton;

        [Header("Game Exit")]
        [SerializeField] private Button forceCloseGameButton;


        [Header("Game Info")]
        [SerializeField] private Button gameInfoButton;
        [SerializeField] private Button gameInfoCloseButton;

        [Header("Hover Popup")]
        [SerializeField] private GameObject hoverPopup;
        [SerializeField] private TextMeshProUGUI hoverProfitText;
        [SerializeField] private TextMeshProUGUI hoverProbabilityText;
        [SerializeField] private RectTransform hoverArrow;
        [SerializeField] private Button hoverCloseButton;

        [Header("Popup Animation Settings")]
        [SerializeField] private float popupScaleInDuration = 0.3f;
        [SerializeField] private float popupScaleOutDuration = 0.2f;
        [SerializeField] private Ease popupScaleInEase = Ease.OutBack;
        [SerializeField] private Ease popupScaleOutEase = Ease.InBack;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private HistoryManager historyManager;

        private bool isManualMode = true;
        private bool isAutoplayActive;
        private int autoplayRounds = 0;
        private bool isHoverPopupVisible = false;
        private Vector3 currentHoverPosition;
        private double currentHoverProfit;
        private double currentHoverProbability;

        private void Awake()
        {
            SetupButtons();
            SetupDropdowns();
            SetupAudioToggles();
            HideAllPopups();
        }

        private void Start()
        {
            SetMode(true);
            UpdateAutoplayRoundsDisplay();
            InitializeAudioToggles();
        }

        // ============================================
        // AUDIO TOGGLE SETUP
        // ============================================

        private void SetupAudioToggles()
        {
            if (musicToggle != null)
            {
                // Remove listener temporarily to prevent triggering during setup
                musicToggle.onValueChanged.RemoveAllListeners();
                musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            }

            if (sfxToggle != null)
            {
                sfxToggle.onValueChanged.RemoveAllListeners();
                sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
            }
        }

        private void InitializeAudioToggles()
        {
            if (AudioManager.Instance != null)
            {
                if (musicToggle != null)
                {
                    // Use SetIsOnWithoutNotify to avoid triggering the callback
                    musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMusicEnabled());
                }

                if (sfxToggle != null)
                {
                    sfxToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsSFXEnabled());
                }
            }
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            AudioManager.Instance?.ToggleMusic(isOn);
            AudioManager.Instance?.PlayButtonClick();
        }

        private void OnSFXToggleChanged(bool isOn)
        {
            AudioManager.Instance?.ToggleSFX(isOn);
            if (isOn)
            {
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        // ============================================
        // BUTTON SETUP
        // ============================================

        private void SetupButtons()
        {
            if (betButton != null)
                betButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayStartButtonClick();
                    gameManager?.OnBetButtonClicked();
                });

            if (increaseBetButton != null)
                increaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    gameManager?.OnBetChanged(true);
                });

            if (decreaseBetButton != null)
                decreaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    gameManager?.OnBetChanged(false);
                });

            if (manualModeButton != null)
                manualModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(true);
                });

            if (autoplayModeButton != null)
                autoplayModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(false);
                });

            if (autoplayToggleButton != null)
                autoplayToggleButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayStartButtonClick();
                    OnAutoplayToggleClicked();
                });

            if (increaseRoundsButton != null)
                increaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    ChangeAutoplayRounds(1);
                });

            if (decreaseRoundsButton != null)
                decreaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    ChangeAutoplayRounds(-1);
                });

            if (forceCloseGameButton != null)
                forceCloseGameButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowExitConfirmPopup();
                });

            if (exitYesButton != null)
                exitYesButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnExitConfirmYes();
                });

            if (exitNoButton != null)
                exitNoButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnExitConfirmNo();
                });

            if (disconnectionOkButton != null)
                disconnectionOkButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnDisconnectionOk();
                });

            if (hoverCloseButton != null)
                hoverCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    HideHoverPopup();
                });

            if (errorPopupbtn != null)
                errorPopupbtn.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnErrorOK();
                });

            if (gameInfoButton != null)
                gameInfoButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowInfoPopup();
                });

            if (gameInfoCloseButton != null)
                gameInfoCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnInfoClose();
                });
        }

        private void SetupDropdowns()
        {
            if (riskDropdown != null)
            {
                riskDropdown.onValueChanged.AddListener(OnRiskChanged);
                var riskTrigger = riskDropdown.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (riskTrigger == null)
                {
                    riskTrigger = riskDropdown.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                AddDropdownClickTrigger(riskTrigger);
            }

            if (rowDropdown != null)
            {
                rowDropdown.onValueChanged.AddListener(OnRowChanged);
                var rowTrigger = rowDropdown.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (rowTrigger == null)
                {
                    rowTrigger = rowDropdown.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                AddDropdownClickTrigger(rowTrigger);
            }

            if (autoplayRoundsInput != null)
            {
                autoplayRoundsInput.interactable = false;
            }
        }

        private void AddDropdownClickTrigger(UnityEngine.EventSystems.EventTrigger trigger)
        {
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
            };
            entry.callback.AddListener((data) => { AudioManager.Instance?.PlayButtonClick(); });
            trigger.triggers.Add(entry);
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        internal void InitializeFromGameData(PlinkoGameData gameData, double initialBalance)
        {
            if (riskDropdown != null && gameData.risks != null)
            {
                riskDropdown.ClearOptions();
                List<string> riskNames = new List<string>();
                foreach (RiskLevel risk in gameData.risks)
                {
                    riskNames.Add(risk.name);
                }
                riskDropdown.AddOptions(riskNames);
                riskDropdown.value = 0;
            }

            if (rowDropdown != null && gameData.rows != null)
            {
                rowDropdown.ClearOptions();
                List<string> rowOptions = new List<string>();
                foreach (PlinkoRow row in gameData.rows)
                {
                    rowOptions.Add($"{row.id}");
                }
                rowDropdown.AddOptions(rowOptions);
                rowDropdown.value = 0;
            }

            UpdateBalance(initialBalance);

            if (gameData.bets != null && gameData.bets.Count > 0)
            {
                UpdateBetDisplay(gameData.bets[0]);
            }

            if (stopAutoplayImage != null)
            {
                stopAutoplayImage.gameObject.SetActive(false);
            }
        }

        internal void UpdateBalance(double balance)
        {
            if (balanceText != null)
            {
                balanceText.text = balance.ToString("F2");
            }
        }

        internal void UpdateBetDisplay(double betAmount)
        {
            if (betAmountText != null)
            {
                betAmountText.text = betAmount.ToString("F2");
            }
        }

        internal void UpdateBetButtonState(bool canBet)
        {
            if (betButton != null)
            {
                betButton.interactable = canBet;
            }

            if (betDisabledOverlay != null)
            {
                betDisabledOverlay.SetActive(!canBet);
            }
        }

        // ============================================
        // MODE MANAGEMENT
        // ============================================

        private void SetMode(bool manualMode)
        {
            isManualMode = manualMode;

            if (manualModeImage != null)
                manualModeImage.SetActive(manualMode);

            if (betButton != null)
                betButton.gameObject.SetActive(manualMode);

            if (autoplayModeImage != null)
                autoplayModeImage.SetActive(!manualMode);

            if (autoplayPanel != null)
                autoplayPanel.SetActive(!manualMode);

            if (betButton != null)
            {
                TextMeshProUGUI buttonText = betButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = manualMode ? "BET" : "START AUTOPLAY";
                }
            }
        }

        // ============================================
        // AUTOPLAY MANAGEMENT
        // ============================================

        private void OnAutoplayToggleClicked()
        {
            if (isAutoplayActive)
            {
                gameManager?.StopAutoplay();
            }
            else
            {
                if (autoplayRounds > 0 || autoplayRounds == 0)
                {
                    gameManager?.StartAutoplay(autoplayRounds);
                }
            }
        }

        private void ChangeAutoplayRounds(int delta)
        {
            if (isAutoplayActive) return;

            autoplayRounds += delta;

            if (autoplayRounds < 0)
            {
                autoplayRounds = 0;
                if (infinityIcon != null)
                    infinityIcon.SetActive(true);
            }
            else if (autoplayRounds > 100)
                autoplayRounds = 100;

            UpdateAutoplayRoundsDisplay();
        }

        private void UpdateAutoplayRoundsDisplay()
        {
            if (autoplayRoundsInput != null)
            {
                if (autoplayRounds == 0)
                {
                    autoplayRoundsInput.gameObject.SetActive(true);
                    autoplayRoundsInput.text = "0";
                }
                else
                {
                    autoplayRoundsInput.gameObject.SetActive(true);
                    autoplayRoundsInput.text = autoplayRounds.ToString();
                    if (infinityIcon != null)
                        infinityIcon.SetActive(false);
                }
            }
        }

        internal void UpdateAutoplayRounds(int rounds)
        {
            if (autoplayRoundsInput != null)
            {
                autoplayRoundsInput.gameObject.SetActive(true);
                autoplayRoundsInput.text = rounds.ToString();

                if (infinityIcon != null)
                {
                    infinityIcon.SetActive(false);
                }
            }
        }

        internal void OnAutoplayStarted(bool isInfinite)
        {
            isAutoplayActive = true;

            if (autoplayToggleButton != null)
            {
                TextMeshProUGUI buttonText = autoplayToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "STOP AUTOPLAY";
                }
            }

            if (stopAutoplayImage != null)
            {
                stopAutoplayImage.gameObject.SetActive(true);
            }

            if (autoplayRoundsInput != null)
            {
                if (isInfinite)
                {
                    autoplayRoundsInput.gameObject.SetActive(false);
                    if (infinityIcon != null)
                        infinityIcon.SetActive(true);
                }
                else
                {
                    autoplayRoundsInput.gameObject.SetActive(true);
                    autoplayRoundsInput.text = autoplayRounds.ToString();
                    if (infinityIcon != null)
                        infinityIcon.SetActive(false);
                }
            }

            SetControlsInteractable(false);
        }

        internal void OnAutoplayStopped()
        {
            isAutoplayActive = false;

            if (autoplayToggleButton != null)
            {
                TextMeshProUGUI buttonText = autoplayToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "START AUTOPLAY";
                }
            }

            if (stopAutoplayImage != null)
            {
                stopAutoplayImage.gameObject.SetActive(false);
            }

            SetControlsInteractable(true);
            UpdateAutoplayRoundsDisplay();
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (riskDropdown != null)
                riskDropdown.interactable = interactable;

            if (rowDropdown != null)
                rowDropdown.interactable = interactable;

            if (increaseBetButton != null)
                increaseBetButton.interactable = interactable;

            if (decreaseBetButton != null)
                decreaseBetButton.interactable = interactable;

            if (increaseRoundsButton != null)
                increaseRoundsButton.interactable = interactable;

            if (decreaseRoundsButton != null)
                decreaseRoundsButton.interactable = interactable;

            if (manualModeButton != null)
                manualModeButton.interactable = interactable;

            if (autoplayModeButton != null)
                autoplayModeButton.interactable = interactable;

            if (riskDisabledOverlay != null)
                riskDisabledOverlay.SetActive(!interactable);

            if (rowDisabledOverlay != null)
                rowDisabledOverlay.SetActive(!interactable);

            if (autoplayDisabledOverlay != null)
                autoplayDisabledOverlay.SetActive(!interactable);
        }

        // ============================================
        // DROPDOWN HANDLERS
        // ============================================

        private void OnRiskChanged(int value)
        {
            AudioManager.Instance?.PlayDropdownClick();
            gameManager?.OnRiskChanged(value);
        }

        private void OnRowChanged(int value)
        {
            AudioManager.Instance?.PlayDropdownClick();
            gameManager?.OnRowChanged(value);
        }

        // ============================================
        // HISTORY
        // ============================================

        internal void AddToHistory(double multiplier, double winAmount, int catcherIndex)
        {
            if (historyManager != null)
            {
                historyManager.AddHistoryEntry(multiplier, catcherIndex);
            }
        }

        // ============================================
        // POPUPS
        // ============================================

        private void HideAllPopups()
        {
            if (winPopupMainPanel != null)
                winPopupMainPanel.SetActive(false);

            if (errorPopupMainPanel != null)
                errorPopupMainPanel.SetActive(false);

            if (reconnectionPopupMainPanel != null)
                reconnectionPopupMainPanel.SetActive(false);

            if (disconnectionPopupMainPanel != null)
                disconnectionPopupMainPanel.SetActive(false);

            if (exitConfirmPopupMainPanel != null)
                exitConfirmPopupMainPanel.SetActive(false);

            if(gameInfoPopupMainPanel != null)
                gameInfoPopupMainPanel.SetActive(false);

            HideHoverPopup();
        }

        internal void CheckAndClosePopups()
        {
            if (reconnectionPopupMainPanel != null && reconnectionPopupMainPanel.activeSelf)
            {
                ClosePopupWithAnimation(reconnectionPopupMainPanel, reconnectionPopupArea);
            }
        }

        internal void ShowWinPopup(double winAmount, double multiplier)
        {
            if (multiplier <= 1.0) return;

            AudioManager.Instance?.PlayWinSound();

            if (winPopupMainPanel != null)
                winPopupMainPanel.SetActive(true);

            if (winPopupArea != null)
            {
                if (winAmountText != null)
                    winAmountText.text = winAmount.ToString("F2");

                if (winMultiplierText != null)
                    winMultiplierText.text = $"{multiplier:F1}x";

                ShowPopupWithAnimation(winPopupArea);
                StartCoroutine(AutoClosePopup(winPopupMainPanel, winPopupArea, 1.0f));
            }
        }

        internal void ShowErrorPopup(string message)
        {
            AudioManager.Instance?.PlayErrorSound();

            if (errorPopupMainPanel != null)
                errorPopupMainPanel.SetActive(true);

            if (errorPopupArea != null)
            {
                if (errorMessageText != null)
                    errorMessageText.text = message;

                ShowPopupWithAnimation(errorPopupArea);
            }
        }

        internal void ShowReconnectionPopup()
        {
            AudioManager.Instance?.PlayErrorSound();

            if (reconnectionPopupMainPanel != null)
                reconnectionPopupMainPanel.SetActive(true);

            if (reconnectionPopupArea != null)
            {
                ShowPopupWithAnimation(reconnectionPopupArea);
            }
        }

        internal void ShowDisconnectionPopup()
        {
            HideAllPopups();

            // ✅ AUDIO: Play error sound for disconnection
            AudioManager.Instance?.PlayErrorSound();

            if (disconnectionPopupMainPanel != null)
                disconnectionPopupMainPanel.SetActive(true);

            if (disconnectionPopupArea != null)
            {
                ShowPopupWithAnimation(disconnectionPopupArea);
            }
        }

        internal void ShowAnotherDevicePopup()
        {
            ShowErrorPopup("Account logged in from another device");
        }

        private void ShowExitConfirmPopup()
        {
         
            AudioManager.Instance?.PlayErrorSound();

            if (exitConfirmPopupMainPanel != null)
                exitConfirmPopupMainPanel.SetActive(true);

            if (exitConfirmPopupArea != null)
            {
                ShowPopupWithAnimation(exitConfirmPopupArea);
            }
        }
        private void ShowInfoPopup()
        {
            if (gameInfoPopupMainPanel != null)
                gameInfoPopupMainPanel.SetActive(true);

        }
        private void ShowPopupWithAnimation(GameObject popupArea)
        {
            if (popupArea == null) return;

            RectTransform rectTransform = popupArea.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            rectTransform.DOKill();
            rectTransform.localScale = Vector3.zero;
            popupArea.SetActive(true);

            rectTransform.DOScale(Vector3.one, popupScaleInDuration)
                .SetEase(popupScaleInEase)
                .SetUpdate(true);
        }

        private void ClosePopupWithAnimation(GameObject popupMainPanel, GameObject popupArea)
        {
            if (popupArea == null || popupMainPanel == null) return;

            RectTransform rectTransform = popupArea.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                popupMainPanel.SetActive(false);
                return;
            }

            rectTransform.DOKill();
            rectTransform.DOScale(Vector3.zero, popupScaleOutDuration)
                .SetEase(popupScaleOutEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    popupMainPanel.SetActive(false);
                    rectTransform.localScale = Vector3.one;
                });
        }

        private System.Collections.IEnumerator AutoClosePopup(GameObject popupMainPanel, GameObject popupArea, float delay)
        {
            yield return new WaitForSeconds(delay);
            ClosePopupWithAnimation(popupMainPanel, popupArea);
        }

        private void OnExitConfirmYes()
        {
            ClosePopupWithAnimation(exitConfirmPopupMainPanel, exitConfirmPopupArea);
            gameManager?.OnExitGame();
        }

        private void OnExitConfirmNo()
        {
            ClosePopupWithAnimation(exitConfirmPopupMainPanel, exitConfirmPopupArea);
        }
        private void OnInfoClose()
        {
            if(gameInfoPopupMainPanel != null  && gameInfoPopupMainPanel.activeSelf)
                gameInfoPopupMainPanel.SetActive(false);
        }
        private void OnDisconnectionOk()
        {
            ClosePopupWithAnimation(disconnectionPopupMainPanel, disconnectionPopupArea);
            gameManager?.OnExitGame();
        }

        private void OnErrorOK()
        {
            ClosePopupWithAnimation(errorPopupMainPanel, errorPopupArea);
        }

        // ============================================
        // HOVER POPUP
        // ============================================

        internal void ShowHoverPopup(Vector3 catcherPosition, double profit, double probability)
        {
            if (hoverPopup == null) return;

            currentHoverPosition = catcherPosition;
            currentHoverProfit = profit;
            currentHoverProbability = probability;

            hoverPopup.SetActive(true);
            UpdateHoverTexts();

            if (hoverArrow != null)
            {
                hoverArrow.position = new Vector3(catcherPosition.x, hoverArrow.position.y, hoverArrow.position.z);
            }

            isHoverPopupVisible = true;

            if (hoverCloseButton != null)
            {
                hoverCloseButton.gameObject.SetActive(IsMobilePlatform());
            }
        }

        internal void HideHoverPopup()
        {
            if (hoverPopup != null)
            {
                hoverPopup.SetActive(false);
            }
            isHoverPopupVisible = false;
        }

        internal void ToggleHoverPopup(Vector3 catcherPosition, double profit, double probability)
        {
            if (isHoverPopupVisible)
            {
                HideHoverPopup();
            }
            else
            {
                ShowHoverPopup(catcherPosition, profit, probability);
            }
        }

        internal void RefreshHoverData(double currentBet)
        {
            if (isHoverPopupVisible && gameManager != null)
            {
                UpdateHoverTexts();
            }
        }

        private void UpdateHoverTexts()
        {
            if (hoverProfitText != null)
            {
                string profitSign = currentHoverProfit >= 0 ? "+" : "";
                hoverProfitText.text = $"{profitSign}{currentHoverProfit:F2}";

                if (currentHoverProfit > 0)
                {
                    hoverProfitText.color = Color.green;
                }
                else if (currentHoverProfit < 0)
                {
                    hoverProfitText.color = Color.red;
                }
                else
                {
                    hoverProfitText.color = Color.white;
                }
            }

            if (hoverProbabilityText != null)
            {
                hoverProbabilityText.text = $"{currentHoverProbability:F2}%";
            }
        }

        private bool IsMobilePlatform()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            return Input.touchSupported;
#else
            return false;
#endif
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            betButton?.onClick.RemoveAllListeners();
            increaseBetButton?.onClick.RemoveAllListeners();
            decreaseBetButton?.onClick.RemoveAllListeners();
            autoplayToggleButton?.onClick.RemoveAllListeners();
            forceCloseGameButton?.onClick.RemoveAllListeners();
            exitYesButton?.onClick.RemoveAllListeners();
            exitNoButton?.onClick.RemoveAllListeners();
            disconnectionOkButton?.onClick.RemoveAllListeners();
            hoverCloseButton?.onClick.RemoveAllListeners();
            riskDropdown?.onValueChanged.RemoveAllListeners();
            rowDropdown?.onValueChanged.RemoveAllListeners();
            errorPopupbtn?.onClick.RemoveAllListeners();
            increaseRoundsButton?.onClick.RemoveAllListeners();
            decreaseRoundsButton?.onClick.RemoveAllListeners();
            musicToggle?.onValueChanged.RemoveAllListeners();
            sfxToggle?.onValueChanged.RemoveAllListeners();
        }
    }
}