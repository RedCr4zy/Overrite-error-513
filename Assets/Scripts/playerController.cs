using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum GameType { Is3D, Is2D_SideScroller, Is2D_TopDown }
    
    [Header("--- CONFIGURATION DU JEU ---")]
    public GameType viewMode = GameType.Is3D;

    [Header("--- FONCTIONNALITÉS ACTIVÉES ---")]
    public bool enableMovement = true;
    public bool enableJump = true;
    public bool enableCrouch = true;
    public bool enableHealth = true;
    public bool enableAttack = true;

    [Header("--- PARAMÈTRES DE MOUVEMENT ---")]
    public float walkSpeed = 6.0f;
    public float crouchSpeed = 3.0f;
    
    [Header("--- PARAMÈTRES DE SAUT ---")]
    public float jumpForce = 7.0f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("--- PARAMÈTRES DE VIE ---")]
    public int maxHealth = 100;
    public int currentHealth;

    // Variables privées de calcul
    private Rigidbody rb3D;
    private Rigidbody2D rb2D;
    private Vector3 moveDirection3D;
    private Vector2 moveDirection2D;
    private bool isGrounded;
    private bool isCrouching;
    private float currentSpeed;
    private Animator playerAnimator;
    private bool isFacingRight = true;
    private int comboStep = 0;
    private float lastClickTime;
    public float comboResetDelay = 0.8f;
    

    void Start()
    {
        currentSpeed = walkSpeed;
        currentHealth = maxHealth;
        playerAnimator = GetComponentInChildren<Animator>();


        // Récupération automatique des composants selon le mode
        if (viewMode == GameType.Is3D)
        {
            rb3D = GetComponent<Rigidbody>();
            if (rb3D == null) rb3D = gameObject.AddComponent<Rigidbody>();
            // Bloquer les rotations pour éviter que le joueur ne tombe comme une quille
            rb3D.freezeRotation = true; 
        }
        else
        {
            rb2D = GetComponent<Rigidbody2D>();
            rb2D ??= gameObject.AddComponent<Rigidbody2D>();
            rb2D.freezeRotation = true;
            
            // Si TopDown, on coupe la gravité de base
            if (viewMode == GameType.Is2D_TopDown) rb2D.gravityScale = 0f;
        }
    }

    void Update()
    {
        // 1. GESTION DES ENTRÉES (ZQSD / Flèches de base via Input Manager)
        float x = Input.GetAxisRaw("Horizontal"); // Q / D
        float z = Input.GetAxisRaw("Vertical");   // Z / S

        // 2. GESTION DU CROUCH (S'accroupir)
        if (enableCrouch)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl)) StartCrouch();
            if (Input.GetKeyUp(KeyCode.LeftControl)) StopCrouch();
        }

        // 3. ENREGISTREMENT DES DIRECTIONS SELON LA VUE
        if (enableMovement)
        {
            if (viewMode == GameType.Is3D)
            {
                moveDirection3D = (transform.right * x + transform.forward * z).normalized;
            }
            else if (viewMode == GameType.Is2D_TopDown)
            {
                moveDirection2D = new Vector2(x, z).normalized;
            }
            else if (viewMode == GameType.Is2D_SideScroller)
            {
                moveDirection2D = new Vector2(x, 0); // Pas de déplacement vertical direct en SideScroller
            }
        }

        // 4. GESTION DU SAUT
        if (enableJump && Input.GetButtonDown("Jump") && CheckIfGrounded())
        {
            Jump();
        }

        // 5. GESTYON DE L'ATTAQUE
        bool canAttack = true;
        if (playerAnimator != null)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            canAttack = !stateInfo.IsName("Attack");

            if (Time.time - lastClickTime > comboResetDelay)
            {
                comboStep = 0;
            }
        }


        if (enableAttack && Input.GetButtonDown("Attack") && canAttack)
        {
            lastClickTime = Time.time;
            comboStep++;

            if (comboStep > 3) comboStep = 1;

            playerAnimator?.SetInteger("ComboStep", comboStep);
            playerAnimator?.SetTrigger("Attack");
        }

        // 6. ANIMATION DEPLACMENT
        if (playerAnimator != null)
        {
            bool isWalking = (x != 0);
            
            if (viewMode == GameType.Is2D_TopDown) isWalking = (x != 0 || z != 0);
            

            playerAnimator.SetBool("IsWalking", isWalking);

            if(isWalking)
            {
                float speedRatio = currentSpeed / walkSpeed;
                playerAnimator.speed = speedRatio;
            }
            else
            {
                playerAnimator.speed = 1f;
            }
        }

        // 7. GESTION DE FLIP
        if (viewMode != GameType.Is3D)
        {
            if (x > 0 && !isFacingRight)
            {
                Flip();
            }
            else if (x < 0 && isFacingRight)
            {
                Flip();
            }
        }
    }

    void FixedUpdate()
    {
        // Application physique des mouvements
        if (!enableMovement) return;

        if (viewMode == GameType.Is3D)
        {
            Vector3 targetVelocity = moveDirection3D * currentSpeed;
            targetVelocity.y = rb3D.linearVelocity.y; // Conserve la gravité (Note 2026 : En Unity récent, 'velocity' est souvent 'linearVelocity')
            rb3D.linearVelocity = targetVelocity;
        }
        else // Modes 2D
        {
            if (viewMode == GameType.Is2D_TopDown)
            {
                rb2D.linearVelocity = moveDirection2D * currentSpeed;
            }
            else if (viewMode == GameType.Is2D_SideScroller)
            {
                rb2D.linearVelocity = new Vector2(moveDirection2D.x * currentSpeed, rb2D.linearVelocity.y);
            }
        }
    }

    // --- FONCTIONS LOGIQUES ---

    private bool CheckIfGrounded()
    {
        if (viewMode == GameType.Is2D_TopDown) return true; // Pas de saut/sol en vue du dessus de base

        if (groundCheck == null) return false;

        if (viewMode == GameType.Is3D)
            return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        else
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Jump()
    {
        if (viewMode == GameType.Is3D)
            rb3D.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        else if (viewMode == GameType.Is2D_SideScroller)
            rb2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void StartCrouch()
    {
        isCrouching = true;
        currentSpeed = crouchSpeed;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.5f, transform.localScale.z); // S'écrase de moitié
    }

    private void StopCrouch()
    {
        isCrouching = false;
        currentSpeed = walkSpeed;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 2f, transform.localScale.z); // Reprend sa taille
    }

    // --- SYSTÈME DE VIE ---
    public void TakeDamage(int damage)
    {
        if (!enableHealth) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("Le joueur est mort !");
        // Ajoutez ici votre logique de game over ou respawn
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }
}