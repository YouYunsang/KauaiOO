using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;           // 따라다닐 대상 (Player)
    public Vector3 offset;            // 캐릭터와의 간격
    public float smoothSpeed = 5f;    // 카메라가 따라가는 부드러움 정도

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 목표 위치 계산 (캐릭터 위치 + 설정한 간격)
        Vector3 desiredPosition = target.position + offset;

        // 2. 부드러운 이동 (Lerp 사용)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. 카메라 위치 업데이트
        transform.position = smoothedPosition;
    }
}