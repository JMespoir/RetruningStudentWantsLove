using UnityEngine;
using TMPro;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public int month = 3;
    public int day = 1;
    public int timeIndex = 0;
    public int weekIndex = 0;

    public int money = 0;
    public int stamina = 100;

    public int[] stats = new int[2];
    public int[] lovePoints = new int[3];
    public bool[] isDateLocked = new bool[5];
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("데이터")]
    public PlayerData data;

    private string[] timeList = { "오전", "오후" };
    private string[] weekList = { "월", "화", "수", "목", "금", "토", "일" };
    private int[] daysPerMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    [Header("UI 그룹 연결")]
    public GameObject startSceneGroup;
    public GameObject gameUIGroup;

    [Header("버튼 그룹 연결 (추가됨)")]
    public GameObject weekdayGroup; // 평일 버튼 뭉치 (알바/상점/학교/수업)
    public GameObject weekendGroup; // 주말 버튼 뭉치 (알바/데이트/자습/휴식)

    [Header("인게임 UI 연결")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI statEduText;  // 학력(파란색)
    public TextMeshProUGUI statLookText; // 외모(노란색)

    [Header("팝업 & 메뉴 UI 연결")]
    public GameObject popupGroup;
    public TextMeshProUGUI popupText;
    public GameObject systemMenuGroup;

    [Header("장소 선택 팝업 연결 (추가됨)")]
    public GameObject locationPopupGroup; // 학교 장소 선택창

    string savePath;
    private bool isSoundOn = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "save.json");
    }

    void Start()
    {
        ShowTitleScreen();
    }

    // ==========================================
    // 1. 화면 전환 및 타이틀 기능
    // ==========================================
    public void ShowTitleScreen()
    {
        if (startSceneGroup != null) startSceneGroup.SetActive(true);
        if (gameUIGroup != null) gameUIGroup.SetActive(false);
        if (systemMenuGroup != null) systemMenuGroup.SetActive(false);
        if (popupGroup != null) popupGroup.SetActive(false);

        // [추가] 장소 팝업도 끄기
        if (locationPopupGroup != null) locationPopupGroup.SetActive(false);
    }

    public void OnClickNewGame()
    {
        data = new PlayerData();

        data.month = 3;
        data.day = 1;
        data.weekIndex = 0;
        data.timeIndex = 0;
        data.stamina = 100;

        StartGamePlay();
    }

    public void OnClickContinue()
    {
        if (!File.Exists(savePath))
        {
            ShowPopup("저장된 파일이 없습니다.\n새 게임을 시작합니다.");
            OnClickNewGame();
            return;
        }

        string json = File.ReadAllText(savePath);
        data = JsonUtility.FromJson<PlayerData>(json);

        StartGamePlay();
    }

    public void OnClickExit()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    void StartGamePlay()
    {
        if (startSceneGroup != null) startSceneGroup.SetActive(false);
        if (gameUIGroup != null) gameUIGroup.SetActive(true);

        UpdateUI();
    }

    // ==========================================
    // 2. 인게임 행동 로직 (수정됨)
    // ==========================================

    // [학교] 버튼 클릭 시 -> 장소 선택 팝업 띄우기
    public void OnClickSchool()
    {
        // 1. 체력 검사
        if (data.stamina < 10)
        {
            ShowPopup("체력이 부족합니다.");
            return;
        }

        // 2. [수정] 바로 실행 안 하고 장소 팝업을 띄움
        if (locationPopupGroup != null)
        {
            locationPopupGroup.SetActive(true);
        }
        else
        {
            Debug.LogError("장소 선택 팝업(Location Popup Group)이 연결되지 않았습니다!");
        }
        PassTime();
        UpdateUI();
    }

    // [추가] 장소 버튼(도서관, 매점 등)을 눌렀을 때 실행되는 함수
    // 유니티 버튼 설정에서 인자(Library, Rooftop 등)를 적어줘야 함
    public void OnClickLocation(string placeName)
    {
        if (locationPopupGroup != null) locationPopupGroup.SetActive(false);
        data.stamina -= 10;


        string fileName = $"School_{placeName}_Event";
        Debug.Log($"[테스트] 대화 파일 로드 시도: {fileName}"); // 콘솔 확인용 로그
        DialogueManager.Instance.StartDialogue(fileName);
    }

    // [알바] 버튼
    public void OnClickPartTime()
    {
        if (data.stamina < 20)
        {
            ShowPopup("너무 힘들어서 일할 수 없습니다.");
            return;
        }

        data.stamina -= 20;
        data.money += 30000;

        PassTime();
        UpdateUI();

        ShowPopup("편의점 알바를 완료했다.\n30,000원을 벌었다!");
    }

    // ==========================================
    // 3. 시스템 기능
    // ==========================================

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        ShowPopup("게임이 안전하게\n저장되었습니다.");
    }

    public void OpenMenu()
    {
        systemMenuGroup.SetActive(true);
        Time.timeScale = 0;
    }

    public void CloseMenu()
    {
        systemMenuGroup.SetActive(false);
        Time.timeScale = 1;
    }

    public void OnClickGoToTitle()
    {
        CloseMenu();
        ShowTitleScreen();
    }

    public void OnClickSoundToggle()
    {
        isSoundOn = !isSoundOn;
        if (isSoundOn)
        {
            AudioListener.volume = 1;
            ShowPopup("소리 ON");
        }
        else
        {
            AudioListener.volume = 0;
            ShowPopup("소리 OFF");
        }
    }

    public void ShowPopup(string message)
    {
        if (popupGroup != null) popupGroup.SetActive(true);
        if (popupText != null) popupText.text = message;
    }

    public void ClosePopup()
    {
        if (popupGroup != null) popupGroup.SetActive(false);
    }

    // 시간 및 날짜 계산 로직
    void PassTime()
    {
        data.timeIndex++;
        if (data.timeIndex >= timeList.Length)
        {
            data.timeIndex = 0;
            NextDay();
        }
    }

    void NextDay()
    {
        data.day++;
        if (data.day > daysPerMonth[data.month])
        {
            data.day = 1;
            data.month++;
            if (data.month > 12) data.month = 1;
        }

        data.weekIndex++;

        // 일요일(6)을 넘어서 월요일(0)이 될 때!
        if (data.weekIndex >= weekList.Length)
        {
            data.weekIndex = 0;

            // ★ [추가] 월요일이 되면 데이트 기록 초기화 (모두 해제)
            for (int i = 0; i < data.isDateLocked.Length; i++)
            {
                data.isDateLocked[i] = false;
            }
            Debug.Log("새로운 주가 시작되어 데이트 기회가 초기화되었습니다.");
        }

        data.stamina = 100;
    }

    public void UpdateUI()
    {
        string dayStr = weekList[data.weekIndex];
        string timeStr = timeList[data.timeIndex];

        dateText.text = $"{data.month:D2}/{data.day:D2} ({dayStr})";
        timeText.text = $"{timeStr}";
        moneyText.text = $"{data.money} \\";
        staminaText.text = $"{data.stamina} %";
        statEduText.text = $"{data.stats[0]}";
        statLookText.text = $"{data.stats[1]}";
        // ==========================================
        // [추가됨] 평일 vs 주말 버튼 교체 로직
        // ==========================================
        // weekIndex: 5(토), 6(일) -> 주말
        if (data.weekIndex >= 5)
        {
            if (weekdayGroup != null) weekdayGroup.SetActive(false);
            if (weekendGroup != null) weekendGroup.SetActive(true);
        }
        else
        {
            // 평일
            if (weekdayGroup != null) weekdayGroup.SetActive(true);
            if (weekendGroup != null) weekendGroup.SetActive(false);
        }
    }
    [Header("데이트 팝업 연결 (추가됨)")]
    public GameObject datePopupGroup; // 데이트 대상 선택창
    public UnityEngine.UI.Button[] heroineButtons; // 팝업 안의 버튼 3개 (잠금 기능용)

    // ==========================================
    // [추가] 주말 데이트 시스템
    // ==========================================

    // 1. [데이트] 버튼 클릭 시 (주말 그룹에 있는 버튼)
    public void OnClickDateButton()
    {
        if (data.money < 5000) // 데이트 비용 체크 (예시: 5000원)
        {
            ShowPopup("돈이 부족합니다. (5000원 필요)");
            return;
        }

        // 팝업 띄우기 (이미 만난 히로인은 잠금 처리)
        OpenDatePopup();
    }

    void OpenDatePopup()
    {
        if (datePopupGroup != null)
        {
            datePopupGroup.SetActive(true);

            // 히로인별 잠금 상태 확인
            for (int i = 0; i < heroineButtons.Length; i++)
            {
                // 데이터 배열 길이 체크 (에러 방지)
                if (i < data.isDateLocked.Length && i < heroineButtons.Length)
                {
                    // true(잠김)면 버튼 비활성화
                    if (data.isDateLocked[i] == true)
                    {
                        heroineButtons[i].interactable = false;
                    }
                    else
                    {
                        heroineButtons[i].interactable = true;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Date Popup Group이 연결되지 않았습니다!");
        }
    }

    // 2. 히로인 버튼 클릭 시 (인자로 0, 1, 2 번호를 받음)
    public void OnClickHeroine(int heroineIndex)
    {
        if (datePopupGroup != null) datePopupGroup.SetActive(false);

        // 비용 소모
        data.money -= 5000;

        // ★ 이번 주는 이 히로인 잠그기!
        if (heroineIndex < data.isDateLocked.Length)
        {
            data.isDateLocked[heroineIndex] = true;
        }

        // 호감도 증가
        if (heroineIndex < data.lovePoints.Length)
        {
            data.lovePoints[heroineIndex] += 10;
        }

        // 데이트 대화 파일 실행 (예: Date_HeroineA_Event)
        string heroineName = "";
        if (heroineIndex == 0) heroineName = "MoonsunYoung";
        else if (heroineIndex == 1) heroineName = "HanGooRoo";
        else if (heroineIndex == 2) heroineName = "LeeNaChun";
        else if (heroineIndex == 3) heroineName = "MyungEunHyuk";
        else heroineName = "RileyLoveCraft";

        string fileName = $"Date_{heroineName}_Event";
        DialogueManager.Instance.StartDialogue(fileName);

        data.timeIndex = 0; // 시간을 다시 '오전'으로 초기화
        NextDay();          // 날짜를 하루 넘김 (월/일/요일 변경)

        UpdateUI();         // 화면 갱신
    }

    // 팝업 닫기 (X버튼)
    public void CloseDatePopup()
    {
        if (datePopupGroup != null) datePopupGroup.SetActive(false);
    }
    public void OnClickWeekendPartTime()
    {
        // 체력 체크 (하루 종일 하니까 좀 더 많이 듬)
        if (data.stamina < 30)
        {
            ShowPopup("체력이 너무 부족합니다.\n(30 필요)");
            return;
        }

        data.stamina -= 30;
        data.money += 50000; // 평일(30000)보다 더 많이!

        // ★ 하루 강제 경과 로직
        data.timeIndex = 0; // 시간을 '오전'으로 초기화
        NextDay();          // 날짜 넘기기

        UpdateUI();

        ShowPopup("주말 풀타임 알바 완료!\n50,000원을 벌었습니다.");
    }

    // 2. 주말 자습 (학력 많이, 하루 지남)
    public void OnClickSelfStudy()
    {
        if (data.stamina < 30)
        {
            ShowPopup("집중할 체력이 없습니다.\n(30 필요)");
            return;
        }

        data.stamina -= 30;
        data.stats[0] += 15; // 평일(5)보다 훨씬 많이 상승!

        // ★ 하루 강제 경과 로직
        data.timeIndex = 0;
        NextDay();

        UpdateUI();

        ShowPopup("하루 종일 자습을 했습니다.\n지식이 대폭 상승했습니다!");
    }

    // 3. 주말 휴식 (체력 회복, 하루 지남) - 혹시 필요하시면 쓰세요
    public void OnClickWeekendRest()
    {
        data.stamina += 50; // 체력 회복
        if (data.stamina > 100) data.stamina = 100;

        data.timeIndex = 0;
        NextDay();

        UpdateUI();
        ShowPopup("푹 쉬었습니다.\n체력이 회복되었습니다.");
    }
    public void OnClickShop()
    {
        // 1. 체력 & 돈 체크
        if (data.stamina < 10)
        {
            ShowPopup("쇼핑할 체력이 없습니다.");
            return;
        }
        if (data.money < 5000)
        {
            ShowPopup("돈이 부족합니다.\n(5,000원 필요)");
            return;
        }

        // 2. 비용 소모
        data.stamina -= 10;
        data.money -= 5000;

        // 3. ★ 외모(stats[1]) 상승 ★
        data.stats[1] += 5;

        // 4. 시간 경과 및 갱신
        PassTime();
        UpdateUI();

        ShowPopup("쇼핑을 했습니다.\n외모가 예뻐졌습니다! (+5)");
    }
}