using UnityEngine;

public class GirlCharacter : MonoBehaviour
{
    public int level;
    public string displayName;
    public SpriteRenderer spriteRenderer;
    public GirlMergeManager mergeManager;
    public GirlEvolutionData evolutionData;

    // 애니메이션 파라미터
    public float bounceSpeed = 2.0f;      // 뛰는 속도
    public float bounceScale = 0.1f;      // 스케일 변화 폭
    public float moveSpeed = 1.5f;        // 좌우 흔들림 속도
    public float moveRange = 0.15f;       // 좌우 이동 범위

    private Vector3 baseScale;
    private Vector3 basePosition;
    private float randomOffset;

    void Start()
    {
        baseScale = transform.localScale;
        basePosition = transform.position;
        randomOffset = Random.Range(0f, 100f); // 유닛별로 움직임 위상 다르게
    }

    void Update()
    {
        // 스케일(펄스) 애니메이션
        float scaleAnim = 1.0f + Mathf.Sin(Time.time * bounceSpeed + randomOffset) * bounceScale;
        transform.localScale = baseScale * scaleAnim;

        // 좌우 흔들림 애니메이션
        float moveAnim = Mathf.Sin(Time.time * moveSpeed + randomOffset) * moveRange;
        transform.position = basePosition + new Vector3(moveAnim, 0, 0);
    }

    // 호출시 데이터 적용
    public void Init(GirlEvolutionData data, Sprite sprite)
    {
        evolutionData = data; 
        level = data.level;
        displayName = data.name;
        spriteRenderer.sprite = sprite;
        // 기타 데이터 적용(필요시)
    }
    
    void OnMouseDown()
    {
        mergeManager.OnGirlClicked(this);
    }
    
    public void SetSelected(bool isSelected)
    {
        // 선택 연출, 예: Outline, 스케일업, 등
    }
}