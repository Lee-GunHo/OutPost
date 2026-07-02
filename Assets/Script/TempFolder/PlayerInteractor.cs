using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{ 
    [Header("Interaction Settings")]

    [Tooltip("플레이어가 벽을 부술 수 있는 최대 거리")]
    public float breakRange = 5f; 
    public LayerMask wallLayer;

    private Camera mainCamera; 

    private void Awake()
    { 
        // 현재 씬에서 MainCamera 태그가 붙은 카메라 가져옴
        mainCamera = Camera.main;
    } 
    
    private void Update()
    { 
        // New Input System 기준 마우스가 없는 환경 체크
        if (Mouse.current == null)
            return;
        
        // 마우스 좌클릭을 '누른 순간' 한 번만 감지
        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryBreakWall();
        }
    }
    
    /// <summary>
    /// 마우스 위치에서 Ray 쏘고, 벽을 맞췄는지 검사하는 함수
    /// </summary>
    private void TryBreakWall()
    {
        // 카메라가 없으면 Ray 만들수 없으니까 종료
        if(mainCamera == null)
            return;
        
        // 현재 마우스 화면 좌표 가져옴
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // 마우스 화면 좌표 기준으로 카메라에서 3D 월드 방향으로 Ray 만듦
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        
        // Ray가 wallLayer에 해당하는 Collider와 부딪혔는지 검사
        if(Physics.Raycast(ray, out RaycastHit hit, 100f, wallLayer))
        {
            // Ray가 맞춘 Collider가 붙은 오브젝트에서 BreakableWall 컴포넌트 찾기
            BreakableWall wall = hit.collider.GetComponent<BreakableWall>();
            
            // 만약 Collider가 자식 오브젝트에 있고
            // BreakableWall이 부모에 붙어 있다면
            // 위 GetComponent로 못 찾을 수 있기 때문에
            // 부모에서도 한 번 더 찾기
            if(wall == null)
            {
                wall = hit.collider.GetComponentInParent<BreakableWall>();
            }
            
            // BreakableWall이 없으면 부술 수 없는 오브젝트니까 종료
            if (wall == null) 
                return;
            
            // 플레이어와 벽 사이의 거리 계산
            float distance = Vector3.Distance(transform.position, wall.transform.position);
            
            // 벽이 breakRange 안에 있을 때만 부숨
            if(distance <= breakRange) 
            { 
                // 벽의 Break() 함수 호출
                wall.Break(); 
            } 
            else 
            { 
                // 거리가 멀면 부수지 않고 로그 출력
                Debug.Log("벽이 너무 멀다."); 
            } 
        } 
    } 
}