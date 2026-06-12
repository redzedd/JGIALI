using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameMultiplayerManager : MonoBehaviour
{
    public static GameMultiplayerManager Instance { get; private set; }

    [Header("Settings")]
    public Transform[] spawnPoints; // P1, P2 �X���I
    public GameObject[] fullPlayerPrefabs; // 0: P1 Prefab, 1: P2 Prefab

    private PlayerInputManager _inputManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        _inputManager = GetComponent<PlayerInputManager>();

        if (_inputManager != null)
        {
            // ���s�q�\�ƥ�
            _inputManager.onPlayerJoined -= OnPlayerJoined;
            _inputManager.onPlayerJoined += OnPlayerJoined;

            // [Important] �C���}�l�e�A�T�O�w�]�ͦ����O P1 (�Ŧ�)
            if (fullPlayerPrefabs != null && fullPlayerPrefabs.Length > 0)
            {
                _inputManager.playerPrefab = fullPlayerPrefabs[0];
            }
        }
    }

    private void OnDisable()
    {
        if (_inputManager != null)
        {
            _inputManager.onPlayerJoined -= OnPlayerJoined;
        }
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        int playerIndex = input.playerIndex;
        Debug.Log($"[Manager] ���a {playerIndex + 1} �[�J! (����W��: {input.gameObject.name})");

        PlayerStateMachine psm = input.GetComponent<PlayerStateMachine>();

        if (psm != null)
        {
            // 1. �]�w���� ID
            psm.TeamID = playerIndex;

            // 2. �]�w��m (�w�� Rigidbody ���ץ�)
            if (spawnPoints != null && playerIndex < spawnPoints.Length)
            {
                // �p�G�� Rigidbody�A�����Ϊ��z�覡���ʡA�_�h�|�Q�Ԧ^���I
                if (psm.Rb != null)
                {
                    psm.Rb.position = spawnPoints[playerIndex].position;
                    psm.Rb.rotation = spawnPoints[playerIndex].rotation;
                    psm.Rb.linearVelocity = Vector3.zero; // �k�s�t�סA����Q��
                    psm.Rb.angularVelocity = Vector3.zero;
                }

                // �����O�I�G�j��]�w Transform
                psm.transform.position = spawnPoints[playerIndex].position;
                psm.transform.rotation = spawnPoints[playerIndex].rotation;

                // [Fix] 記住出生點,供掉落重生 (PlayerStateMachine.Respawn) 使用
                psm.RespawnPoint = spawnPoints[playerIndex].position;

                Debug.Log($"[Manager] �w�N P{playerIndex + 1} �ǰe�ܥX���I");
            }

            // 3. [����] �ǳƤU�@�Ӫ��a�� Prefab
            // �� P1 (Index 0) �[�J��A�ڭ̰��W�� Manager ���ͦ��ؼЧ令 P2 Prefab
            // �o�ˤU�@�ӤH�����s�ɡAInputManager �N�|�ͦ����⪺
            int nextIndex = playerIndex + 1;
            if (fullPlayerPrefabs != null && nextIndex < fullPlayerPrefabs.Length)
            {
                _inputManager.playerPrefab = fullPlayerPrefabs[nextIndex];
                Debug.Log($"[Manager] �����U�@�ӥͦ��ؼЬ�: {fullPlayerPrefabs[nextIndex].name}");
            }
        }
    }
}