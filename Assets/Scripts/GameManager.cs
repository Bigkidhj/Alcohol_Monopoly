using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴(간단 버전) - 다른 스크립트에서 쉽게 접근하기 위함
    public static GameManager Instance { get; private set; }

    public Button diceRollButton;
    public GameObject diceResultPopup;
    public TMP_Text resultText;
    public Button moveButton;
    public GameObject actionPopup;
    public TMP_Text actionText;
    public Button closeActionButton;
    public Button exitButton;

    public PlayerMovements playerMovements;
    public BoardGenerator boardGenerator;

    // ----- 새로운 기능 관련 변수 및 UI 참조 -----
    [Header("Dice Roll Attempts")]
    public Button addAttemptButton;       // 횟수 추가 버튼 (Inspector에서 연결)
    public TMP_Text attemptsText;         // 남은 횟수 표시 텍스트 (Inspector에서 연결)
    private int diceRollAttempts = 0;     // 주사위 굴리기 가능 횟수 (시작 시 0회 제공)

    [Header("Lap Count")]
    public TMP_Text lapCountText;         // 바퀴 수 표시 텍스트 (Inspector에서 연결)
    private int lapCount = 0;             // 현재 바퀴 수

    // ----- 캐릭터 선택 기능 관련 변수 -----
    [Header("Character Selection")]
    public GameObject characterSelectionPopup; // 캐릭터 선택 팝업 GameObject (Inspector에서 연결)
    public Button[] characterSelectionButtons; // 캐릭터 선택 버튼 배열 (Inspector에서 크기 4로 설정 후 연결)
    public Image playerImage;                  // 플레이어의 Image 컴포넌트 (Inspector에서 연결)
    public Image diceResultPopupImage;         // 주사위 결과 팝업의 Image 컴포넌트 (Inspector에서 연결)
    public Image actionPopupImage;             // 액션 팝업의 Image 컴포넌트 (Inspector에서 연결)
    public Sprite[] playerSprites;             // 플레이어 이미지 배열 (Inspector에서 크기 4로 설정 후 이미지 할당)
    public Sprite[] dicePopupSprites;          // 주사위 결과 팝업 배경 이미지 배열 (Inspector에서 크기 4로 설정 후 이미지 할당)
    public Sprite[] actionPopupSprites;        // 행동 팝업 배경 이미지 배열

    private int currentDiceValue;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if(Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환시 유지하려면 주석 해제
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ----- 캐릭터 선택 팝업 초기화 -----
        if (characterSelectionPopup != null)
        {
            characterSelectionPopup.SetActive(true); // 캐릭터 선택 팝업 표시
            // 다른 게임 UI는 선택 완료 후 활성화
            diceRollButton.gameObject.SetActive(false);
            if (addAttemptButton != null) addAttemptButton.gameObject.SetActive(false);
            // 다른 UI 요소들도 필요에 따라 비활성화
        }
        else
        {
            Debug.LogError("Character Selection Popup이 연결되지 않았습니다!");
            // 선택 팝업이 없으면 바로 게임 시작 준비
            InitializeGameControls();
        }

        // 캐릭터 선택 버튼 이벤트 리스너 연결
        if (characterSelectionButtons != null && characterSelectionButtons.Length == 4)
        {
            for (int i = 0; i < characterSelectionButtons.Length; i++)
            {
                int buttonIndex = i; // 클로저 문제를 피하기 위해 로컬 변수 사용
                characterSelectionButtons[i].onClick.AddListener(() => OnCharacterSelected(buttonIndex));
            }
        }
        else
        {
            Debug.LogError("캐릭터 선택 버튼이 제대로 설정되지 않았습니다. Inspector에서 4개의 버튼을 연결해주세요.");
        }

        // 버튼 이벤트 리스너 연결
        diceRollButton.onClick.AddListener(RollDice);
        moveButton.onClick.AddListener(ConfirmMove);
        closeActionButton.onClick.AddListener(CloseActionPopup);

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(QuitGame);
        }

        // ----- 새로운 기능 리스너 및 UI 초기화 -----
        if (addAttemptButton != null) addAttemptButton.onClick.AddListener(AddDiceAttempt);
        UpdateAttemptsUI(); // 초기 횟수 UI 업데이트
        UpdateLapCountUI(); // 초기 바퀴 수 UI 업데이트
        CheckDiceRollButtonState(); // 초기 주사위 버튼 상태 설정

        // 초기 상태 설정
        diceResultPopup.SetActive(false);
        actionPopup.SetActive(false);
    }

    // 캐릭터 선택 시 호출될 함수
    void OnCharacterSelected(int index)
    {
        if (playerImage != null && playerSprites != null && index >= 0 && index < playerSprites.Length)
        {
            playerImage.sprite = playerSprites[index];
        }
        else
        {
            Debug.LogError($"플레이어 이미지를 변경할 수 없습니다. (인덱스: {index})");
        }

        if (diceResultPopupImage != null && dicePopupSprites != null && index >= 0 && index < dicePopupSprites.Length)
        {
            diceResultPopupImage.sprite = dicePopupSprites[index];
        }
        else
        {
            Debug.LogError($"주사위 결과 팝업 이미지를 변경할 수 없습니다. (인덱스: {index})");
        }

        if (actionPopupImage != null && actionPopupSprites != null && index >= 0 && index < actionPopupSprites.Length)
        {
            actionPopupImage.sprite = actionPopupSprites[index];
        }
        else
        {
            Debug.LogError($"주사위 결과 팝업 이미지를 변경할 수 없습니다. (인덱스: {index})");
        }

        if (characterSelectionPopup != null)
        {
            characterSelectionPopup.SetActive(false); // 선택 팝업 닫기
        }

        // 게임 컨트롤 UI 활성화 및 초기화
        InitializeGameControls();
    }

    // 게임 컨트롤 UI 활성화 및 관련 값 초기화
    void InitializeGameControls()
    {
        diceRollButton.gameObject.SetActive(true);
        if (addAttemptButton != null) addAttemptButton.gameObject.SetActive(true);
        // 다른 필요한 UI 요소들도 여기서 활성화

        UpdateAttemptsUI(); // 초기 횟수 UI 업데이트
        UpdateLapCountUI(); // 초기 바퀴 수 UI 업데이트
        CheckDiceRollButtonState(); // 초기 주사위 버튼 상태 설정
    }

    // ----- 주사위 굴리기 횟수 관련 메소드 -----
    void AddDiceAttempt()
    {
        diceRollAttempts++;
        UpdateAttemptsUI();
        CheckDiceRollButtonState();
        Debug.Log("주사위 굴리기 횟수 1 추가. 현재: " + diceRollAttempts);
    }

    void UpdateAttemptsUI()
    {
        if (attemptsText != null)
        {
            attemptsText.text = "굴리기 횟수: " + diceRollAttempts;
        }
    }

    void CheckDiceRollButtonState()
    {
        if (diceRollButton != null)
        {
            diceRollButton.interactable = (diceRollAttempts > 0);
        }
    }

    // ----- 바퀴 수 관련 메소드 -----
    public void IncrementLapCount() // PlayerMovement에서 호출됨
    {
        lapCount++;
        UpdateLapCountUI();
        Debug.Log("바퀴 수 증가! 현재: " + lapCount + " 바퀴");
        // 여기에 바퀴 수 증가 시 효과음 등을 추가할 수 있습니다.
    }

    void UpdateLapCountUI()
    {
        if (lapCountText != null)
        {
            lapCountText.text = "바퀴 수: " + lapCount;
        }
    }

    void RollDice()
    {
        if (diceRollAttempts <= 0)
        {
            Debug.Log("주사위 굴리기 횟수가 없습니다.");
            CheckDiceRollButtonState(); // 확실히 비활성화
            return;
        }
        diceRollAttempts--; // 횟수 1 소모
        UpdateAttemptsUI();

        currentDiceValue = Random.Range(1, 7); // 1~6 사이의 랜덤 정수 생성
        resultText.text = $"주사위 결과: {currentDiceValue}";

        diceResultPopup.SetActive(true);
        diceRollButton.interactable = false; // 주사위 버튼 비활성화
    }

    void ConfirmMove()
    {
        diceResultPopup.SetActive(false);

        // MoveSteps 호출 시 반환되는 Coroutine 저장
        Coroutine moveCoroutine = null;
        if (playerMovements != null)
        {
            moveCoroutine = playerMovements.MoveSteps(currentDiceValue);
        }

        // 코루틴이 정상적으로 시작되었는지 확인 후 WaitForMoveEnd 호출
        if (moveCoroutine != null)
        {
            StartCoroutine(WaitForMoveEnd(moveCoroutine)); // Coroutine 참조 전달
        }
        else
        {
            Debug.LogError("PlayerMovement 참조가 없거나 이동을 시작할 수 없습니다!");
            diceRollButton.interactable = true; // 오류 시 버튼 다시 활성화
        }
    }

    // 파라미터로 Coroutine을 받고, 해당 코루틴을 직접 기다림
    IEnumerator WaitForMoveEnd(Coroutine moveCoroutine)
    {
        // isMoving 플래그 대신 전달받은 코루틴이 끝날 때까지 기다림
        yield return moveCoroutine;

        // 코루틴 종료 후 액션 팝업 표시
        ShowTileActionPopup();
    }

    void ShowTileActionPopup()
    {
        if (playerMovements == null) return;
        int currentTileIndex = playerMovements.GetCurrentTileIndex();
        string textToShow = GetActionForTile(currentTileIndex);
        actionText.text = textToShow;
        actionPopup.SetActive(true);
    }

    void CloseActionPopup()
    {
        actionPopup.SetActive(false); // 팝업 숨기기
        // 액션 팝업이 닫히면 주사위 굴리기가 가능하도록 버튼 활성화
        diceRollButton.interactable = true;
    }

    string GetActionForTile(int tileIndex)
    {
        if (boardGenerator != null && boardGenerator.tileTransforms != null &&
            tileIndex >= 0 && tileIndex < boardGenerator.tileTransforms.Length)
        {
            TileInfo info = boardGenerator.tileTransforms[tileIndex].GetComponent<TileInfo>();
            if (info != null)
            {
                // TileInfo에 저장된 actionDescription 반환 (이 값은 2단계에서 파일로부터 로드됨)
                return info.actionDescription;
            }
            else { Debug.LogError($"칸 {tileIndex}에서 TileInfo 컴포넌트를 찾을 수 없습니다."); }
        }
        else { Debug.LogError($"잘못된 타일 인덱스({tileIndex}) 또는 보드 정보 부족"); }

        return "칸 정보를 불러올 수 없습니다."; // 오류 발생 시 기본 메시지
    }

    // 다른 스크립트에서 호출될 함수
    public void OnActionPopupClosed()
    {
        // 액션 팝업이 닫히면 주사위 버튼 활성화
        diceRollButton.interactable = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
