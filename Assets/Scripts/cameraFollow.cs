using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("--- CIBLE ---")]
    public Transform target;

    [Header("--- RETARD / LISSAGE ---")]
    public bool enableSmoothing = true; 
    [Range(0.01f, 1f)] public float smoothSpeed = 0.125f;

    [Header("--- RÉGLAGES DE SUIVI & OFFSET ---")]
    public Vector3 baseOffset = new Vector3(0, 0, -10);

    [Header("--- REGARDER VERS L'AVANT (LOOK AHEAD) ---")]
    public bool enableLookAhead = true;
    public float lookAheadDistance = 3.0f; // La distance max de décalage devant le joueur
    public float lookAheadSpeed = 2.0f;    // La vitesse à laquelle la caméra passe de gauche à droite

    [Header("--- LIMITES (Optionnel pour la 2D) ---")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // Variables privées
    private float currentLookAheadX;
    private float targetLookAheadX;
    private Rigidbody2D targetRb2D;
    private Rigidbody targetRb3D;

    void Start()
    {
        if (target != null)
        {
            // On essaie de récupérer le Rigidbody du joueur pour connaître son sens de déplacement physique
            targetRb2D = target.GetComponent<Rigidbody2D>();
            targetRb3D = target.GetComponent<Rigidbody>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. GESTION DU LOOK AHEAD (DÉCALAGE DU SENS DE MARCHE)
        if (enableLookAhead)
        {
            float moveDirectionX = 0f;

            // On vérifie si le joueur bouge physiquement (via son Rigidbody 2D ou 3D)
            if (targetRb2D != null) moveDirectionX = targetRb2D.linearVelocity.x;
            else if (targetRb3D != null) moveDirectionX = targetRb3D.linearVelocity.x;

            // Si le joueur avance vers la droite (vitesse positive)
            if (moveDirectionX > 0.1f)
            {
                targetLookAheadX = lookAheadDistance;
            }
            // Si le joueur avance vers la gauche (vitesse négative)
            else if (moveDirectionX < -0.1f)
            {
                targetLookAheadX = -lookAheadDistance;
            }
            // Note : Si le joueur s'arrête, la caméra garde le dernier décalage (style Mario/Donkey Kong). 
            // Si tu veux qu'elle se recentre quand il s'arrête, décoche le commentaire ci-dessous :
            // else { targetLookAheadX = 0f; }

            // Lissage du décalage horizontal pour éviter que la caméra ne donne des coups brusques
            currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadSpeed);
        }
        else
        {
            currentLookAheadX = 0f;
        }

        // 2. CALCUL DE LA POSITION VOULUE
        // On prend l'offset de base et on y ajoute le décalage horizontal (Look Ahead)
        Vector3 finalOffset = baseOffset + new Vector3(currentLookAheadX, 0, 0);
        Vector3 desiredPosition = target.position + finalOffset;
        
        // 3. RETARD / RETARD DÉSACTIVABLE
        Vector3 finalPosition;
        if (enableSmoothing)
        {
            // Effet de retard fluide
            finalPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        }
        else
        {
            // Pas de retard : la caméra est collée mathématiquement au joueur
            finalPosition = desiredPosition;
        }
        
        // 4. APPLICATION DES LIMITES (BOUNDS)
        if (useBounds)
        {
            finalPosition.x = Mathf.Clamp(finalPosition.x, minBounds.x, maxBounds.x);
            finalPosition.y = Mathf.Clamp(finalPosition.y, minBounds.y, maxBounds.y);
        }

        // Appliquer la position finale à la caméra
        transform.position = finalPosition;
    }
}