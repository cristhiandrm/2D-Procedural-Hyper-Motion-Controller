using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMovement), typeof(Rigidbody2D))]
public class ProceduralAnimator : MonoBehaviour
{
    // Componentes
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    // --- Referencias Visuales ---
    [Header("Visuals")]
    [SerializeField] private Transform torsoVisual;
    [SerializeField] private Transform legRVisual;
    [SerializeField] private Transform legLVisual;
    [SerializeField] private Transform headVisual;
    [SerializeField] private Transform upperArmRVisual;
    [SerializeField] private Transform lowerArmRVisual;
    [SerializeField] private Transform upperArmLVisual;
    [SerializeField] private Transform lowerArmLVisual;

    // --- Renderers ---
    private SpriteRenderer torsoRenderer, legRRenderer, legLRenderer, headRenderer;
    private SpriteRenderer upperArmRRenderer, lowerArmRRenderer, upperArmLRenderer, lowerArmLRenderer;

    // --- Parámetros de Animación ---
    [Header("Tuning")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float verticalStretchFactor = 0.05f;
    [SerializeField] private float landSquashAmount = 0.4f;
    [SerializeField] private float squashReturnSpeed = 10f;
    [SerializeField] private float walkCycleSpeed = 15f;
    [SerializeField] private float walkCycleAmount = 0.1f;
    [SerializeField] private float walkCycleWidth = 0.15f;
    [SerializeField] private float armSwingAngle = 25f;
    [SerializeField] private float walkElbowFlexAngle = 10f;
    // <<< NUEVO >>>
    [Tooltip("Cantidad de 'retraso' vertical de los brazos al saltar/caer (ej: 0.2-0.5)")]
    [SerializeField] private float jumpArmLagAmount = 0.3f;


    // --- Parámetros de Ataque ---
    [Header("Attack")]
    [Tooltip("Ángulo del HOMBRO en la anticipación")]
    [SerializeField] private float attackShoulderAnticipation = -45f;
    [Tooltip("Ángulo del CODO en la anticipación (más doblado)")]
    [SerializeField] private float attackElbowAnticipation = 45f;

    [Tooltip("Ángulo del HOMBRO en el 'slash'")]
    [SerializeField] private float attackShoulderSwing = 110f;
    [Tooltip("Ángulo del CODO en el 'slash' (casi recto)")]
    [SerializeField] private float attackElbowSwing = 10f;

    [SerializeField] private float attackAnticipationDuration = 0.15f;
    [SerializeField] private float attackSwingDuration = 0.1f;
    [SerializeField] private float attackCooldown = 0.3f;

    // --- Parámetros de Ordenación ---
    [Header("Sorting")]
    [SerializeField] private int farOrder = 1;
    [SerializeField] private int middleOrder = 3;
    [SerializeField] private int nearOrder = 5;

    // --- Posiciones y Rotaciones de Reposo ---
    private Vector3 defaultTorsoPos, defaultLegRPos, defaultLegLPos, defaultHeadPos;
    private Vector3 defaultUpperArmRPos, defaultLowerArmRPos, defaultUpperArmLPos, defaultLowerArmLPos;
    private Quaternion defaultUpperArmRRot, defaultLowerArmRRot, defaultUpperArmLRot, defaultLowerArmLRot;
    private Vector3 defaultElbowOffsetR, defaultElbowOffsetL;

    // --- Estado Interno ---
    private Vector3 torsoVel, legRVel, legLVel, headVel;
    private Vector3 upperArmRVel, lowerArmRVel, upperArmLVel, lowerArmLVel;
    private bool wasGrounded = true;
    private float currentLandSquash = 0;

    // --- Estado de Rotación ---
    private float currentUpperArmRAngle, currentLowerArmRAngle, currentUpperArmLAngle, currentLowerArmLAngle;
    private float upperArmAngleVelR, lowerArmAngleVelR, upperArmAngleVelL, lowerArmAngleVelL;

    // --- Estado de Ataque ---
    private bool isAttacking = false;
    private float targetUpperArmRAngle, targetLowerArmRAngle, targetUpperArmLAngle, targetLowerArmLAngle;


    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();

        defaultTorsoPos = torsoVisual.localPosition;
        defaultLegRPos = legRVisual.localPosition;
        defaultLegLPos = legLVisual.localPosition;
        defaultHeadPos = headVisual.localPosition;
        defaultUpperArmRPos = upperArmRVisual.localPosition;
        defaultLowerArmRPos = lowerArmRVisual.localPosition;
        defaultUpperArmLPos = upperArmLVisual.localPosition;
        defaultLowerArmLPos = lowerArmLVisual.localPosition;

        defaultUpperArmRRot = upperArmRVisual.localRotation;
        defaultLowerArmRRot = lowerArmRVisual.localRotation;
        defaultUpperArmLRot = upperArmLVisual.localRotation;
        defaultLowerArmLRot = lowerArmLVisual.localRotation;

        defaultElbowOffsetR = defaultLowerArmRPos - defaultUpperArmRPos;
        defaultElbowOffsetL = defaultLowerArmLPos - defaultUpperArmLPos;

        torsoRenderer = torsoVisual.GetComponent<SpriteRenderer>();
        legRRenderer = legRVisual.GetComponent<SpriteRenderer>();
        legLRenderer = legLVisual.GetComponent<SpriteRenderer>();
        headRenderer = headVisual.GetComponent<SpriteRenderer>();
        upperArmRRenderer = upperArmRVisual.GetComponent<SpriteRenderer>();
        lowerArmRRenderer = lowerArmRVisual.GetComponent<SpriteRenderer>();
        upperArmLRenderer = upperArmLVisual.GetComponent<SpriteRenderer>();
        lowerArmLRenderer = lowerArmLVisual.GetComponent<SpriteRenderer>();

        targetUpperArmRAngle = defaultUpperArmRRot.eulerAngles.z;
        targetLowerArmRAngle = defaultLowerArmRRot.eulerAngles.z;
        targetUpperArmLAngle = defaultUpperArmLRot.eulerAngles.z;
        targetLowerArmLAngle = defaultLowerArmLRot.eulerAngles.z;
    }

