using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class FirstComponent : MonoBehaviour
{
    // public int FirstVariable;       // 类变量 (Field)，int类型，公有
    // private float SecondVariable;   // 类变量 (Field)，float类型，私有
    public Vector3 ThirdVariable;   // 类变量 (Field)，Vector3类型，公有
    public float Lerp;

    // Start is called before the first frame update
    void Start()
    {
        /*ThirdVariable = new Vector3(1, 2, 3);   // 变量赋值
        SecondVariable = 2.0f;  // 变量赋值
        Debug.Log(FirstVariable);
        Debug.Log(SecondVariable);
        Debug.Log(ThirdVariable);

        int FourthVariable = Add(3, SecondVariable);
        Debug.Log(FourthVariable);

        FirstComponent comp;
        comp = GetComponent<FirstComponent>();
        Debug.Log(comp.FirstVariable);*/

        Debug.Log(transform.position);
        transform.position = ThirdVariable;
        Debug.Log(transform.position);

        for (float i = 0; i < 2.0; i = i + 0.1f)
        {
            Debug.Log(i);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = CornerLine(i);
        }
    }

    public int Add(int a, float b)
    {
        return a + (int) b;
    }

    void Update()
    {
        transform.position = CornerLine(Lerp);
    }

    public Vector3 CornerLine(float lerp)
    {
        if (lerp < 1)
        {
            return Vector3.Lerp(Vector3.zero, new Vector3(10, 10, 0), lerp);
        }
        else
        {
            return Vector3.Lerp(new Vector3(10, 10, 0), new Vector3(20, 0, 0), lerp - 1);
        }
    }
}