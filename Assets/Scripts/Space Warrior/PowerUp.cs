using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [Header("Set in Inspector")]
    // ���������, �� ������� ���������� Vector2. x  ������ ����������� ��������, � ..y.. - ������������ �������� ��� ������ Random.Range(),
    // ������� ����� ���������� �����
    public Vector2 rotMinMax = new Vector2(15, 90);
    public Vector2 driftMinMax = new Vector2(.25f, 2);
    public float lifeTime = 6f; // ����� � �������� ������������� PowerUp
    public float fadeTime = 4f; // Seconds it will then fade

    [Header("Set Dynamically")]
    public WeaponType type; // ��� ������
    public GameObject cube; // ������ �� ��������� ���
    public TextMesh letter; // ������ �� TextMesh
    public Vector3 rotPerSecond; // �������� ��������
    public float birthTime;

    private Rigidbody rigid;
    private BoundsCheck bndCheck;
    private Renderer cubeRend;

    void Awake()
    {
        // �������� ������ �� ���
        cube = transform.Find("Cube").gameObject;
        // �������� ������ �� TextMesh � ������ ����������
        letter = GetComponent<TextMesh>();
        rigid = GetComponent<Rigidbody>();
        bndCheck = GetComponent<BoundsCheck>();
        cubeRend = cube.GetComponent<Renderer>();

        // ������� ��������� ��������
        Vector3 vel = Random.onUnitSphere; // �������� ��������� �������� XYZ
        // Random.onUnitShere ���������� ������, ����������� �� ��������� �����, ����������� �� ����������� �����
        // � �������� 1 � � � ������� � ������ ���������
        vel.z = 0;      // ���������� vel �� ��������� XY
        vel.Normalize();// ������������ ������������� ����� Vector3 ������ 1 �
        vel *= Random.Range(driftMinMax.x, driftMinMax.y);
        rigid.linearVelocity = vel;

        // ���������� ���� �������� ����� �������� ������� ������ R:[0, 0, 0]
        transform.rotation = Quaternion.identity;
        // Quaternion.identity ���������� ���������� ��������.

        // ������� ��������� �������� �������� ��� ���������� ���� � �������������� rotMinMax.x � rotMinMax.y
        rotPerSecond = new Vector3(Random.Range(rotMinMax.x, rotMinMax.y),
                                   Random.Range(rotMinMax.x, rotMinMax.y),
                                   Random.Range(rotMinMax.x, rotMinMax.y));

        birthTime = Time.time;
    }

    void Update()
    {
        cube.transform.rotation = Quaternion.Euler(rotPerSecond * Time.time);

        // ������ ����������� ���� PowerUp � �������� �������
        // �� ���������� �� ��������� ����� ���������� 10 ������, � ����� ������������ � ������� 4 ������.
        float u = (Time.time - (birthTime + lifeTime)) / fadeTime;
        // � ������� lifeTime ������ �������� "u" ����� <= 0.
        // ����� ��� ������ ������������� � ����� fadeTime ������ ������ ������ 1.

        // ���� "u" >= 1, ���������� �����
        if (u >= 1)
        {
            Destroy(this.gameObject);
            return;
        }

        // ������������ "u" ��� ����������� �����-�������� ���� � �����
        if (u > 0)
        {
            Color c = cubeRend.material.color;
            c.a = 1f - u;
            cubeRend.material.color = c;
            // ����� ���� ������ ������������, �� ���������
            c = letter.color;
            c.a = 1f - (u * 0.5f);
            letter.color = c;
        }

        if (!bndCheck.isOnScreen)
        {
            // ���� ����� ��������� ����� �� ������� ������, ���������� ��� 
            Destroy(gameObject);
        }
    }

    public void SetType(WeaponType wt)
    {
        // �������� WeaponDefinition �� Main
        WeaponDefinition def = Main.GetWeaponDefinition(wt);
        // ���������� ���� ��������� ����
        cubeRend.material.color = def.color;
        letter.text = def.letter;  // ����� ���� ����� �������� � ��� �� ����
        letter.text = def.letter;     // ���������� ������������ �����
        type = wt; // � ���������� ���������� ����������� ���
    }

    public void AbsorbedBy(GameObject target)
    {
        // ��� ������� ���������� ������� Hero, ����� ����� ��������� �����
        // ����� ���� �� ����������� ������ ���������� ������, �������� ���
        // ������� � ������� ���������� ������, �� ���� ������ ���������
        // this.gameObject
        Destroy(this.gameObject);
    }
}
