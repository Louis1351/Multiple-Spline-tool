using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotableObject : MonoBehaviour
{
    [SerializeField]
    private Vector3 addAngle = Vector3.zero;
    [SerializeField]
    private float speed = 0.0f;
    [SerializeField]
    private float coolDown = 0.0f;
    [SerializeField]
    private bool onStart = false;
    void Start()
    {
        if (onStart)
        {
            StartCoroutine(RotateCoroutine());
        }
    }

    private IEnumerator RotateCoroutine()
    {
        float t = 0.0f;
        Vector3 startEuler = transform.rotation.eulerAngles;
        Vector3 currentEuler;

        do
        {
            currentEuler = startEuler + addAngle * t;
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z);
            t += Time.deltaTime * speed;
            yield return null;

        } while (t <= 1.0f);

        currentEuler = startEuler + addAngle;
        transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z);

        yield return new WaitForSeconds(coolDown);
        StartCoroutine(RotateCoroutine());
    }
}
