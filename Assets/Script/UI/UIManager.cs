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
    /// NOW HANDLES ORIENTATION SWITCHING - Horizontal vs Vertical layouts
    /// Controls which layout is active based on screen dimensions
    /// FIXED: Autoplay counter system and hover popup dual-mode support
    /// FIXED: Dropdown interactability synced with overlay state
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ============================================
        // LAYOUT SYSTEM
        // ============================================

        [Header("Layout Control")]
        [SerializeField] private GameObject horizontalLayout;
        [SerializeField] private GameObject verticalLayout;

        private bool isHorizontalActive = true;

        // ============================================
        // HORIZONTAL LAYOUT REFERENCES
        // ============================================

        [Header("Horizontal - Betting UI")]
        [SerializeField] private TextMeshProUGUI h_balanceText;
        [SerializeField] private TextMeshProUGUI h_betAmountText;
        [SerializeField] private Button h_betButton;
        [SerializeField] private Button h_increaseBetButton;
        [SerializeField] private Button h_decreaseBetButton;
        [SerializeField] private GameObject h_betDisabledOverlay;

        [Header("Horizontal - Risk Selection")]
        [SerializeField] private TMP_Dropdown h_riskDropdown;
        [SerializeField] private GameObject h_riskDisabledOverlay;

        [Header("Horizontal - Row Selection")]
        [SerializeField] private TMP_Dropdown h_rowDropdown;
        [SerializeField] private GameObject h_rowDisabledOverlay;

        [Header("Horizontal - Mode Toggle")]
        [SerializeField] private Button h_manualModeButton;
        [SerializeField] private Button h_autoplayModeButton;
        [SerializeField] private GameObject h_manualModeImage;
        [SerializeField] private GameObject h_autoplayModeImage;

        [Header("Horizontal - Autoplay Controls")]
        [SerializeField] private GameObject h_autoplayPanel;
        [SerializeField] private TMP_InputField h_autoplayRoundsInput;
        [SerializeField] private Button h_autoplayToggleButton;
        [SerializeField] private Image h_stopAutoplayImage;
        [SerializeField] private Button h_increaseRoundsButton;
        [SerializeField] private Button h_decreaseRoundsButton;
        [SerializeField] private GameObject h_infinityIcon;
        [SerializeField] private GameObject h_autoplayDisabledOverlay;

        [Header("Horizontal - Audio Toggles")]
        [SerializeField] private Toggle h_musicToggle;
        [SerializeField] private Toggle h_sfxToggle;

        [Header("Horizontal - Game Info")]
        [SerializeField] private Button h_gameInfoButton;
        [SerializeField] private Button h_gameInfoCloseButton;

        [Header("Horizontal - Exit")]
        [SerializeField] private Button h_forceCloseGameButton;

        [Header("Horizontal - History")]
        [SerializeField] private Transform h_historyContainer;
        [SerializeField] private TextMeshProUGUI h_historyEmptyText;

        [Header("Horizontal - Hover Popup")]
        [SerializeField] private GameObject h_hoverPopup;
        [SerializeField] private TextMeshProUGUI h_hoverProfitText;
        [SerializeField] private TextMeshProUGUI h_hoverProbabilityText;
        [SerializeField] private RectTransform h_hoverArrow;
        [SerializeField] private Button h_hoverCloseButton;
        [SerializeField] private RectTransform h_hoverPopupRect;

        // ============================================
        // VERTICAL LAYOUT REFERENCES
        // ============================================

        [Header("Vertical - Betting UI")]
        [SerializeField] private TextMeshProUGUI v_balanceText;
        [SerializeField] private TextMeshProUGUI v_betAmountText;
        [SerializeField] private Button v_betButton;
        [SerializeField] private Button v_increaseBetButton;
        [SerializeField] private Button v_decreaseBetButton;
        [SerializeField] private GameObject v_betDisabledOverlay;

        [Header("Vertical - Risk Selection")]
        [SerializeField] private TMP_Dropdown v_riskDropdown;
        [SerializeField] private GameObject v_riskDisabledOverlay;

        [Header("Vertical - Row Selection")]
        [SerializeField] private TMP_Dropdown v_rowDropdown;
        [SerializeField] private GameObject v_rowDisabledOverlay;

        [Header("Vertical - Mode Toggle")]
        [SerializeField] private Button v_manualModeButton;
        [SerializeField] private Button v_autoplayModeButton;
        [SerializeField] private GameObject v_manualModeImage;
        [SerializeField] private GameObject v_autoplayModeImage;

        [Header("Vertical - Autoplay Controls")]
        [SerializeField] private GameObject v_autoplayPanel;
        [SerializeField] private TMP_InputField v_autoplayRoundsInput;
        [SerializeField] private Button v_autoplayToggleButton;
        [SerializeField] private Image v_stopAutoplayImage;
        [SerializeField] private Button v_increaseRoundsButton;
        [SerializeField] private Button v_decreaseRoundsButton;
        [SerializeField] private GameObject v_infinityIcon;
        [SerializeField] private GameObject v_autoplayDisabledOverlay;

        [Header("Vertical - Audio Toggles")]
        [SerializeField] private Toggle v_musicToggle;
        [SerializeField] private Toggle v_sfxToggle;

        [Header("Vertical - Game Info")]
        [SerializeField] private Button v_gameInfoButton;
        [SerializeField] private Button v_gameInfoCloseButton;

        [Header("Vertical - Exit")]
        [SerializeField] private Button v_forceCloseGameButton;

        [Header("Vertical - History")]
        [SerializeField] private Transform v_historyContainer;
        [SerializeField] private TextMeshProUGUI v_historyEmptyText;

        [Header("Vertical - Hover Popup")]
        [SerializeField] private GameObject v_hoverPopup;
        [SerializeField] private TextMeshProUGUI v_hoverProfitText;
        [SerializeField] private TextMeshProUGUI v_hoverProbabilityText;
        [SerializeField] private RectTransform v_hoverArrow;
        [SerializeField] private Button v_hoverCloseButton;
        [SerializeField] private RectTransform v_hoverPopupRect;

        // ============================================
        // SHARED POPUPS (Same for both layouts)
        // ============================================

        [Header("Shared Popups")]
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

        [Header("Shared Popup Buttons")]
        [SerializeField] private Button exitYesButton;
        [SerializeField] private Button exitNoButton;
        [SerializeField] private Button disconnectionOkButton;

        [Header("Popup Animation Settings")]
        [SerializeField] private float popupScaleInDuration = 0.3f;
        [SerializeField] private float popupScaleOutDuration = 0.2f;
        [SerializeField] private Ease popupScaleInEase = Ease.OutBack;
        [SerializeField] private Ease popupScaleOutEase = Ease.InBack;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private HistoryManager historyManager;

        // ============================================
        // POPUP X POSITION STORAGE
        // ============================================

        private Dictionary<RectTransform, float> storedHorizontalXPositions = new Dictionary<RectTransform, float>();

        // State
        private bool isManualMode = true;
        private bool isAutoplayActive;
        private int autoplayRounds = 0; // Start with 0
        private int currentAutoplayCount = 0; // Track current countdown
        private bool isInfiniteMode = true; // Start in infinite mode
        private bool isHoverPopupVisible = false;
        private Vector3 currentHoverPosition;
        private double currentHoverProfit;
        private double currentHoverProbability;

        private void Awake()
        {
            SetupButtons();
            SetupDropdowns();
            SetupAudioToggles();
            SetupInputFieldListeners();
            HideAllPopups();
            StoreInitialPopupXPositions();
        }

        private void Start()
        {
            SetMode(true);
            UpdateAutoplayDisplay(); // Initialize display
            InitializeAudioToggles();

            // Start with horizontal layout by default
            SwitchToLayout(true);
        }

        // ============================================
        // INPUT FIELD SETUP
        // ============================================

        private void SetupInputFieldListeners()
        {
            // Horizontal
            if (h_autoplayRoundsInput != null)
            {
                h_autoplayRoundsInput.onValueChanged.AddListener(OnAutoplayInputChanged);
                h_autoplayRoundsInput.onEndEdit.AddListener(OnAutoplayInputEndEdit);
            }

            // Vertical
            if (v_autoplayRoundsInput != null)
            {
                v_autoplayRoundsInput.onValueChanged.AddListener(OnAutoplayInputChanged);
                v_autoplayRoundsInput.onEndEdit.AddListener(OnAutoplayInputEndEdit);
            }
        }

        private void OnAutoplayInputChanged(string value)
        {
            // Prevent user from typing infinity symbol
            if (value.Contains("∞"))
            {
                UpdateAutoplayDisplay();
            }
        }

        private void OnAutoplayInputEndEdit(string value)
        {
            // Parse user input
            if (int.TryParse(value, out int rounds))
            {
                // Clamp between 0 and 999
                autoplayRounds = Mathf.Clamp(rounds, 0, 999);
                isInfiniteMode = (autoplayRounds == 0);
            }
            else
            {
                // Invalid input, reset
                autoplayRounds = 0;
                isInfiniteMode = true;
            }

            UpdateAutoplayDisplay();

            // Sync other layout
            SyncAutoplayInputs();
        }

        // ============================================
        // POPUP X POSITION MANAGEMENT
        // ============================================

        private void StoreInitialPopupXPositions()
        {
            StorePopupXPosition(winPopupArea);
            StorePopupXPosition(errorPopupArea);
            StorePopupXPosition(reconnectionPopupArea);
            StorePopupXPosition(disconnectionPopupArea);
            StorePopupXPosition(exitConfirmPopupArea);
            StorePopupXPosition(gameInfoPopupArea);

            Debug.Log($"[UIManager] Stored {storedHorizontalXPositions.Count} popup X positions");
        }

        private void StorePopupXPosition(GameObject popupArea)
        {
            if (popupArea != null)
            {
                RectTransform rect = popupArea.GetComponent<RectTransform>();
                if (rect != null && !storedHorizontalXPositions.ContainsKey(rect))
                {
                    storedHorizontalXPositions[rect] = rect.localPosition.x;
                }
            }
        }

        private void UpdateAllPopupXPositions()
        {
            foreach (var kvp in storedHorizontalXPositions)
            {
                RectTransform rect = kvp.Key;
                float horizontalX = kvp.Value;

                if (rect != null)
                {
                    Vector3 pos = rect.localPosition;
                    pos.x = isHorizontalActive ? horizontalX : 0f;
                    rect.localPosition = pos;
                }
            }
        }

        // ============================================
        // ORIENTATION & LAYOUT CONTROL
        // ============================================

        public void OnOrientationChanged(int width, int height)
        {
            bool shouldBeHorizontal = width > height;

            if (shouldBeHorizontal != isHorizontalActive)
            {
                SwitchToLayout(shouldBeHorizontal);

                if (gameManager != null)
                {
                    gameManager.OnLayoutSwitched(shouldBeHorizontal);
                }
            }
        }

        private void SwitchToLayout(bool horizontal)
        {
            isHorizontalActive = horizontal;

            if (horizontalLayout != null)
                horizontalLayout.SetActive(horizontal);

            if (verticalLayout != null)
                verticalLayout.SetActive(!horizontal);

            UpdateAllPopupXPositions();

            if (historyManager != null)
            {
                Transform activeHistoryContainer = horizontal ? h_historyContainer : v_historyContainer;
                TextMeshProUGUI activeEmptyText = horizontal ? h_historyEmptyText : v_historyEmptyText;
                historyManager.BindToLayout(isHorizontalActive); // Pass true/false directly
            }

            if (isHoverPopupVisible)
            {
                ShowHoverPopup(currentHoverPosition, currentHoverProfit, currentHoverProbability);
            }

            Debug.Log($"[UIManager] Switched to {(horizontal ? "HORIZONTAL" : "VERTICAL")} layout");
        }

        public bool IsHorizontalLayout()
        {
            return isHorizontalActive;
        }

        // ============================================
        // AUDIO TOGGLE SETUP
        // ============================================

        private void SetupAudioToggles()
        {
            if (h_musicToggle != null)
            {
                h_musicToggle.onValueChanged.RemoveAllListeners();
                h_musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            }
            if (h_sfxToggle != null)
            {
                h_sfxToggle.onValueChanged.RemoveAllListeners();
                h_sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
            }

            if (v_musicToggle != null)
            {
                v_musicToggle.onValueChanged.RemoveAllListeners();
                v_musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            }
            if (v_sfxToggle != null)
            {
                v_sfxToggle.onValueChanged.RemoveAllListeners();
                v_sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
            }
        }

        private void InitializeAudioToggles()
        {
            if (AudioManager.Instance != null)
            {
                bool musicEnabled = AudioManager.Instance.IsMusicEnabled();
                bool sfxEnabled = AudioManager.Instance.IsSFXEnabled();

                if (h_musicToggle != null)
                    h_musicToggle.SetIsOnWithoutNotify(musicEnabled);
                if (v_musicToggle != null)
                    v_musicToggle.SetIsOnWithoutNotify(musicEnabled);

                if (h_sfxToggle != null)
                    h_sfxToggle.SetIsOnWithoutNotify(sfxEnabled);
                if (v_sfxToggle != null)
                    v_sfxToggle.SetIsOnWithoutNotify(sfxEnabled);
            }
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            AudioManager.Instance?.ToggleMusic(isOn);
            AudioManager.Instance?.PlayButtonClick();

            if (isHorizontalActive && v_musicToggle != null)
                v_musicToggle.SetIsOnWithoutNotify(isOn);
            else if (!isHorizontalActive && h_musicToggle != null)
                h_musicToggle.SetIsOnWithoutNotify(isOn);
        }

        private void OnSFXToggleChanged(bool isOn)
        {
            AudioManager.Instance?.ToggleSFX(isOn);
            if (isOn)
            {
                AudioManager.Instance?.PlayButtonClick();
            }

            if (isHorizontalActive && v_sfxToggle != null)
                v_sfxToggle.SetIsOnWithoutNotify(isOn);
            else if (!isHorizontalActive && h_sfxToggle != null)
                h_sfxToggle.SetIsOnWithoutNotify(isOn);
        }

        // ============================================
        // BUTTON SETUP (Both Layouts)
        // ============================================

        private void SetupButtons()
        {
            // Horizontal layout
            if (h_betButton != null)
                h_betButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayStartButtonClick();
                    gameManager?.OnBetButtonClicked();
                });

            if (h_increaseBetButton != null)
                h_increaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    gameManager?.OnBetChanged(true);
                });

            if (h_decreaseBetButton != null)
                h_decreaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    gameManager?.OnBetChanged(false);
                });

            if (h_manualModeButton != null)
                h_manualModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(true);
                });

            if (h_autoplayModeButton != null)
                h_autoplayModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(false);
                });

            if (h_autoplayToggleButton != null)
                h_autoplayToggleButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnAutoplayToggle();
                });

            if (h_increaseRoundsButton != null)
                h_increaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    AdjustAutoplayRounds(true);
                });

            if (h_decreaseRoundsButton != null)
                h_decreaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    AdjustAutoplayRounds(false);
                });

            if (h_forceCloseGameButton != null)
                h_forceCloseGameButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowExitConfirmPopup();
                });

            if (h_gameInfoButton != null)
                h_gameInfoButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowInfoPopup();
                });

            if (h_gameInfoCloseButton != null)
                h_gameInfoCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnInfoClose();
                });

            // Vertical layout (same callbacks)
            if (v_betButton != null)
                v_betButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayStartButtonClick();
                    gameManager?.OnBetButtonClicked();
                });

            if (v_increaseBetButton != null)
                v_increaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    gameManager?.OnBetChanged(true);
                });

            if (v_decreaseBetButton != null)
                v_decreaseBetButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    gameManager?.OnBetChanged(false);
                });

            if (v_manualModeButton != null)
                v_manualModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(true);
                });

            if (v_autoplayModeButton != null)
                v_autoplayModeButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    SetMode(false);
                });

            if (v_autoplayToggleButton != null)
                v_autoplayToggleButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnAutoplayToggle();
                });

            if (v_increaseRoundsButton != null)
                v_increaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayIncreaseBet();
                    AdjustAutoplayRounds(true);
                });

            if (v_decreaseRoundsButton != null)
                v_decreaseRoundsButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayDecreaseBet();
                    AdjustAutoplayRounds(false);
                });

            if (v_forceCloseGameButton != null)
                v_forceCloseGameButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowExitConfirmPopup();
                });

            if (v_gameInfoButton != null)
                v_gameInfoButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    ShowInfoPopup();
                });

            if (v_gameInfoCloseButton != null)
                v_gameInfoCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnInfoClose();
                });

            // Shared popup buttons
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

            if (errorPopupbtn != null)
                errorPopupbtn.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    OnErrorOK();
                });

            // Hover popup close buttons
            if (h_hoverCloseButton != null)
                h_hoverCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    HideHoverPopup();
                });

            if (v_hoverCloseButton != null)
                v_hoverCloseButton.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayButtonClick();
                    HideHoverPopup();
                });
        }

        // ============================================
        // DROPDOWN SETUP (Both Layouts)
        // ============================================

        private void SetupDropdowns()
        {
            if (h_riskDropdown != null)
            {
                h_riskDropdown.onValueChanged.AddListener((index) => {
                    AudioManager.Instance?.PlayDropdownClick();
                    gameManager?.OnRiskChanged(index);

                    if (v_riskDropdown != null)
                        v_riskDropdown.SetValueWithoutNotify(index);
                });
            }

            if (h_rowDropdown != null)
            {
                h_rowDropdown.onValueChanged.AddListener((index) => {
                    AudioManager.Instance?.PlayDropdownClick();
                    gameManager?.OnRowChanged(index);

                    if (v_rowDropdown != null)
                        v_rowDropdown.SetValueWithoutNotify(index);
                });
            }

            if (v_riskDropdown != null)
            {
                v_riskDropdown.onValueChanged.AddListener((index) => {
                    AudioManager.Instance?.PlayDropdownClick();
                    gameManager?.OnRiskChanged(index);

                    if (h_riskDropdown != null)
                        h_riskDropdown.SetValueWithoutNotify(index);
                });
            }

            if (v_rowDropdown != null)
            {
                v_rowDropdown.onValueChanged.AddListener((index) => {
                    AudioManager.Instance?.PlayDropdownClick();
                    gameManager?.OnRowChanged(index);

                    if (h_rowDropdown != null)
                        h_rowDropdown.SetValueWithoutNotify(index);
                });
            }
        }

        // ============================================
        // UI INITIALIZATION FROM GAME DATA
        // ============================================

        internal void InitializeFromGameData(PlinkoGameData gameData, double initialBalance)
        {
            InitializeRiskDropdowns(gameData.risks);
            InitializeRowDropdowns(gameData.rows);
            UpdateBalance(initialBalance);
            UpdateBetDisplay(gameData.bets[0]);
        }

        private void InitializeRiskDropdowns(List<RiskLevel> risks)
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var risk in risks)
            {
                options.Add(new TMP_Dropdown.OptionData(risk.name));
            }

            if (h_riskDropdown != null)
            {
                h_riskDropdown.ClearOptions();
                h_riskDropdown.AddOptions(options);
                h_riskDropdown.value = 0;
            }

            if (v_riskDropdown != null)
            {
                v_riskDropdown.ClearOptions();
                v_riskDropdown.AddOptions(options);
                v_riskDropdown.value = 0;
            }
        }

        private void InitializeRowDropdowns(List<PlinkoRow> rows)
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var row in rows)
            {
                options.Add(new TMP_Dropdown.OptionData(row.id));
            }

            if (h_rowDropdown != null)
            {
                h_rowDropdown.ClearOptions();
                h_rowDropdown.AddOptions(options);
                h_rowDropdown.value = 0;
            }

            if (v_rowDropdown != null)
            {
                v_rowDropdown.ClearOptions();
                v_rowDropdown.AddOptions(options);
                v_rowDropdown.value = 0;
            }
        }

        // ============================================
        // UI UPDATES (Both Layouts)
        // ============================================

        internal void UpdateBalance(double balance)
        {
            string formatted = balance.ToString("F2");
            if (h_balanceText != null)
                h_balanceText.text = formatted;
            if (v_balanceText != null)
                v_balanceText.text = formatted;
        }

        internal void UpdateBetDisplay(double betAmount)
        {
            string formatted = betAmount.ToString("F2");
            if (h_betAmountText != null)
                h_betAmountText.text = formatted;
            if (v_betAmountText != null)
                v_betAmountText.text = formatted;
        }

        /// <summary>
        /// FIXED: Overlay activates during autoplay to prevent bet changes
        /// In manual mode, overlay never shows (users can always adjust bet amounts)
        /// </summary>
        internal void UpdateBetButtonState(bool canBet, bool isAutoplayActive)
        {
            if (h_betButton != null)
                h_betButton.interactable = canBet;
            if (v_betButton != null)
                v_betButton.interactable = canBet;

            // Show overlay during autoplay to prevent bet amount changes
            // Never show in manual mode (users can always adjust bet amounts)
            bool showOverlay = isAutoplayActive;

            if (h_betDisabledOverlay != null)
                h_betDisabledOverlay.SetActive(showOverlay);
            if (v_betDisabledOverlay != null)
                v_betDisabledOverlay.SetActive(showOverlay);
        }

        /// <summary>
        /// Updates risk and row overlays based on active ball count
        /// Prevents players from changing risk/row while balls are dropping
        /// FIXED: Now also controls dropdown interactability
        /// </summary>
        internal void UpdateRiskRowOverlays(bool hasBallsActive)
        {
            // Show overlays when balls are active to prevent path changes
            if (h_riskDisabledOverlay != null)
                h_riskDisabledOverlay.SetActive(hasBallsActive);
            if (v_riskDisabledOverlay != null)
                v_riskDisabledOverlay.SetActive(hasBallsActive);

            if (h_rowDisabledOverlay != null)
                h_rowDisabledOverlay.SetActive(hasBallsActive);
            if (v_rowDisabledOverlay != null)
                v_rowDisabledOverlay.SetActive(hasBallsActive);

            // FIXED: Control dropdown interactability based on overlay state
            // Dropdowns are non-interactable when overlays are active
            if (h_riskDropdown != null)
                h_riskDropdown.interactable = !hasBallsActive;
            if (v_riskDropdown != null)
                v_riskDropdown.interactable = !hasBallsActive;

            if (h_rowDropdown != null)
                h_rowDropdown.interactable = !hasBallsActive;
            if (v_rowDropdown != null)
                v_rowDropdown.interactable = !hasBallsActive;
        }

        /// <summary>
        /// Updates mode button interactability based on ball dropping state
        /// Prevents switching between Manual/Auto modes while balls are dropping
        /// FIXED: Separate control from autoplay state - buttons disabled only when balls are physically dropping
        /// </summary>
        internal void UpdateModeButtonsState(bool hasBallsDropping)
        {
            // Disable mode buttons when balls are physically dropping
            // This prevents mode switching mid-round
            if (h_manualModeButton != null)
                h_manualModeButton.interactable = !hasBallsDropping;
            if (h_autoplayModeButton != null)
                h_autoplayModeButton.interactable = !hasBallsDropping;

            if (v_manualModeButton != null)
                v_manualModeButton.interactable = !hasBallsDropping;
            if (v_autoplayModeButton != null)
                v_autoplayModeButton.interactable = !hasBallsDropping;
        }

        /// <summary>
        /// ORIENTATION CHANGE: Lock all controls during orientation change
        /// Prevents user from changing settings while layout is transitioning
        /// Autoplay continues running but new interactions are blocked
        /// </summary>
        internal void LockControlsDuringOrientationChange(bool locked)
        {
            Debug.Log($"[UIManager] ========== LOCKING CONTROLS: {locked} ==========");

            // STEP 1: Lock risk/row dropdowns with overlays
            Debug.Log($"[UIManager] Setting risk/row overlays: {locked}");

            // CRITICAL FIX: When unlocking, check if autoplay is active - if so, keep these locked
            bool shouldLockRiskRow = locked || isAutoplayActive;

            // Show/hide overlays
            if (h_riskDisabledOverlay != null)
            {
                h_riskDisabledOverlay.SetActive(locked);
                Debug.Log($"[UIManager] Horizontal risk overlay: {shouldLockRiskRow}");
            }
            if (v_riskDisabledOverlay != null)
            {
                v_riskDisabledOverlay.SetActive(shouldLockRiskRow);
                Debug.Log($"[UIManager] Vertical risk overlay: {shouldLockRiskRow}");
            }
            if (h_rowDisabledOverlay != null)
            {
                h_rowDisabledOverlay.SetActive(shouldLockRiskRow);
                Debug.Log($"[UIManager] Horizontal row overlay: {shouldLockRiskRow}");
            }
            if (v_rowDisabledOverlay != null)
            {
                v_rowDisabledOverlay.SetActive(shouldLockRiskRow);
                Debug.Log($"[UIManager] Vertical row overlay: {shouldLockRiskRow}");
            }

            // Make dropdowns non-interactable
            if (h_riskDropdown != null)
            {
                h_riskDropdown.interactable = !shouldLockRiskRow;
                Debug.Log($"[UIManager] Horizontal risk dropdown interactable: {!shouldLockRiskRow}");
            }
            if (v_riskDropdown != null)
            {
                v_riskDropdown.interactable = !shouldLockRiskRow;
                Debug.Log($"[UIManager] Vertical risk dropdown interactable: {!shouldLockRiskRow}");
            }
            if (h_rowDropdown != null)
            {
                h_rowDropdown.interactable = !shouldLockRiskRow;
                Debug.Log($"[UIManager] Horizontal row dropdown interactable: {!shouldLockRiskRow}");
            }
            if (v_rowDropdown != null)
            {
                v_rowDropdown.interactable = !shouldLockRiskRow;
                Debug.Log($"[UIManager] Vertical row dropdown interactable: {!shouldLockRiskRow}");
            }

            // STEP 2: Lock mode buttons
            Debug.Log($"[UIManager] Setting mode buttons interactable: {!locked}");
            if (h_manualModeButton != null)
                h_manualModeButton.interactable = !locked;
            if (h_autoplayModeButton != null)
                h_autoplayModeButton.interactable = !locked;
            if (v_manualModeButton != null)
                v_manualModeButton.interactable = !locked;
            if (v_autoplayModeButton != null)
                v_autoplayModeButton.interactable = !locked;

            // STEP 3: Lock bet buttons (manual mode)
            // CRITICAL FIX: When unlocking, check if autoplay is active
            bool shouldLockBet = locked || isAutoplayActive;

            Debug.Log($"[UIManager] Setting bet buttons interactable: {!shouldLockBet}");
            if (h_betButton != null)
                h_betButton.interactable = !shouldLockBet;
            if (v_betButton != null)
                v_betButton.interactable = !shouldLockBet;

            // Show bet overlay for visual feedback during orientation
            if (h_betDisabledOverlay != null)
            {
                h_betDisabledOverlay.SetActive(shouldLockBet);
                Debug.Log($"[UIManager] Horizontal bet overlay: {shouldLockBet}");
            }
            if (v_betDisabledOverlay != null)
            {
                v_betDisabledOverlay.SetActive(shouldLockBet);
                Debug.Log($"[UIManager] Vertical bet overlay: {shouldLockBet}");
            }

            // STEP 4: Lock bet increase/decrease buttons
            if (h_increaseBetButton != null)
                h_increaseBetButton.interactable = !locked;
            if (h_decreaseBetButton != null)
                h_decreaseBetButton.interactable = !locked;
            if (v_increaseBetButton != null)
                v_increaseBetButton.interactable = !locked;
            if (v_decreaseBetButton != null)
                v_decreaseBetButton.interactable = !locked;

            // CRITICAL FIX: When unlocking, check if autoplay is active
            bool shouldLockAutoplayRounds = locked || isAutoplayActive;

            // STEP 5: Lock autoplay rounds adjustment (but NOT autoplay toggle)
            Debug.Log($"[UIManager] Setting autoplay round buttons interactable: {!shouldLockAutoplayRounds}");
            if (h_increaseRoundsButton != null)
                h_increaseRoundsButton.interactable = !shouldLockAutoplayRounds;
            if (h_decreaseRoundsButton != null)
                h_decreaseRoundsButton.interactable = !shouldLockAutoplayRounds;
            if (v_increaseRoundsButton != null)
                v_increaseRoundsButton.interactable = !shouldLockAutoplayRounds;
            if (v_decreaseRoundsButton != null)
                v_decreaseRoundsButton.interactable = !shouldLockAutoplayRounds;
            // CRITICAL FIX: Keep overlay active if autoplay is running
            bool shouldShowAutoplayOverlay = locked || isAutoplayActive;


            // STEP 6: Show autoplay overlay during orientation (optional - only if autoplay is active)
            // This provides visual feedback that controls are locked
            if (h_autoplayDisabledOverlay != null)
            {
                // Only show if we're locking AND not already in autoplay mode
                // Using shouldShowAutoplayOverlay defined above
                h_autoplayDisabledOverlay.SetActive(shouldShowAutoplayOverlay);
                Debug.Log($"[UIManager] Horizontal autoplay overlay: {shouldShowAutoplayOverlay}");
            }
            if (v_autoplayDisabledOverlay != null)
            {
                // Using shouldShowAutoplayOverlay defined above
                v_autoplayDisabledOverlay.SetActive(shouldShowAutoplayOverlay);
                Debug.Log($"[UIManager] Vertical autoplay overlay: {shouldShowAutoplayOverlay}");
            }

            Debug.Log($"[UIManager] ========== CONTROLS LOCK COMPLETE: {locked} ==========");
        }

        // ============================================
        // MODE SWITCHING (Manual/Autoplay)
        // ============================================

        private void SetMode(bool manual)
        {
            isManualMode = manual;

            // Horizontal
            if (h_manualModeImage != null)
                h_manualModeImage.SetActive(manual);
            if (h_betButton != null)
                h_betButton.gameObject.SetActive(manual);
            if (h_autoplayModeImage != null)
                h_autoplayModeImage.SetActive(!manual);
            if (h_autoplayPanel != null)
                h_autoplayPanel.SetActive(!manual);

            // Vertical
            if (v_manualModeImage != null)
                v_manualModeImage.SetActive(manual);
            if (v_betButton != null)
                v_betButton.gameObject.SetActive(manual);
            if (v_autoplayModeImage != null)
                v_autoplayModeImage.SetActive(!manual);
            if (v_autoplayPanel != null)
                v_autoplayPanel.SetActive(!manual);

            UpdateControlStates();
        }

        private void UpdateControlStates()
        {
            bool controlsEnabled = !isAutoplayActive;

            // Horizontal
            if (h_riskDisabledOverlay != null)
                h_riskDisabledOverlay.SetActive(!controlsEnabled);
            if (h_rowDisabledOverlay != null)
                h_rowDisabledOverlay.SetActive(!controlsEnabled);

            // FIXED: Control dropdown interactability in UpdateControlStates too
            if (h_riskDropdown != null)
                h_riskDropdown.interactable = controlsEnabled;
            if (h_rowDropdown != null)
                h_rowDropdown.interactable = controlsEnabled;

            // Vertical
            if (v_riskDisabledOverlay != null)
                v_riskDisabledOverlay.SetActive(!controlsEnabled);
            if (v_rowDisabledOverlay != null)
                v_rowDisabledOverlay.SetActive(!controlsEnabled);

            // FIXED: Control dropdown interactability in UpdateControlStates too
            if (v_riskDropdown != null)
                v_riskDropdown.interactable = controlsEnabled;
            if (v_rowDropdown != null)
                v_rowDropdown.interactable = controlsEnabled;
        }

        // ============================================
        // AUTOPLAY CONTROLS - FIXED VERSION
        // ============================================

        private void OnAutoplayToggle()
        {
            if (isAutoplayActive)
            {
                gameManager?.StopAutoplay();
            }
            else
            {
                // Pass the rounds (0 = infinite)
                gameManager?.StartAutoplay(autoplayRounds);
            }
        }

        internal void OnAutoplayStarted(bool isInfinite)
        {
            isAutoplayActive = true;
            isInfiniteMode = isInfinite;

            // Set current count
            if (isInfinite)
            {
                currentAutoplayCount = 0; // Infinite mode
            }
            else
            {
                currentAutoplayCount = autoplayRounds;
            }

            // Horizontal
            if (h_stopAutoplayImage != null)
                h_stopAutoplayImage.gameObject.SetActive(true);
            if (h_infinityIcon != null)
                h_infinityIcon.SetActive(isInfinite);
            if (h_autoplayRoundsInput != null)
                h_autoplayRoundsInput.interactable = false;
            if (h_autoplayDisabledOverlay != null)
                h_autoplayDisabledOverlay.SetActive(true);

            // Vertical
            if (v_stopAutoplayImage != null)
                v_stopAutoplayImage.gameObject.SetActive(true);
            if (v_infinityIcon != null)
                v_infinityIcon.SetActive(isInfinite);
            if (v_autoplayRoundsInput != null)
                v_autoplayRoundsInput.interactable = false;
            if (v_autoplayDisabledOverlay != null)

                // Disable increase/decrease round buttons during autoplay
                if (h_increaseRoundsButton != null)
                    h_increaseRoundsButton.interactable = false;
            if (h_decreaseRoundsButton != null)
                h_decreaseRoundsButton.interactable = false;
            if (v_increaseRoundsButton != null)
                v_increaseRoundsButton.interactable = false;
            if (v_decreaseRoundsButton != null)
                v_decreaseRoundsButton.interactable = false;
            v_autoplayDisabledOverlay.SetActive(true);

            UpdateAutoplayDisplay();
            UpdateControlStates();
        }

        internal void OnAutoplayStopped()
        {

            // FIXED: Reset autoplayRounds to 0 (show infinity mode)
            autoplayRounds = 0;
            isInfiniteMode = true;
            currentAutoplayCount = 0;
            isAutoplayActive = false;

            // Horizontal
            if (h_stopAutoplayImage != null)
                h_stopAutoplayImage.gameObject.SetActive(false);
            if (h_autoplayRoundsInput != null)
                h_autoplayRoundsInput.interactable = true;
            if (h_autoplayDisabledOverlay != null)
                h_autoplayDisabledOverlay.SetActive(false);
            // FIXED: Turn off bet overlay when autoplay stops
            if (h_betDisabledOverlay != null)
                h_betDisabledOverlay.SetActive(false);


            // Re-enable increase/decrease round buttons when autoplay stops
            if (h_increaseRoundsButton != null)
                h_increaseRoundsButton.interactable = true;
            if (h_decreaseRoundsButton != null)
                h_decreaseRoundsButton.interactable = true;
            if (v_increaseRoundsButton != null)
                v_increaseRoundsButton.interactable = true;
            if (v_decreaseRoundsButton != null)
                v_decreaseRoundsButton.interactable = true;
            // Vertical
            if (v_stopAutoplayImage != null)
                v_stopAutoplayImage.gameObject.SetActive(false);
            if (v_autoplayRoundsInput != null)
                v_autoplayRoundsInput.interactable = true;
            if (v_autoplayDisabledOverlay != null)
                v_autoplayDisabledOverlay.SetActive(false);
            // FIXED: Turn off bet overlay when autoplay stops
            if (v_betDisabledOverlay != null)
                v_betDisabledOverlay.SetActive(false);

            UpdateControlStates();
            UpdateAutoplayDisplay();
        }

        /// <summary>
        /// Called by GameManager after each bet completes during autoplay
        /// </summary>
        internal void UpdateAutoplayRounds(int remainingRounds)
        {
            if (isAutoplayActive && !isInfiniteMode)
            {
                currentAutoplayCount = remainingRounds;
                UpdateAutoplayDisplay();
            }
        }

        private void AdjustAutoplayRounds(bool increase)
        {
            if (increase)
            {
                if (autoplayRounds == 0)
                {
                    autoplayRounds = 1;
                    isInfiniteMode = false;
                }
                else if (autoplayRounds < 999)
                {
                    autoplayRounds = Mathf.Min(autoplayRounds + 1, 999);
                    isInfiniteMode = false;
                }
            }
            else
            {
                autoplayRounds = Mathf.Max(autoplayRounds - 1, 0);
                isInfiniteMode = (autoplayRounds == 0);
            }

            UpdateAutoplayDisplay();
            SyncAutoplayInputs();
        }

        /// <summary>
        /// Updates the visual display based on current state
        /// </summary>
        private void UpdateAutoplayDisplay()
        {
            string displayText;
            bool showInfinity;

            if (isAutoplayActive)
            {
                // During autoplay: show countdown or infinity
                if (isInfiniteMode)
                {
                    displayText = "0";
                    showInfinity = true;
                }
                else
                {
                    displayText = currentAutoplayCount.ToString();
                    showInfinity = false;
                }
            }
            else
            {
                // Before autoplay: show set value or 0
                if (isInfiniteMode || autoplayRounds == 0)
                {
                    displayText = "0";
                    showInfinity = true;
                }
                else
                {
                    displayText = autoplayRounds.ToString();
                    showInfinity = false;
                }
            }

            // Update both layouts
            if (h_autoplayRoundsInput != null)
                h_autoplayRoundsInput.text = displayText;
            if (v_autoplayRoundsInput != null)
                v_autoplayRoundsInput.text = displayText;

            if (h_infinityIcon != null)
                h_infinityIcon.SetActive(showInfinity);
            if (v_infinityIcon != null)
                v_infinityIcon.SetActive(showInfinity);
        }

        private void SyncAutoplayInputs()
        {
            // Sync both input fields
            string displayText = isInfiniteMode ? "0" : autoplayRounds.ToString();

            if (h_autoplayRoundsInput != null)
                h_autoplayRoundsInput.text = displayText;
            if (v_autoplayRoundsInput != null)
                v_autoplayRoundsInput.text = displayText;
        }

        // ============================================
        // HISTORY MANAGEMENT
        // ============================================

        internal void AddToHistory(double multiplier, double winAmount, int catcherIndex)
        {
            historyManager?.AddToHistory(multiplier, winAmount, catcherIndex);
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
            if (gameInfoPopupMainPanel != null)
                gameInfoPopupMainPanel.SetActive(false);

            if (h_hoverPopup != null)
                h_hoverPopup.SetActive(false);
            if (v_hoverPopup != null)
                v_hoverPopup.SetActive(false);
        }

        internal void ShowWinPopup(double winAmount, double multiplier)
        {
            AudioManager.Instance?.PlayWinSound();

            if (winAmountText != null)
                winAmountText.text = winAmount.ToString("F2");
            if (winMultiplierText != null)
                winMultiplierText.text = multiplier.ToString("F2") + "x";

            if (winPopupMainPanel != null)
                winPopupMainPanel.SetActive(true);

            if (winPopupArea != null)
            {
                ShowPopupWithAnimation(winPopupArea);
                StartCoroutine(AutoClosePopup(winPopupMainPanel, winPopupArea, 2f));
            }
        }

        internal void ShowErrorPopup(string message)
        {
            AudioManager.Instance?.PlayErrorSound();

            if (errorMessageText != null)
                errorMessageText.text = message;

            if (errorPopupMainPanel != null)
                errorPopupMainPanel.SetActive(true);

            if (errorPopupArea != null)
            {
                ShowPopupWithAnimation(errorPopupArea);
            }
        }

        internal void ShowReconnectionPopup()
        {
            if (reconnectionPopupMainPanel != null)
                reconnectionPopupMainPanel.SetActive(true);

            if (reconnectionPopupArea != null)
            {
                ShowPopupWithAnimation(reconnectionPopupArea);
            }
        }

        internal void HideReconnectionPopup()
        {
            ClosePopupWithAnimation(reconnectionPopupMainPanel, reconnectionPopupArea);
        }

        internal void ShowDisconnectionPopup()
        {
            if (disconnectionPopupMainPanel != null)
                disconnectionPopupMainPanel.SetActive(true);

            if (disconnectionPopupArea != null)
            {
                ShowPopupWithAnimation(disconnectionPopupArea);
            }
        }

        private void ShowExitConfirmPopup()
        {
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

        private void ClosePopupWithAnimation(GameObject mainPanel, GameObject area)
        {
            if (mainPanel != null && area != null)
            {
                RectTransform areaRect = area.GetComponent<RectTransform>();
                if (areaRect != null)
                {
                    areaRect.DOScale(Vector3.zero, popupScaleOutDuration)
                        .SetEase(popupScaleOutEase)
                        .OnComplete(() =>
                        {
                            mainPanel.SetActive(false);

                            // FIXED: Reset bet area state after popup closes
                            ResetBetAreaAfterPopup();
                        });
                }
                else
                {
                    mainPanel.SetActive(false);
                    ResetBetAreaAfterPopup(); // FIXED: Also reset here
                }
            }
        }

        private void ResetBetAreaAfterPopup()
        {
            // Re-enable bet controls if they should be enabled
            if (gameManager != null)
            {
                // Trigger a state update to ensure button state is correct
                UpdateBetButtonState(gameManager.CanBetNow(), isAutoplayActive);
            }
            // The overlay state is now handled by UpdateBetButtonState based on autoplay status
            // No need to manually set overlay here
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
            if (gameInfoPopupMainPanel != null && gameInfoPopupMainPanel.activeSelf)
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

        internal void CheckAndClosePopups()
        {
            if (reconnectionPopupMainPanel != null && reconnectionPopupMainPanel.activeSelf)
            {
                ClosePopupWithAnimation(reconnectionPopupMainPanel, reconnectionPopupArea);
            }
        }

        internal void ShowAnotherDevicePopup()
        {
            ShowErrorPopup("Account logged in from another device");
        }

        // ============================================
        // HOVER POPUP (DUAL MODE SUPPORT)
        // ============================================

        internal void ShowHoverPopup(Vector3 catcherPosition, double profit, double probability)
        {
            currentHoverPosition = catcherPosition;
            currentHoverProfit = profit;
            currentHoverProbability = probability;

            GameObject activeHoverPopup = isHorizontalActive ? h_hoverPopup : v_hoverPopup;
            RectTransform activeHoverArrow = isHorizontalActive ? h_hoverArrow : v_hoverArrow;
            Button activeHoverCloseButton = isHorizontalActive ? h_hoverCloseButton : v_hoverCloseButton;

            GameObject inactiveHoverPopup = isHorizontalActive ? v_hoverPopup : h_hoverPopup;
            if (inactiveHoverPopup != null)
            {
                inactiveHoverPopup.SetActive(false);
            }

            if (activeHoverPopup == null) return;

            activeHoverPopup.SetActive(true);
            UpdateHoverTexts();

            if (activeHoverArrow != null)
            {
                Vector3 arrowPos = activeHoverArrow.position;
                arrowPos.x = catcherPosition.x;
                activeHoverArrow.position = arrowPos;
            }

            isHoverPopupVisible = true;

            if (activeHoverCloseButton != null)
            {
                activeHoverCloseButton.gameObject.SetActive(IsMobilePlatform());
            }
        }

        internal void HideHoverPopup()
        {
            if (h_hoverPopup != null)
            {
                h_hoverPopup.SetActive(false);
            }
            if (v_hoverPopup != null)
            {
                v_hoverPopup.SetActive(false);
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
            TextMeshProUGUI activeProfitText = isHorizontalActive ? h_hoverProfitText : v_hoverProfitText;
            TextMeshProUGUI activeProbabilityText = isHorizontalActive ? h_hoverProbabilityText : v_hoverProbabilityText;

            if (activeProfitText != null)
            {
                string profitSign = currentHoverProfit >= 0 ? "+" : "";
                activeProfitText.text = $"{profitSign}{currentHoverProfit:F2}";

                if (currentHoverProfit > 0)
                {
                    activeProfitText.color = Color.green;
                }
                else if (currentHoverProfit < 0)
                {
                    activeProfitText.color = Color.red;
                }
                else
                {
                    activeProfitText.color = Color.white;
                }
            }

            if (activeProbabilityText != null)
            {
                // Convert to percentage and show exact value
                double percentage = currentHoverProbability * 100.0;

                // Format to remove trailing zeros but keep precision
                string percentageText = percentage.ToString("0.#################");

                activeProbabilityText.text = percentageText;

                // Note: TextMeshPro text already has "%" symbol in the UI,
                // so we don't add it here
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
            // Horizontal
            h_betButton?.onClick.RemoveAllListeners();
            h_increaseBetButton?.onClick.RemoveAllListeners();
            h_decreaseBetButton?.onClick.RemoveAllListeners();
            h_autoplayToggleButton?.onClick.RemoveAllListeners();
            h_forceCloseGameButton?.onClick.RemoveAllListeners();
            h_riskDropdown?.onValueChanged.RemoveAllListeners();
            h_rowDropdown?.onValueChanged.RemoveAllListeners();
            h_increaseRoundsButton?.onClick.RemoveAllListeners();
            h_decreaseRoundsButton?.onClick.RemoveAllListeners();
            h_musicToggle?.onValueChanged.RemoveAllListeners();
            h_sfxToggle?.onValueChanged.RemoveAllListeners();
            h_gameInfoButton?.onClick.RemoveAllListeners();
            h_gameInfoCloseButton?.onClick.RemoveAllListeners();
            h_hoverCloseButton?.onClick.RemoveAllListeners();
            h_autoplayRoundsInput?.onValueChanged.RemoveAllListeners();
            h_autoplayRoundsInput?.onEndEdit.RemoveAllListeners();

            // Vertical
            v_betButton?.onClick.RemoveAllListeners();
            v_increaseBetButton?.onClick.RemoveAllListeners();
            v_decreaseBetButton?.onClick.RemoveAllListeners();
            v_autoplayToggleButton?.onClick.RemoveAllListeners();
            v_forceCloseGameButton?.onClick.RemoveAllListeners();
            v_riskDropdown?.onValueChanged.RemoveAllListeners();
            v_rowDropdown?.onValueChanged.RemoveAllListeners();
            v_increaseRoundsButton?.onClick.RemoveAllListeners();
            v_decreaseRoundsButton?.onClick.RemoveAllListeners();
            v_musicToggle?.onValueChanged.RemoveAllListeners();
            v_sfxToggle?.onValueChanged.RemoveAllListeners();
            v_gameInfoButton?.onClick.RemoveAllListeners();
            v_gameInfoCloseButton?.onClick.RemoveAllListeners();
            v_hoverCloseButton?.onClick.RemoveAllListeners();
            v_autoplayRoundsInput?.onValueChanged.RemoveAllListeners();
            v_autoplayRoundsInput?.onEndEdit.RemoveAllListeners();

            // Shared
            exitYesButton?.onClick.RemoveAllListeners();
            exitNoButton?.onClick.RemoveAllListeners();
            disconnectionOkButton?.onClick.RemoveAllListeners();
            errorPopupbtn?.onClick.RemoveAllListeners();
        }


        /// <summary>
        /// Updates risk dropdown to show specific value (without triggering events)
        /// Used during orientation change to reflect enforced settings
        /// </summary>
        internal void UpdateRiskDropdownValue(int riskIndex)
        {
            if (isHorizontalActive && h_riskDropdown != null)
            {
                h_riskDropdown.SetValueWithoutNotify(riskIndex);
            }
            else if (!isHorizontalActive && v_riskDropdown != null)
            {
                v_riskDropdown.SetValueWithoutNotify(riskIndex);
            }
        }

        /// <summary>
        /// Updates row dropdown to show specific value (without triggering events)
        /// Used during orientation change to reflect enforced settings
        /// </summary>
        internal void UpdateRowDropdownValue(int rowIndex)
        {
            if (isHorizontalActive && h_rowDropdown != null)
            {
                h_rowDropdown.SetValueWithoutNotify(rowIndex);
            }
            else if (!isHorizontalActive && v_rowDropdown != null)
            {
                v_rowDropdown.SetValueWithoutNotify(rowIndex);
            }
        }


        /// <summary>
        /// CRITICAL: Syncs autoplay state between layouts after orientation change
        /// Ensures autoplay counter, overlays, and stop button are properly shown in new layout
        /// </summary>
        internal void SyncAutoplayStateAfterOrientationChange()
        {
            if (!isAutoplayActive)
            {
                // Not in autoplay, nothing to sync
                return;
            }

            Debug.Log($"[UIManager] Syncing autoplay state after orientation change - Active: {isAutoplayActive}, Count: {currentAutoplayCount}, Infinite: {isInfiniteMode}");

            // Determine which layout is now active
            TMP_InputField activeInput = isHorizontalActive ? h_autoplayRoundsInput : v_autoplayRoundsInput;
            Image activeStopImage = isHorizontalActive ? h_stopAutoplayImage : v_stopAutoplayImage;
            GameObject activeInfinityIcon = isHorizontalActive ? h_infinityIcon : v_infinityIcon;
            GameObject activeAutoplayOverlay = isHorizontalActive ? h_autoplayDisabledOverlay : v_autoplayDisabledOverlay;

            // Update autoplay counter display
            if (activeInput != null)
            {
                activeInput.interactable = false;
                if (isInfiniteMode)
                {
                    activeInput.text = "0";
                }
                else
                {
                    activeInput.text = currentAutoplayCount.ToString();
                }
                Debug.Log($"[UIManager] Synced autoplay input: {activeInput.text}");
            }

            // Show stop button in new layout
            if (activeStopImage != null)
            {
                activeStopImage.gameObject.SetActive(true);
                Debug.Log($"[UIManager] Enabled stop autoplay button");
            }

            // Show/hide infinity icon
            if (activeInfinityIcon != null)
            {
                activeInfinityIcon.SetActive(isInfiniteMode);
                Debug.Log($"[UIManager] Infinity icon: {isInfiniteMode}");
            }

            // Enable autoplay overlay in new layout
            if (activeAutoplayOverlay != null)
            {
                activeAutoplayOverlay.SetActive(true);
                Debug.Log($"[UIManager] Enabled autoplay overlay");
            }

            // Disable increase/decrease round buttons during autoplay
            if (h_increaseRoundsButton != null)
                h_increaseRoundsButton.interactable = false;
            if (h_decreaseRoundsButton != null)
                h_decreaseRoundsButton.interactable = false;
            if (v_increaseRoundsButton != null)
                v_increaseRoundsButton.interactable = false;
            if (v_decreaseRoundsButton != null)
                v_decreaseRoundsButton.interactable = false;
            Debug.Log($"[UIManager] Disabled round adjustment buttons");

            // Also sync the inactive layout to keep both in sync
            TMP_InputField inactiveInput = isHorizontalActive ? v_autoplayRoundsInput : h_autoplayRoundsInput;
            Image inactiveStopImage = isHorizontalActive ? v_stopAutoplayImage : h_stopAutoplayImage;
            GameObject inactiveInfinityIcon = isHorizontalActive ? v_infinityIcon : h_infinityIcon;
            GameObject inactiveAutoplayOverlay = isHorizontalActive ? v_autoplayDisabledOverlay : h_autoplayDisabledOverlay;

            if (inactiveInput != null)
            {
                inactiveInput.interactable = false;
                inactiveInput.text = activeInput != null ? activeInput.text : (isInfiniteMode ? "0" : currentAutoplayCount.ToString());
            }
            if (inactiveStopImage != null)
            {
                inactiveStopImage.gameObject.SetActive(true);
            }
            if (inactiveInfinityIcon != null)
            {
                inactiveInfinityIcon.SetActive(isInfiniteMode);
            }
            if (inactiveAutoplayOverlay != null)
            {
                inactiveAutoplayOverlay.SetActive(true);
            }

            Debug.Log($"[UIManager] Autoplay state sync complete");
        }

    }


}