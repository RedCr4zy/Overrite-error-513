using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("--- CIBLE ---")]
    public Transform target;

    [Header("--- RETARD / LISSAGE (FAIBLE = NERVEUX) ---")]
    public bool enableSmoothing = true; 
    [Tooltip("Le temps de réponse en secondes. Plus la valeur est petite (ex: 0.05), plus le blocage de fin est sec et rapide.")]
    public float smoothTime = 0.08f;

    [Header("--- RÉGLAGES DE SUIVI & OFFSET ---")]
    public Vector3 baseOffset = new Vector3(0, 0, -10);

    [Header("--- REGARDER VERS L'AVANT (LOOK AHEAD) ---")]
    public bool enableLookAhead = true;
    public float lookAheadDistance = 3.0f; 
    [Tooltip("Temps de transition pour le décalage horizontal (ex: 0.05 = ultra réactif).")]
    public float lookAheadSmoothTime = 0.05f;
[Header("--- LIMITES (Optionnel pour la 2D) ---")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // Variables privées de contrôle mécanique
    private float currentLookAheadX;
    private float targetLookAheadX;
    private float lookAheadVelocity; // Requis pour le SmoothDamp horizontal
    private Vector3 cameraVelocity;   // Requis pour le SmoothDamp global

    private Rigidbody2D targetRb2D;
    private Rigidbody targetRb3D;

    void Start()
    {
        if (target != null)
        {
            targetRb2D = target.GetComponent<Rigidbody2D>();
            targetRb3D = target.GetComponent<Rigidbody>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. GESTION DU LOOK AHEAD SANS LERP (PLUS DE MOLLESSE)
        if (enableLookAhead)
        {
            float moveDirectionX = 0f;

            if (targetRb2D != null) moveDirectionX = targetRb2D.linearVelocity.x;
            else if (targetRb3D != null) moveDirectionX = targetRb3D.linearVelocity.x;

            if (moveDirectionX > 0.1f)
            {
                targetLookAheadX = lookAheadDistance;
            }
            else if (moveDirectionX < -0.1f)
            {
                targetLookAheadX = -lookAheadDistance;
            }

            // Utilisation du SmoothDamp ici pour une transition de visée ultra-sèche
            currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref lookAheadVelocity, lookAheadSmoothTime);
        }
        else
        {
            currentLookAheadX = 0f;
        }

        // 2. CALCUL DE LA POSITION VOULUE
        Vector3 finalOffset = baseOffset + new Vector3(currentLookAheadX, 0, 0);
        Vector3 desiredPosition = target.position + finalOffset;

        // 3. ENCLENCHEMENT DU COMPORTEMENT RESSORT ACCÉLÉRÉ
Vector3 finalPosition;
        if (enableSmoothing)
        {
            // La caméra fonce sur la cible et se stoppe d'un coup net
            finalPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, smoothTime);
        }
        else
        {
            finalPosition = desiredPosition;
        }

        // 4. APPLICATION DES LIMITES (BOUNDS)
        if (useBounds)
        {
            finalPosition.x = Mathf.Clamp(finalPosition.x, minBounds.x, maxBounds.x);
            finalPosition.y = Mathf.Clamp(finalPosition.y, minBounds.y, maxBounds.y);
        }

        // Application sur la caméra
        transform.position = finalPosition;
    }
}
