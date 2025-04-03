using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject twelveGamePanel;
    [SerializeField]
    private GameObject sixteenGamePanel;
    [SerializeField]
    private List<Marbles> pooledMarbles = new List<Marbles>();
    [SerializeField]
    private List<Marbles> pooledMarblesSixteen = new List<Marbles>();
    [SerializeField]
    int marblesToSpawn = 2;
    [SerializeField]
    private List<int> containersToFill = new List<int>();
    [SerializeField]
    private List<int> autoBetBallCount = new List<int>();
    [SerializeField]
    private List<Transform> Containers = new List<Transform>();
    [SerializeField]
    private List<Transform> ContainersSixteen = new List<Transform>();
    [Header("Buttons")]
    [SerializeField]
    private Button TBetPlus_Button;
    [SerializeField]
    private Button TBetMinus_Button;

    [SerializeField]
    private Button TBallPlus_Button;
    [SerializeField]
    private Button TBallMinus_Button;

    [SerializeField]
    private Button sendBet_Button;
    [SerializeField]
    private Button autoBet_Button;
    private double currentTotalBet = 0;
    private double currentBalance;
    internal int BetCounter;
    internal int BallCounter;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text balance_text;
    [Header("Buttons")]
    [SerializeField]
     TMP_Dropdown riskDropDown;
    [SerializeField]
    TMP_Dropdown rowDropDown;
    [SerializeField]
    TMP_InputField autoBetField;
    [SerializeField]
    List<ContainerClass> containers = new List<ContainerClass>();
    [SerializeField]
    List<ContainerClass> containersSixteen = new List<ContainerClass>();
    [SerializeField]
    private Transform mainBoard;
    [SerializeField]
    private Transform mainBoardSixteen;
    [SerializeField]
    GameObject marblePrefab,marblePrefabSixteen;
    private int marbleSpawnCount;
    internal int autoBetTotalCount,autoBetCurrentCount;
    [SerializeField]
    internal float autoBetFrequency = 0.5f;
    bool isAutoBetPlaying;
    bool autoBetInstanceDone;
    internal int totalMarbleInAction;
    Coroutine autoBetCoroutine;
    [SerializeField]
    AudioManager audioManager;
    [SerializeField]
    SocketIOManager socketIoManager;  
    [SerializeField]
    internal UiManager uiManager;
    internal gameType _gameType = gameType.TWELVE;
    public enum marbleType
    {
        RED,
        YELLOW,
        GREEN
    }

    public enum gameType
    {
        TWELVE,
        SIXTEEN
    }

    private void Start()
    {
        if (TBetPlus_Button) TBetPlus_Button.onClick.RemoveAllListeners();
        if (TBetPlus_Button) TBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });

        if (TBetMinus_Button) TBetMinus_Button.onClick.RemoveAllListeners();
        if (TBetMinus_Button) TBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (TBallPlus_Button) TBallPlus_Button.onClick.RemoveAllListeners();
        if (TBallPlus_Button) TBallPlus_Button.onClick.AddListener(delegate { changeBallCount(true); });

        if (TBallMinus_Button) TBallMinus_Button.onClick.RemoveAllListeners();
        if (TBallMinus_Button) TBallMinus_Button.onClick.AddListener(delegate { changeBallCount(false); });

        if (sendBet_Button) sendBet_Button.onClick.RemoveAllListeners();
        if (sendBet_Button) sendBet_Button.onClick.AddListener(delegate { sendBetData(); });

        if (autoBet_Button) autoBet_Button.onClick.RemoveAllListeners();
        if (autoBet_Button) autoBet_Button.onClick.AddListener(delegate { sendBetData(); uiManager.StopAutoBet.gameObject.SetActive(true); });

        if (uiManager.StopAutoBet) uiManager.StopAutoBet.onClick.RemoveAllListeners();
        if (uiManager.StopAutoBet) uiManager.StopAutoBet.onClick.AddListener(delegate { stopAutoBet(); });

        if (riskDropDown)riskDropDown.onValueChanged.AddListener(delegate { changeRiskFactor(); });
        if(rowDropDown)rowDropDown.onValueChanged.AddListener(delegate {changeGameMode();});
        if (autoBetField) autoBetField.onValueChanged.AddListener(delegate { autoBetInput(); });
    }


    private void changeGameMode()
    {
        if(rowDropDown.value == 0)
        {
            _gameType = gameType.TWELVE;
            twelveGamePanel.SetActive(true);
            sixteenGamePanel.SetActive(false);
        }
        else
        {
            twelveGamePanel.SetActive(false);
            sixteenGamePanel.SetActive(true);
            _gameType = gameType.SIXTEEN;
        }
        changeRiskFactor();
    }


    internal void setInitialUI()
    {
        if (riskDropDown) riskDropDown.ClearOptions();
        riskDropDown.AddOptions(socketIoManager.initialData.risk);
        if (rowDropDown) rowDropDown.ClearOptions();
        rowDropDown.AddOptions(socketIoManager.initialData.rows);      
        currentBalance = socketIoManager.playerdata.Balance;
        balance_text.text = socketIoManager.playerdata.Balance.ToString("f3");
        currentTotalBet = socketIoManager.initialData.Bets[0];
        if (TotalBet_text) TotalBet_text.text = (socketIoManager.initialData.Bets[BetCounter]).ToString("f2");
        currentTotalBet = socketIoManager.initialData.Bets[BetCounter];
        
        changeRiskFactor();

    }


    internal void autoBetInput()
    {
        if (!isAutoBetPlaying)
        {
            int ballCount = int.Parse(autoBetField.text);
            if (ballCount > 100)
            {
                autoBetField.text = "";
            }
            else
            {
                autoBetTotalCount = ballCount;
            }
        }

    }


    private void sendBetData()
    {
        toggleUI(false);
        if (autoBetTotalCount == 0)
        {
            StartCoroutine(accumulateResult());
        }
        else
        {
            isAutoBetPlaying = true;
           
            autoBetCurrentCount = autoBetTotalCount;
            autoBetCoroutine =  StartCoroutine(startAutoBet());
        }

    }


    IEnumerator accumulateResult()
    {
        if (currentBalance < currentTotalBet)
        {
            lowBalance();
            yield break;
        }
        else
        {
            updateBalance(currentTotalBet, false);
            //socketIoManager.AccumulateResult(socketIoManager.initialData.Bets[BetCounter], rowDropDown.value, riskDropDown.value);
            socketIoManager.AccumulateResult(BetCounter, rowDropDown.value, riskDropDown.value);
            yield return new WaitUntil(() => socketIoManager.isResultdone);
            
            if (_gameType != gameType.TWELVE)
            {
                while (marbleSpawnCount < pooledMarblesSixteen.Count && pooledMarblesSixteen[marbleSpawnCount].gameObject.activeSelf)
                {
                    marbleSpawnCount++;
                }

                // If all marbles are active, create a new one
                if (marbleSpawnCount >= pooledMarblesSixteen.Count)
                {
                    GameObject marble = GameObject.Instantiate(marblePrefabSixteen, mainBoardSixteen);
                    pooledMarblesSixteen.Add(marble.GetComponent<Marbles>());
                }
                if (pooledMarblesSixteen[marbleSpawnCount].game_Manager == null)
                {

                    pooledMarblesSixteen[marbleSpawnCount].game_Manager = this;
                }
                pooledMarblesSixteen[marbleSpawnCount].winAmount = socketIoManager.playerdata.currentWining;
                pooledMarblesSixteen[marbleSpawnCount].gameObject.SetActive(true);
                pooledMarblesSixteen[marbleSpawnCount].transform.localPosition = new Vector2(Random.Range(-30f, 20f), 742);
                pooledMarblesSixteen[marbleSpawnCount].containerToFill = containersSixteen[socketIoManager.resultData.ballPosition].transform.position;
                pooledMarblesSixteen[marbleSpawnCount].containerNum = socketIoManager.resultData.ballPosition;
                pooledMarblesSixteen[marbleSpawnCount].multiplier = socketIoManager.resultData.selectedMultiplier;
                pooledMarblesSixteen[marbleSpawnCount].descentStart();
                totalMarbleInAction++;
                
                
               
                if (isAutoBetPlaying)
                {
                    autoBetCurrentCount--;
                    autoBetField.text = autoBetCurrentCount.ToString();
                    autoBetInstanceDone = true;
                }
                marbleSpawnCount = 0;
            }
            else
            {
                while (marbleSpawnCount < pooledMarbles.Count && pooledMarbles[marbleSpawnCount].gameObject.activeSelf)
                {
                    marbleSpawnCount++;
                }

                // If all marbles are active, create a new one
                if (marbleSpawnCount >= pooledMarbles.Count)
                {
                    GameObject marble = GameObject.Instantiate(marblePrefab, mainBoard);
                    pooledMarbles.Add(marble.GetComponent<Marbles>());
                }
               
                if (pooledMarbles[marbleSpawnCount].game_Manager == null)
                {
                   
                    pooledMarbles[marbleSpawnCount].game_Manager = this;
                }
                pooledMarbles[marbleSpawnCount].winAmount = socketIoManager.playerdata.currentWining;
                pooledMarbles[marbleSpawnCount].gameObject.SetActive(true);
                pooledMarbles[marbleSpawnCount].transform.localPosition = new Vector2(Random.Range(-17f, 57f), 742);
                pooledMarbles[marbleSpawnCount].containerToFill = Containers[socketIoManager.resultData.ballPosition].transform.position;
                pooledMarbles[marbleSpawnCount].containerNum = socketIoManager.resultData.ballPosition;
                pooledMarbles[marbleSpawnCount].multiplier = socketIoManager.resultData.selectedMultiplier;
                pooledMarbles[marbleSpawnCount].descentStart();
                totalMarbleInAction++; 
               
               
               
                if (isAutoBetPlaying)
                {
                    autoBetCurrentCount--;
                    autoBetField.text = autoBetCurrentCount.ToString();
                    autoBetInstanceDone = true;
                }
                marbleSpawnCount = 0;
            }
        }

    }

    void lowBalance()
    {
        toggleUI(true);
        if (isAutoBetPlaying)
        {
            StopCoroutine(autoBetCoroutine);
        }
    }

    internal void updateBalance(double amount,bool add)
    {
        if (add)
        {

            currentBalance += amount;
            balance_text.text = currentBalance.ToString("f3");
        }
        else
        {
            currentBalance -= amount;
            balance_text.text = currentBalance.ToString("f3");

        }
    }

    internal void checkForFallingMarbles()
    {
        if(totalMarbleInAction == 0)
        {
            toggleUI(true);
        }
    }

    private void changeRiskFactor()
    {
        string rowtext = rowDropDown.options[rowDropDown.value].text;
        Debug.Log(rowtext);
        switch (rowtext)
        {
            case "12":
                {
                    for (int i = 0; i < containers.Count; i++)
                    {
                        containers[i].riskText.text = socketIoManager.initialData.multiplier[0][riskDropDown.value][i].ToString();
                    }
                    break;
                }
            case "16":
                {
                    for (int i = 0; i < containersSixteen.Count; i++)
                    {
                        containersSixteen[i].riskText.text = socketIoManager.initialData.multiplier[1][riskDropDown.value][i].ToString();
                    }
                    break;
                }
        }
    }




    private void ChangeBet(bool IncDec)
    {
        Debug.Log("changeBetRan");
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter >= socketIoManager.initialData.Bets.Count)
            {
                BetCounter = 0;
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = socketIoManager.initialData.Bets.Count - 1;
            }
        }
        if (TotalBet_text) TotalBet_text.text = (socketIoManager.initialData.Bets[BetCounter]).ToString("f2");
        currentTotalBet = socketIoManager.initialData.Bets[BetCounter];

    }

    private void changeBallCount(bool IncDec)
    {
        Debug.Log("changeBetRan");
        if (IncDec)
        {
            BallCounter++;
            if (BallCounter >= autoBetBallCount.Count - 1)
            {
                BallCounter = 0;
            }
        }
        else
        {
            BallCounter--;
            if (BallCounter < 0)
            {
                BallCounter = autoBetBallCount.Count - 1;
            }
        }
        autoBetField.text = autoBetBallCount[BallCounter].ToString();

    }


    public void spawnMarbles()
    {
        if (marblesToSpawn <= pooledMarbles.Count)
        {
            for (int i = 0; i < marblesToSpawn; i++)
            {
                pooledMarbles[i].gameObject.SetActive(true);

                pooledMarbles[i].transform.localPosition = new Vector2(Random.Range(-17f, 67f), 742);
                pooledMarbles[i].containerToFill = Containers[containersToFill[i]].transform.position;
                pooledMarbles[i].containerNum = containersToFill[i];
                pooledMarbles[i].descentStart();
            }
        }
    
    }


    IEnumerator startAutoBet()
    {
       
        for (int i = 0; i < autoBetTotalCount; i++)
        {
            StartCoroutine(accumulateResult());
            yield return new WaitUntil(() => autoBetInstanceDone);
            yield return new WaitForSeconds(autoBetFrequency );
            autoBetInstanceDone = false;
        }
        isAutoBetPlaying = false;
        uiManager.StopAutoBet.gameObject.SetActive(false);
        autoBetField.text = autoBetTotalCount.ToString();

    }

    void stopAutoBet()
    {
        uiManager.StopAutoBet.gameObject.SetActive(false);
        StopCoroutine(autoBetCoroutine);
    }


    private void toggleUI(bool toggle)
    {
        rowDropDown.interactable = toggle;
        riskDropDown.interactable = toggle;
        TBetPlus_Button.interactable = toggle;
        TBetMinus_Button.interactable = toggle;
        TBallMinus_Button.interactable = toggle;
        TBallPlus_Button.interactable = toggle;
      
    }


}
