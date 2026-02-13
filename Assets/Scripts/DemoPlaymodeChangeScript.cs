using UnityEngine;

public class DemoPlaymodeChangeScript : MonoBehaviour
{
    [Header("Basic Values")]
    public int health = 100;
    public float speed = 5f;
    public bool isBoss = false;

    [Header("Vector Values")]
    public Vector3 spawnOffset = new Vector3(0, 2, 0);
    public Vector2 uiOffset = new Vector2(50, 50);

    [Header("References")]
    public Transform target;
    public GameObject somePrefab;
    public Material materialRef;

    [Header("Color / String")]
    public Color tint = Color.white;
    public string npcName = "Enemy_01";

    [Header("Array / List")]
    public int[] scores = new int[] { 10, 20, 30 };

    [TextArea]
    public string notes = "Change me in Play Mode!";

    [Header("Nested Class Example")]
    public Stats stats = new Stats();

    [System.Serializable]
    public class Stats
    {
        public float attack = 10;
        public float defense = 5;
        public float stamina = 20;
    }
}