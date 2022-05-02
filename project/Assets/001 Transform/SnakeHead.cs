using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeHead : MonoBehaviour
{
    public int Count = 200;
    void Awake()
    {
        var transforms = new List<Transform>() {transform};
        for (int i = 0; i < Count; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = transforms[i].transform;
            cube.transform.localPosition = new Vector3(0, 0, -1);
            var snake = cube.AddComponent<Snake>();
            snake.PositionDamping = 0.2f;
            snake.RotationDamping = 0.05f;
            transforms.Add(cube.transform);
        }
    }

    void Update()
    {
        float time = Time.time * 10.0f;
        Vector3 position = transform.localPosition;
        position.y = 10 * Wave(0, time * 0.2f);
        position.z = 10 * Wave(0, time * 0.1f);
        position.x = 10 * Wave(0, time * 0.05f);
        transform.localPosition = position;
        transform.localRotation = Quaternion.LookRotation(-position, Vector3.up);
    }

    public static float Wave(float x, float t)
    {
        float y = Mathf.Sin(Mathf.PI * (x + 0.5f * t));
        y += 0.5f * Mathf.Sin(2f * Mathf.PI * (x + t));
        return y;
    }

}
