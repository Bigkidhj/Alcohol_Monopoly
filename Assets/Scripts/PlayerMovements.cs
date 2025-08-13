using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovements : MonoBehaviour
{
    public BoardGenerator boardGenerator;
    public float moveSpeed = 500f;
    public float delayBetweenSteps = 0.1f; // 칸 이동 사이의 짧은 딜레이

    private int currentTileIndex = 0;
    private int totalTiles = 0;
    private bool isMoving = false; // 이동 중인지 확인하는 플래그
    private bool hasLeftStartTileOnce = false; // 시작 타일(0번)을 한 번이라도 떠났는지 여부

    void Start()
    {

    }

    public void Initialize()
    {
        if (boardGenerator != null && boardGenerator.tileTransforms != null && boardGenerator.tileTransforms.Length > 0)
        {
            totalTiles = boardGenerator.tileTransforms.Length;
            // 시작 위치로 설정
            transform.position = boardGenerator.tileTransforms[currentTileIndex].position;
        }
        else
        {
            Debug.LogError("BoardGenerator 또는 tileTransforms가 설정되지 않았습니다.");
        }
    }

    // 외부 (예: GameManger) 에서 호출될 이동 함수
    public Coroutine MoveSteps(int steps)
    {
        if (isMoving || boardGenerator == null || boardGenerator.tileTransforms == null || totalTiles == 0)
        {
            return null; // 이동 중이거나 보드 정보가 없으면 무시
        }

        return StartCoroutine(MoveStepByStep(steps));
    }

    IEnumerator MoveStepByStep(int steps)
    {
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            int previousSingleStepIndex = currentTileIndex; // 현재 스텝 이동 전 위치 저장
            int nextTileIndex = (currentTileIndex + 1) % totalTiles;
            Vector3 nextPosition = boardGenerator.tileTransforms[nextTileIndex].position;

            yield return StartCoroutine(AnimateSingleStep(nextPosition));

            currentTileIndex = nextTileIndex;

            // ----- 바퀴 수 감지 로직 -----
            if (currentTileIndex != 0) // 0번 타일이 아니면, 일단 시작 타일을 떠난 것으로 간주
            {
                hasLeftStartTileOnce = true;
            }
            // 시작 타일을 한 번이라도 떠났었고, 현재 0번 타일에 도착했으며, 이전 칸이 마지막 칸이었던 경우
            // (즉, 0번 타일을 '지나서' 다시 0번 타일에 '도착'한 경우)
            if (hasLeftStartTileOnce && currentTileIndex == 0 && previousSingleStepIndex == totalTiles - 1)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.IncrementLapCount();
                }
                // hasLeftStartTileOnce = false; // 한 바퀴 돌았으므로 다시 0번을 떠나야 다음 랩 감지 (선택적)
                // 이 플래그를 false로 하면 0번에서 0번으로 바로 한칸 더 돌때는 랩카운트 안됨.
                // 유저가 0번칸에서 다시 출발해서 0번칸으로 돌아올때 카운트되는것이 자연스러우므로 그대로 둠.
            }
            // 5. (선택 사항) 칸 이동 사이에 짧은 딜레이 추가
            if (delayBetweenSteps > 0)
            {
                yield return new WaitForSeconds(delayBetweenSteps);
            }
        }

        isMoving = false;
        // 모든 이동 완료 후 처리 (GameManager에게 알림 등)
        Debug.Log($"플레이어가 최종적으로 {currentTileIndex}번 칸에 도착했습니다.");
        // GameManager.Instance.ShowTileActionPopup(); // GameManager에서 호출하도록 변경될 수 있음 (WaitForMoveEnd에서 처리)
    }
    //기존 이동 방식
    /*
    IEnumerator MoveToTile(int targetIndex)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = boardGenerator.tileTransforms[targetIndex].position;

        float journeyLength = Vector3.Distance(startPos, endPos);
        float startTime = Time.time;

        // 부드러운 이동 (Lerp 방식 예시)
        // 한 칸씩 이동하는 로직으로도 변경 가능
        float distanceCovered = 0f;
        while (distanceCovered < journeyLength)
        {
            distanceCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
            yield return null; // 다음 프레임까지 대기
        }

        // 정확히 목표 위치에 도달하도록 보정
        transform.position = endPos;
        currentTileIndex = targetIndex;
        isMoving = false;

        // 이동 완료 처리 후 처리 (예: 칸 액션 팝업 호출)
        Debug.Log($"플레이어가 {currentTileIndex}번 칸에 도착했습니다.");
        // GameManager.Instance.ShowTileAction(currentTileIndex); // GameManager와 연동 필요
    }
    */

    // 한 칸을 부드럽게 이동하는 애니메이션 코루틴
    IEnumerator AnimateSingleStep(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        // 약간의 오차를 허용하는 거리 기반 체크가 더 안정적일 수 있음
        float closeEnoughDistance = 0.01f;

        while (Vector3.Distance(transform.position, targetPosition) > closeEnoughDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            // 목표 지점에 매우 가까워지면 루프 탈출 (무한 루프 방지)
            if (Vector3.Distance(transform.position, targetPosition) <= closeEnoughDistance)
            {
                break;
            }
            yield return null;
        }
        // 정확히 목표 위치에 도달하도록 보정
        transform.position = targetPosition;
    }

    public int GetCurrentTileIndex()
    {
        return currentTileIndex;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}