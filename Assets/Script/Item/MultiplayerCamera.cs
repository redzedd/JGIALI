using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class MultiplayerCamera : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 40f;  // FOV �̤p�� (�̪�)
    public float maxZoom = 60f;  // FOV �̤j�� (�̻�)
    public float zoomLimiter = 10f; // �Y��Y�� (�V�j�Y��V������)

    [Header("Movement Settings")]
    public Vector3 offset = new Vector3(0, 15, -10); // �۾��P���ߪ�����
    public float smoothTime = 0.5f;
    [Tooltip("攝影機鎖定中心點的 Y 下限,防止有玩家掉出地圖時鏡頭跟著鑽到地板下方(燈光/取景異常的根源)")]
    public float minCenterY = 8f;

    private Vector3 velocity;
    private Camera cam;

    // �۰ʰl�ܩҦ��[�J�����a
    private List<Transform> targets = new List<Transform>();

    private void Awake()
    {
        cam = GetComponent<Camera>();

        // �q�\ PlayerInputManager �ƥ�
        // �������a�[�J�ɡA�۰ʱN��[�J�l�ܦC��
        var manager = FindObjectOfType<PlayerInputManager>();
        if (manager != null)
        {
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        targets.Add(input.transform);
    }

    private void OnPlayerLeft(PlayerInput input)
    {
        targets.Remove(input.transform);
    }

    private void LateUpdate()
    {
        if (targets.Count == 0) return;

        Move();
        Zoom();
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        // [Fix] 夾住中心點 Y 下限,避免攝影機跟著墜落玩家鑽到地圖下方(燈光/取景異常的根源)
        centerPoint.y = Mathf.Max(centerPoint.y, minCenterY);
        Vector3 newPosition = centerPoint + offset;
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    void Zoom()
    {
        // �ھ� targets ���̤j�Z���ӭp��s�� FOV
        float newZoom = Mathf.Lerp(maxZoom, minZoom, GetGreatestDistance() / zoomLimiter);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
    }

    float GetGreatestDistance()
    {
        // �p��]��Ҧ����a����ؤj�p
        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        // �^�Ǫ��μe���̤j��
        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
        {
            return targets[0].position;
        }

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        return bounds.center;
    }
}