// TargetBounce.cs — unchanged
using System.Collections;
using UnityEngine;

public class TargetBounce : MonoBehaviour
{
    public Transform rimCenter;
    public float rimTime = 0.25f;

    void OnCollisionEnter(Collision collision)
    {
        Pickupable p = collision.collider.GetComponent<Pickupable>();
        if (p == null) p = collision.collider.GetComponentInParent<Pickupable>();

        if (p != null)
        {
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null && rimCenter != null)
                StartCoroutine(Bounce(rb));
        }
    }

    private IEnumerator Bounce(Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        if (rb == null) yield break;

        Vector3 targetPos = rimCenter.position + Vector3.down * 0.5f;
        Vector3 toRim = targetPos - rb.position;
        float time = rimTime * 1.8f;

        // calc velocity to rim
        Vector3 velocity = new Vector3(toRim.x / time, 0, toRim.z / time);
        velocity.y = (toRim.y - 0.5f * Physics.gravity.y * time * time) / time;

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
    }
}