    void OnEnable() { playerMovement.OnAttack += HandleAttack; }
    void OnDisable() { playerMovement.OnAttack -= HandleAttack; }

    private void HandleAttack()
    {
        if (isAttacking) return;
        StartCoroutine(MeleeAttackRoutine());
    }

    void Update()
    {
        // --- 1. Calcular Posiciones Objetivo (Anchors) ---
        Vector3 targetTorsoPos = defaultTorsoPos;
        Vector3 targetLegRPos = defaultLegRPos;
        Vector3 targetLegLPos = defaultLegLPos;
        Vector3 targetHeadPos = defaultHeadPos;
        Vector3 targetUpperArmRPos = defaultUpperArmRPos;
        Vector3 targetUpperArmLPos = defaultUpperArmLPos;

        // --- 2. Squash de Aterrizaje ---
        if (playerMovement.IsGrounded && !wasGrounded) { currentLandSquash = landSquashAmount; }
        else { currentLandSquash = Mathf.Lerp(currentLandSquash, 0, Time.deltaTime * squashReturnSpeed); }

        targetTorsoPos.y -= currentLandSquash;
        targetHeadPos.y -= currentLandSquash * 0.5f;
        targetUpperArmRPos.y -= currentLandSquash * 0.7f;
        targetUpperArmLPos.y -= currentLandSquash * 0.7f;

        // --- 3. Stretch Vertical (en el aire) ---
        // <<< INICIO DEL CAMBIO >>>
        if (!playerMovement.IsGrounded)
        {
            float stretch = rb.linearVelocity.y * verticalStretchFactor;
            targetTorsoPos.y += stretch;
            targetLegRPos.y -= stretch;
            targetLegLPos.y -= stretch;
            targetHeadPos.y += stretch * 0.7f;

            // Los brazos ahora tienen un "lag" vertical distinto
            targetUpperArmRPos.y += stretch * jumpArmLagAmount; // Usamos el nuevo parámetro
            targetUpperArmLPos.y += stretch * jumpArmLagAmount; // Usamos el nuevo parámetro
        }
        // <<< FIN DEL CAMBIO >>>

        // --- 4. Ciclo de Caminar / Ataque ---
        if (!isAttacking)
        {
            if (playerMovement.MoveInput != 0 && playerMovement.IsGrounded)
            {
                // -- Lógica de Caminar --
                float walkCycleTime = Time.time * walkCycleSpeed;

                // Piernas
                targetLegRPos.y += Mathf.Sin(walkCycleTime) * walkCycleAmount;
                targetLegLPos.y += Mathf.Sin(walkCycleTime + Mathf.PI) * walkCycleAmount;
                targetLegRPos.x += Mathf.Cos(walkCycleTime) * walkCycleWidth;
                targetLegLPos.x += Mathf.Cos(walkCycleTime + Mathf.PI) * walkCycleWidth;

                // Brazos (Vertical)
                targetUpperArmRPos.y += Mathf.Cos(walkCycleTime + Mathf.PI) * walkCycleAmount * 0.5f;
                targetUpperArmLPos.y += Mathf.Cos(walkCycleTime) * walkCycleAmount * 0.5f;

                // Brazos (Rotación)
                targetUpperArmRAngle = defaultUpperArmRRot.eulerAngles.z + Mathf.Cos(walkCycleTime) * armSwingAngle;
                targetUpperArmLAngle = defaultUpperArmLRot.eulerAngles.z + Mathf.Cos(walkCycleTime + Mathf.PI) * armSwingAngle;

                targetLowerArmRAngle = targetUpperArmRAngle + (defaultLowerArmRRot.eulerAngles.z - defaultUpperArmRRot.eulerAngles.z);
                targetLowerArmRAngle += Mathf.Sin(walkCycleTime) * walkElbowFlexAngle;

                targetLowerArmLAngle = targetUpperArmLAngle + (defaultLowerArmLRot.eulerAngles.z - defaultUpperArmLRot.eulerAngles.z);
                targetLowerArmLAngle += Mathf.Sin(walkCycleTime + Mathf.PI) * walkElbowFlexAngle;
            }
            else
            {
                // -- Lógica de Inactividad (volver a reposo) --
                targetUpperArmRAngle = defaultUpperArmRRot.eulerAngles.z;
                targetLowerArmRAngle = defaultLowerArmRRot.eulerAngles.z;
                targetUpperArmLAngle = defaultUpperArmLRot.eulerAngles.z;
                targetLowerArmLAngle = defaultLowerArmLRot.eulerAngles.z;
            }
        }

        // --- 5. Aplicar Movimiento Suave (Posición) ---
        torsoVisual.localPosition = Vector3.SmoothDamp(torsoVisual.localPosition, targetTorsoPos, ref torsoVel, followSmoothTime);
        legRVisual.localPosition = Vector3.SmoothDamp(legRVisual.localPosition, targetLegRPos, ref legRVel, followSmoothTime);
        legLVisual.localPosition = Vector3.SmoothDamp(legLVisual.localPosition, targetLegLPos, ref legLVel, followSmoothTime);
        headVisual.localPosition = Vector3.SmoothDamp(headVisual.localPosition, targetHeadPos, ref headVel, followSmoothTime);
        upperArmRVisual.localPosition = Vector3.SmoothDamp(upperArmRVisual.localPosition, targetUpperArmRPos, ref upperArmRVel, followSmoothTime);
        upperArmLVisual.localPosition = Vector3.SmoothDamp(upperArmLVisual.localPosition, targetUpperArmLPos, ref upperArmLVel, followSmoothTime);

        // --- 6. Aplicar Movimiento Suave (Rotación) ---
        float lowerArmSmoothTime = isAttacking ? (followSmoothTime * 0.5f) : followSmoothTime;

        currentUpperArmRAngle = Mathf.SmoothDampAngle(currentUpperArmRAngle, targetUpperArmRAngle, ref upperArmAngleVelR, followSmoothTime);
        currentUpperArmLAngle = Mathf.SmoothDampAngle(currentUpperArmLAngle, targetUpperArmLAngle, ref upperArmAngleVelL, followSmoothTime);

        currentLowerArmRAngle = Mathf.SmoothDampAngle(currentLowerArmRAngle, targetLowerArmRAngle, ref lowerArmAngleVelR, lowerArmSmoothTime);
        currentLowerArmLAngle = Mathf.SmoothDampAngle(currentLowerArmLAngle, targetLowerArmLAngle, ref lowerArmAngleVelL, lowerArmSmoothTime);

        upperArmRVisual.localRotation = Quaternion.Euler(0, 0, currentUpperArmRAngle);
        upperArmLVisual.localRotation = Quaternion.Euler(0, 0, currentUpperArmLAngle);
        lowerArmRVisual.localRotation = Quaternion.Euler(0, 0, currentLowerArmRAngle);
        lowerArmLVisual.localRotation = Quaternion.Euler(0, 0, currentLowerArmLAngle);

        // --- 7. Recalcular Posición del Antebrazo (para que quede pegado al codo) ---
        lowerArmRVisual.localPosition = upperArmRVisual.localPosition + (upperArmRVisual.localRotation * defaultElbowOffsetR);
        lowerArmLVisual.localPosition = upperArmLVisual.localPosition + (upperArmLVisual.localRotation * defaultElbowOffsetL);

        // --- 8. Actualizar Sorting Order ---
        UpdateSortingOrder();

        // --- 9. Guardar estado para el próximo frame ---
        wasGrounded = playerMovement.IsGrounded;
    }

    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;

