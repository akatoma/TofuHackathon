using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 720f;

    Rigidbody rb;
    Vector3 inputDirection = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        inputDirection = new Vector3(h, 0f, v);

        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(inputDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        Vector3 move = inputDirection.normalized * moveSpeed * Time.fixedDeltaTime;
        if (rb != null)
        {
            rb.MovePosition(rb.position + move);
        }
        else
        {
            transform.Translate(move, Space.World);
        }
    }
}
