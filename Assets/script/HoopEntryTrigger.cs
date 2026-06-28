// HoopEntryTrigger.cs — unchanged
using UnityEngine;

public class HoopEntryTrigger : MonoBehaviour
{
    public GameManager scorer;

    void OnTriggerEnter(Collider other)
    {
        Pickupable p = other.GetComponent<Pickupable>();
        if (p == null) p = other.GetComponentInParent<Pickupable>();

        if (p != null && scorer != null && p.isInFlight && !p.hasScoredThisShot)
            scorer.RegisterScore(p, transform);
    }
}