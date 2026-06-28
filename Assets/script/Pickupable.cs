using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickupable : MonoBehaviour
{
    private Rigidbody rb;

    private Renderer[] renderers;
    private Material[] mats;
    private bool[] originalEmission;

    [HideInInspector] public bool isInFlight = false;
    [HideInInspector] public bool hasScoredThisShot = false;
    [HideInInspector] public Vector3 shootPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        renderers = GetComponentsInChildren<Renderer>(true);
        mats = new Material[renderers.Length];
        originalEmission = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material shared = renderers[i].sharedMaterial;
            if (shared != null)
            {
                originalEmission[i] = shared.IsKeywordEnabled("_EMISSION");
                // instance material so we don't affect other objects
                mats[i] = renderers[i].material;
            }
        }
    }

    public void OnPickup()
    {
        rb.useGravity = false;
        rb.isKinematic = true;
        isInFlight = false;
        hasScoredThisShot = false;
        SetGlow(true);
    }

    public void OnDrop(Vector3 throwForce)
    {
        SetGlow(false);
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.WakeUp();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(throwForce, ForceMode.Impulse);

        isInFlight = throwForce.magnitude > 0.01f;
        hasScoredThisShot = false;
        shootPosition = transform.position;
    }

    public void MarkScored()
    {
        hasScoredThisShot = true;
        isInFlight = false;
    }

    public void MarkShotEnded()
    {
        isInFlight = false;
    }

    public void SetGlow(bool glow)
    {
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;

            if (glow)
            {
                mats[i].EnableKeyword("_EMISSION");
                mats[i].EnableKeyword("EMISSION");

                mats[i].SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 2f);

                if (mats[i].HasProperty("_EmissiveColor"))
                    mats[i].SetColor("_EmissiveColor", new Color(1f, 0.6f, 0.1f) * 2f);

                DynamicGI.SetEmissive(renderers[i], new Color(1f, 0.6f, 0.1f) * 2f);
                renderers[i].UpdateGIMaterials();
            }
            else
            {
                // restore original emission state
                if (!originalEmission[i])
                    mats[i].DisableKeyword("_EMISSION");

                mats[i].SetColor("_EmissionColor", Color.black);

                if (mats[i].HasProperty("_EmissiveColor"))
                    mats[i].SetColor("_EmissiveColor", Color.black);

                DynamicGI.SetEmissive(renderers[i], Color.black);
                renderers[i].UpdateGIMaterials();
            }
        }
    }
}