        bool isRightArm = playerMovement.IsFacingRight;

        float startUpperArmAngle = isRightArm ? currentUpperArmRAngle : currentUpperArmLAngle;
        float startLowerArmAngle = isRightArm ? currentLowerArmRAngle : currentLowerArmLAngle;

        // --- 1. Anticipación ---
        float timer = 0f;
        float targetShoulder = attackShoulderAnticipation;
        float targetElbow = targetShoulder + attackElbowAnticipation;

        while (timer < attackAnticipationDuration)
        {
            float t = timer / attackAnticipationDuration;
            if (isRightArm)
            {
                targetUpperArmRAngle = Mathf.LerpAngle(startUpperArmAngle, targetShoulder, t);
                targetLowerArmRAngle = Mathf.LerpAngle(startLowerArmAngle, targetElbow, t);
            }
            else
            {
                targetUpperArmLAngle = Mathf.LerpAngle(startUpperArmAngle, targetShoulder, t);
                targetLowerArmLAngle = Mathf.LerpAngle(startLowerArmAngle, targetElbow, t);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // --- 2. Slash ---
        timer = 0f;
        startUpperArmAngle = isRightArm ? currentUpperArmRAngle : currentUpperArmLAngle;
        startLowerArmAngle = isRightArm ? currentLowerArmRAngle : currentLowerArmLAngle;
        targetShoulder = attackShoulderSwing;
        targetElbow = targetShoulder + attackElbowSwing;

        while (timer < attackSwingDuration)
        {
            float t = timer / attackSwingDuration;
            if (isRightArm)
            {
                targetUpperArmRAngle = Mathf.LerpAngle(startUpperArmAngle, targetShoulder, t);
                targetLowerArmRAngle = Mathf.LerpAngle(startLowerArmAngle, targetElbow, t);
            }
            else
            {
                targetUpperArmLAngle = Mathf.LerpAngle(startUpperArmAngle, targetShoulder, t);
                targetLowerArmLAngle = Mathf.LerpAngle(startLowerArmAngle, targetElbow, t);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // --- 3. Cooldown ---
        float totalDuration = attackAnticipationDuration + attackSwingDuration;
        if (totalDuration < attackCooldown)
        {
            yield return new WaitForSeconds(attackCooldown - totalDuration);
        }

        // --- 4. Finalizar Ataque ---
        isAttacking = false;
    }

    private void UpdateSortingOrder()
    {
        if (playerMovement.IsFacingRight)
        {
            SetSortingOrder(upperArmLRenderer, farOrder);
            SetSortingOrder(lowerArmLRenderer, farOrder);
            SetSortingOrder(legLRenderer, farOrder + 1);
            SetSortingOrder(torsoRenderer, middleOrder);
            SetSortingOrder(headRenderer, middleOrder + 1);
            SetSortingOrder(legRRenderer, nearOrder);
            SetSortingOrder(upperArmRRenderer, nearOrder + 1);
            SetSortingOrder(lowerArmRRenderer, nearOrder + 1);
        }
        else
        {
            SetSortingOrder(upperArmRRenderer, farOrder);
            SetSortingOrder(lowerArmRRenderer, farOrder);
            SetSortingOrder(legRRenderer, farOrder + 1);
            SetSortingOrder(torsoRenderer, middleOrder);
            SetSortingOrder(headRenderer, middleOrder + 1);
            SetSortingOrder(legLRenderer, nearOrder);
            SetSortingOrder(upperArmLRenderer, nearOrder + 1);
            SetSortingOrder(lowerArmLRenderer, nearOrder + 1);
        }
    }

    private void SetSortingOrder(SpriteRenderer renderer, int order)
    {
        if (renderer != null)
        {
            renderer.sortingOrder = order;
        }
    }
}