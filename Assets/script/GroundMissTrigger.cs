// GroundMissTrigger.cs — unchanged
using UnityEngine;

public class GroundMissTrigger : MonoBehaviour
{
    public GameManager scorer;

    void OnCollisionEnter(Collision collision) => BallTouch(collision.collider);
    void OnTriggerEnter(Collider other)        => BallTouch(other);

    private void BallTouch(Collider other)
    {
        Pickupable p = other.GetComponent<Pickupable>()
                    ?? other.GetComponentInParent<Pickupable>();

        if (p != null && p.isInFlight && !p.hasScoredThisShot)
        {
            p.MarkShotEnded();
            if (scorer != null) scorer.RegisterMiss();
        }
    }
}