using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴(간단 버전) - 다른 스크립트에서 쉽게 접근하기 위함
    public static GameManager instance { get; private set; }

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

    private int currentDiceValue;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if(instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환시 유지하려면 주석 해제
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 버튼 이벤트 리스너 연결
        diceRollButton.onClick.AddListener(RollDice);
        moveButton.onClick.AddListener(ConfirmMove);

        // 초기 상태 설정
        diceResultPopup.SetActive(false);
        diceRollButton.interactable = true;

        actionPopup.SetActive(false);
        closeActionButton.onClick.AddListener(CloseActionPopup);

        if(exitButton != null)
        {
            exitButton.onClick.AddListener(QuitGame);
        }
    }

    void RollDice()
    {
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
