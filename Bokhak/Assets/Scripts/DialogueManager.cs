using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // 버튼 사용을 위해 필수

// ==========================================
// 1. 데이터 설계도 (선택지 클래스 추가!)
// ==========================================
[System.Serializable]
public class ChoiceData
{
    public string text; // 버튼에 들어갈 텍스트
    public int next;    // 버튼 누르면 이동할 대화 ID
}

[System.Serializable]
public class DialogueData
{
    public int id;
    public string type; // "Talk" 또는 "Choice"
    public string name;
    public string text;
    public string image;
    public string background;
    public int next;

    public ChoiceData[] choices; // 선택지 목록
}

[System.Serializable]
public class DialogueWrapper
{
    public DialogueData[] dialogues;
}

// ==========================================
// 2. 매니저 기능 구현
// ==========================================
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI 연결")]
    public GameObject dialogueGroup;
    public GameObject mainMenuGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image characterImg;
    public Image backgroundImage;
    private Sprite originalBackground;
    [Header("선택지 UI 연결 (추가됨)")]
    public GameObject choiceGroup;        // 선택지 전체 패널 (어두운 배경)
    public Button[] choiceButtons;        // 버튼 3개 배열
    public TextMeshProUGUI[] choiceTexts; // 버튼 안의 텍스트 3개 배열

    Dictionary<int, DialogueData> dialogueDic = new Dictionary<int, DialogueData>();
    int currentDialogueId = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    void Start()
    {
        // 게임 시작할 때 설정된 기본 배경(학교 정문 등)을 기억해둡니다.
        if (backgroundImage != null)
        {
            originalBackground = backgroundImage.sprite;
        }
    }
    // 이 함수 전체를 복사해서 덮어씌우세요!
    public void StartDialogue(string jsonFileName)
    {
        Debug.Log("================ [디버깅 시작] ================");
        Debug.Log($"1. 요청받은 파일 이름: '{jsonFileName}'");

        dialogueDic.Clear();

        // 1. 파일 로드 시도
        string loadPath = "Dialogues/" + jsonFileName;
        Debug.Log($"2. Resources 폴더 검색 경로: Assets/Resources/{loadPath}");

        TextAsset jsonText = Resources.Load<TextAsset>(loadPath);

        // [체크 1] 파일이 없는 경우
        if (jsonText == null)
        {
            Debug.LogError("🚨 [치명적 에러] 파일을 찾을 수 없습니다!");
            Debug.LogError($"확인 1: 'Assets/Resources/{loadPath}' 경로에 파일이 진짜 있나요?");
            Debug.LogError($"확인 2: 파일 이름 뒤에 .txt나 .json이 중복으로 붙어있진 않나요? (예: {jsonFileName}.json.json)");
            Debug.LogError($"확인 3: 폴더 이름 스펠링이 Dialogues (s 붙음) 맞나요?");
            return;
        }

        Debug.Log($"3. 파일 찾기 성공! (내용 길이: {jsonText.text.Length}자)");

        // 2. JSON 파싱 시도
        try
        {
            DialogueWrapper wrapper = JsonUtility.FromJson<DialogueWrapper>(jsonText.text);

            // [체크 2] JSON 문법 문제 or 내용 없음
            if (wrapper == null || wrapper.dialogues == null)
            {
                Debug.LogError("🚨 [치명적 에러] JSON 내용은 읽었지만, 데이터가 비어있습니다!");
                Debug.LogError("JSON 형식이 맞는지, 오타가 없는지 확인하세요. (콤마, 괄호 등)");
                return;
            }

            Debug.Log($"4. 데이터 변환 성공! (대화 개수: {wrapper.dialogues.Length}개)");

            foreach (DialogueData data in wrapper.dialogues)
            {
                dialogueDic.Add(data.id, data);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🚨 [치명적 에러] JSON 변환 중 오류 발생: {e.Message}");
            return;
        }

        // 3. UI 켜기
        if (mainMenuGroup != null) mainMenuGroup.SetActive(false);

        if (dialogueGroup != null)
        {
            dialogueGroup.SetActive(true);
            Debug.Log($"5. 대화창 UI ({dialogueGroup.name}) 켜짐 완료");
        }
        else
        {
            Debug.LogError("🚨 [에러] Dialogue Group이 연결되지 않았습니다! Inspector를 확인하세요.");
            return;
        }

        if (choiceGroup != null) choiceGroup.SetActive(false);

        // 4. 대화 시작
        ShowDialogue(0);
        Debug.Log("================ [디버깅 종료: 성공] ================");
    }

    void ShowDialogue(int id)
    {
        currentDialogueId = id;
        DialogueData data = dialogueDic[id];

        // 1. 타입에 따른 분기 처리
        if (data.type == "Choice")
        {
            ShowChoiceUI(data); // 선택지 보여주기 함수 호출
        }
        else
        {
            // 일반 대화라면 선택지 창 끄기
            if (choiceGroup != null) choiceGroup.SetActive(false);

            nameText.text = data.name;
            dialogueText.text = data.text;
        }

        // 2. 캐릭터 이미지 처리
        if (data.image == "None" || string.IsNullOrEmpty(data.image))
        {
            characterImg.gameObject.SetActive(false);
        }
        else
        {
            Sprite charSprite = Resources.Load<Sprite>("Characters/" + data.image);
            if (charSprite != null)
            {
                characterImg.gameObject.SetActive(true);
                characterImg.sprite = charSprite;
                characterImg.SetNativeSize();
            }
        }

        // 3. 배경 이미지 처리
        if (!string.IsNullOrEmpty(data.background) && data.background != "Keep")
        {
            Sprite bgSprite = Resources.Load<Sprite>("Backgrounds/" + data.background);
            if (bgSprite != null) backgroundImage.sprite = bgSprite;
        }
    }

    // =============================================
    // [추가됨] 선택지 UI 표시 함수
    // =============================================
    void ShowChoiceUI(DialogueData data)
    {
        choiceGroup.SetActive(true); // 패널 켜기

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < data.choices.Length)
            {
                // 버튼 켜고 텍스트 설정
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = data.choices[i].text;

                // 버튼 클릭 이벤트 연결 (람다식 주의)
                int nextID = data.choices[i].next;
                choiceButtons[i].onClick.RemoveAllListeners(); // 기존 연결 삭제
                choiceButtons[i].onClick.AddListener(() => OnClickChoice(nextID));
            }
            else
            {
                // 데이터 없는 버튼은 끄기
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 선택지 버튼 눌렀을 때 실행되는 함수
    void OnClickChoice(int nextID)
    {
        choiceGroup.SetActive(false); // 선택지 끄고
        ShowDialogue(nextID);         // 다음 대화로 이동
    }

    public void NextDialogue()
    {
        // [중요] 선택지가 떠 있을 때는 화면 클릭해도 안 넘어가게 막음
        if (choiceGroup != null && choiceGroup.activeSelf) return;

        if (dialogueDic.ContainsKey(currentDialogueId))
        {
            int nextId = dialogueDic[currentDialogueId].next;
            if (nextId == -1) EndDialogue();
            else ShowDialogue(nextId);
        }
    }

    void EndDialogue()
    {
        dialogueGroup.SetActive(false);
        characterImg.gameObject.SetActive(false);

        // [추가] 대화가 끝나면 배경을 원래대로(기본 배경) 되돌림!
        if (backgroundImage != null && originalBackground != null)
        {
            backgroundImage.sprite = originalBackground;
        }

        if (choiceGroup != null) choiceGroup.SetActive(false);

        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);

        Debug.Log("대화 종료 -> 메인 화면 복귀");
    }
}