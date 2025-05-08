using System.Collections;
using System.Collections.Generic;
using System.IO; // 파일 입출력 사용
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;
using Unity.VisualScripting; // Encoding 사용 시

public class BoardGenerator : MonoBehaviour
{
    public int horizontalTiles = 7;
    public int verticalTiles = 5;
    public float tileSize = 200f;
    public float horizontalSpacing = 60f; // 가로 간격 변수 추가 (값을 원하는 만큼 늘리세요)
    public float verticalSpacing = 10f;   // 세로 간격 변수 추가 (기존 간격을 유지하거나 다르게 설정)
    public GameObject tilePrefab;
    public Transform[] tileTransforms;
    public PlayerMovements playerMovements;
    [Header("Tile Sprites")]
    public Sprite defaultTileSprite;
    public Sprite cornerSprite;

    private List<string> tileActions; // Action.txt 내용을 저장할 리스트
    private int totalOuterTiles;

    void Start()
    {
        LoadTileActions();
        GenerateBoard();
    }

    // Action.txt 파일 로드 함수
    void LoadTileActions()
    {
        tileActions = new List<string>();
        string filePath = Path.Combine(Application.streamingAssetsPath, "Action.txt");

        try
        {
            // UTF-8 인코딩으로 읽기 (메모장 기본 저장 인코딩 문제 방지)
            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    tileActions.Add(line);
                }
            }
            Debug.Log($"Action.txt 로드 완료 : 총 {tileActions.Count}개의 액션");
        }
        catch (FileNotFoundException)
        {
            Debug.LogError($"오류: Action.txt 파일을 찾을 수 없습니다! 경로: {filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"오류: Action.txt 파일을 읽는 중 오류 발생: {e.Message}");
        }
    }

    void GenerateBoard()
    {
        totalOuterTiles = (horizontalTiles * 2) + ((verticalTiles - 2) * 2);
        tileTransforms = new Transform[totalOuterTiles];
        int currentTileIndex = 0;

        // 시작 위치 계산 시 horizontalSpacing과 verticalSpacing 사용
        float startX = -(horizontalTiles - 1) * (tileSize + horizontalSpacing) / 2f;
        float startY = (verticalTiles - 1) * (tileSize + verticalSpacing) / 2f;

        // 상단 가로 라인 (왼쪽 -> 오른쪽) : horizontalSpacing 사용
        for (int i = 0; i < horizontalTiles; i++)
        {
            Vector3 position = new Vector3(startX + i * (tileSize + horizontalSpacing), startY, 0);
            tileTransforms[currentTileIndex] = CreateTile(position, currentTileIndex);
            currentTileIndex++;
        }

        // 오른쪽 세로 라인 (위 -> 아래) : verticalSpacing 사용
        // 가로 위치는 horizontalSpacing을 사용하여 계산된 맨 오른쪽 X좌표 사용
        float rightEdgeX = startX + (horizontalTiles - 1) * (tileSize + horizontalSpacing);
        for (int i = 1; i < verticalTiles - 1; i++) // 모서리 제외
        {
            Vector3 position = new Vector3(rightEdgeX, startY - i * (tileSize + verticalSpacing), 0);
            tileTransforms[currentTileIndex] = CreateTile(position, currentTileIndex);
            currentTileIndex++;
        }

        // 하단 가로 라인 (오른쪽 -> 왼쪽) : horizontalSpacing 사용
        // 세로 위치는 verticalSpacing을 사용하여 계산된 맨 아래쪽 Y좌표 사용
        float bottomEdgeY = startY - (verticalTiles - 1) * (tileSize + verticalSpacing);
        for (int i = horizontalTiles - 1; i >= 0; i--)
        {
            Vector3 position = new Vector3(startX + i * (tileSize + horizontalSpacing), bottomEdgeY, 0);
            tileTransforms[currentTileIndex] = CreateTile(position, currentTileIndex);
            currentTileIndex++;
        }

        // 왼쪽 세로 라인 (아래 -> 위) : verticalSpacing 사용
        // 가로 위치는 맨 왼쪽 X좌표(startX) 사용
        for (int i = verticalTiles - 2; i >= 1; i--) // 모서리 제외
        {
            Vector3 position = new Vector3(startX, startY - i * (tileSize + verticalSpacing), 0);
            tileTransforms[currentTileIndex] = CreateTile(position, currentTileIndex);
            currentTileIndex++;
        }

        if (tileActions != null && tileActions.Count != totalOuterTiles)
        {
            Debug.LogWarning($"경고: Action.txt의 줄 수({tileActions.Count})와 보드 칸 수({totalOuterTiles})가 일치하지 않습니다.");
        }

        playerMovements.Initialize();
    }

    Transform CreateTile(Vector3 position, int index)
    {
        GameObject tileGO = Instantiate(tilePrefab, transform); // BoardPanel 하위에 생성
        tileGO.GetComponent<RectTransform>().localPosition = position;
        TileInfo tileInfo = tileGO.GetComponent<TileInfo>();
        tileInfo.tileIndex = index;

        // 파일에서 읽어온 액션 할당
        if(tileActions != null && index >= 0 && index < tileActions.Count)
        {
            tileInfo.actionDescription = tileActions[index];
        }
        else
        {
            tileInfo.actionDescription = $"액션 정보 없음 (칸 {index})"; // 로드 실패 또는 인덱스 오류 시
            Debug.LogWarning($"칸 {index}에 대한 액션 정보를 Action.txt에서 찾을 수 없습니다.");
        }

        tileGO.name = "Tile_" + index;

        // 자식 오브젝트에서 TextMeshProUGUI 컴포넌트 찾기
        TextMeshProUGUI actionTextComponent = tileGO.GetComponentInChildren<TextMeshProUGUI>();
        if( actionTextComponent != null)
        {
            actionTextComponent.text = tileInfo.actionDescription;
        }
        else
        {
            // 프리팹에 Text 컴포넌트가 없는 경우 경고
            Debug.LogWarning($"칸 {index} ({tileGO.name})의 프리팹에 TextMeshProUGUI (또는 Text) 자식 컴포넌트가 없습니다. 프리팹 설정을 확인하세요.");
        }

        // 타일 이미지 할당 로직 수정
        Image tileImage = tileGO.GetComponent<Image>();
        if(tileImage != null)
        {
            // 모서리 인덱스 정의
            int topLeftIndex = 0;
            int topRightIndex = horizontalTiles - 1;
            int bottomRightIndex = horizontalTiles + verticalTiles - 2;
            int bottomLeftIndex = totalOuterTiles - 4;

            // 인덱스 확인 및 스프라이트 할당
            if (index == topLeftIndex || index == topRightIndex || index == bottomLeftIndex || index == bottomRightIndex && cornerSprite != null)
            {
                tileImage.sprite = cornerSprite;
            }
            else if (defaultTileSprite != null)
            {
                tileImage.sprite = defaultTileSprite;
            }
        }
        else
        {
            Debug.LogWarning($"칸 {index} ({tileGO.name})에 Image 컴포넌트가 없습니다.");
        }
        return tileGO.transform;
    }
}
