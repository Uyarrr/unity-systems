using Mirror;
using UnityEngine;

[RequireComponent(typeof(CastleFiascoActor))]
[RequireComponent(typeof(CastleFiascoProperties))]
public class CastleFiascoCPU : CPUBase
{
    public enum State
    {
        Idle,
        RoamRandomly,
        PickupLife,
        PickupSword,
        ChasePlayer,
        Attack
    }

    // ---------------- ARENA ----------------

    [Header("Arena Bounds")]
    public float arenaHalfWidth = 14f;   // X
    public float arenaHalfLength = 12f;  // Z
    public float roamPadding = 1.2f;
    public Vector3 arenaCenter = new Vector3(0f, 0f, -12f);

    // ---------------- ROAMING ----------------

    [Header("Roaming")]
    public float arriveDist = 1.3f;

    [Header("Natural Movement")]
    public float roamRetargetTimeMin = 0.6f;
    public float roamRetargetTimeMax = 1.2f;
    public float steeringNoiseStrength = 0.35f;
    public float directionSmoothTime = 0.12f;

    // ---------------- VISION ----------------

    [Header("Vision")]
    public float visionRadius = 7f;
    public float chaseRadius = 8f;
    public float attackRange = 2.2f;

    // ---------------- COMBAT ----------------

    [Header("Combat")]
    public float attackCooldown = 1.1f;

    // ---------------- AI DECISION ----------------

    [Header("AI Decision")]
    [Range(0f, 1f)] public float baseChaseChance = 0.6f;
    [Range(0f, 1f)] public float difficultyAggression = 0.5f;
    public float healthFearPerMissing = 0.10f; // 10% per HP missing

    // ---------------- INTERNAL ----------------

    private CastleFiascoActor actor;
    private CastleFiascoProperties props;

    [SyncVar] public State state;

    private Vector3 roamTarget;
    private float nextRoamRetargetTime;

    private PickupBase targetPickup;
    private CastleFiascoProperties targetEnemy;

    private float nextAttackTime;

    // Movement smoothing
    private Vector2 smoothMoveDir;
    private Vector2 smoothMoveVel;

    // ---------------- LIFECYCLE ----------------

    public override void OnStartServer()
    {
        actor = GetComponent<CastleFiascoActor>();
        props = GetComponent<CastleFiascoProperties>();

        PickNewRoamTarget();
        SetState(State.RoamRandomly);
    }

    private void Update()
    {
        if (!isServer) return;
        if (!props.isAlive || props.isPaused || props.isAttacking) return;

        OpportunisticScan();

        switch (state)
        {
            case State.RoamRandomly: TickRoam(); break;
            case State.PickupSword:
            case State.PickupLife: TickPickup(); break;
            case State.ChasePlayer: TickChase(); break;
            case State.Attack: TickAttack(); break;
        }
    }

    // ---------------- STATES ----------------

    private void TickRoam()
    {
        if (Time.time >= nextRoamRetargetTime)
            PickNewRoamTarget();

        MoveTowards(roamTarget);
    }

    private void TickPickup()
    {
        if (!IsPickupValid(targetPickup))
        {
            targetPickup = null;
            SetState(State.RoamRandomly);
            return;
        }

        if (state == State.PickupSword && props.hasWeapon)
        {
            targetPickup = null;
            SetState(State.ChasePlayer);
            return;
        }

        MoveTowards(targetPickup.transform.position);
    }

    private void TickChase()
    {
        if (!props.hasWeapon || targetEnemy == null || !targetEnemy.isAlive)
        {
            SetState(State.RoamRandomly);
            return;
        }

        float d = Vector3.Distance(transform.position, targetEnemy.transform.position);

        if (d <= attackRange)
        {
            SetState(State.Attack);
            return;
        }

        MoveTowards(targetEnemy.transform.position);
    }

    private void TickAttack()
    {
        if (Time.time < nextAttackTime)
        {
            SetState(State.ChasePlayer);
            return;
        }

        actor.Server_Slash();
        nextAttackTime = Time.time + attackCooldown;

        SetState(props.hasWeapon ? State.ChasePlayer : State.RoamRandomly);
    }

    // ---------------- DECISION LOGIC ----------------

    private void OpportunisticScan()
    {
        if (targetPickup != null && !IsPickupValid(targetPickup))
        {
            targetPickup = null;
            SetState(State.RoamRandomly);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, visionRadius);

        // -------- Sword Priority --------
        if (!props.hasWeapon)
        {
            foreach (var h in hits)
            {
                var sword = h.GetComponent<SwordPickup>();
                if (sword != null && sword.IsAvailable())
                {
                    targetPickup = sword;
                    SetState(State.PickupSword);
                    return;
                }
            }
        }

        // -------- Enemy Evaluation --------
        if (props.hasWeapon && state != State.Attack)
        {
            CastleFiascoProperties enemy = FindClosestEnemy();
            if (enemy == null) return;

            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d > chaseRadius) return;

            if (ShouldChase(enemy))
            {
                targetEnemy = enemy;
                SetState(State.ChasePlayer);
            }
        }
    }

    private CastleFiascoProperties FindClosestEnemy()
    {
        CastleFiascoProperties best = null;
        float bestDist = float.MaxValue;

        foreach (var go in CastleFiascoGameManager.Instance.spawnedPlayers)
        {
            if (go == gameObject) continue;

            var p = go.GetComponent<CastleFiascoProperties>();
            if (p == null || !p.isAlive) continue;

            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        return best;
    }

    private bool ShouldChase(CastleFiascoProperties enemy)
    {
        float chance = baseChaseChance;

        if (enemy.hasWeapon)
        {
            int missingHP = props.playerMaxHealth - props.playerHealth;
            chance -= missingHP * healthFearPerMissing;
        }

        chance += difficultyAggression * 0.3f;

        chance = Mathf.Clamp01(chance);
        return Random.value <= chance;
    }

    // ---------------- MOVEMENT ----------------

    private void MoveTowards(Vector3 pos)
    {
        float minX = arenaCenter.x - arenaHalfWidth + roamPadding;
        float maxX = arenaCenter.x + arenaHalfWidth - roamPadding;
        float minZ = arenaCenter.z - arenaHalfLength + roamPadding;
        float maxZ = arenaCenter.z + arenaHalfLength - roamPadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        Vector3 rawDir = pos - transform.position;
        rawDir.y = 0f;

        Vector2 desiredDir = new Vector2(rawDir.x, rawDir.z).normalized;

        // Steering noise (human thumbstick wobble)
        Vector2 noise = Random.insideUnitCircle * steeringNoiseStrength;
        desiredDir = (desiredDir + noise).normalized;

        // Smooth direction blending
        smoothMoveDir = Vector2.SmoothDamp(
            smoothMoveDir,
            desiredDir,
            ref smoothMoveVel,
            directionSmoothTime
        );

        actor.SetMoveInput(smoothMoveDir);
        props.animator.SetBool("isMoving", smoothMoveDir.sqrMagnitude > 0.01f);
    }

    private void StopMovement()
    {
        actor.SetMoveInput(Vector2.zero);
        smoothMoveDir = Vector2.zero;
        smoothMoveVel = Vector2.zero;
        props.animator.SetBool("isMoving", false);
    }

    private void PickNewRoamTarget()
    {
        float minX = arenaCenter.x - arenaHalfWidth + roamPadding;
        float maxX = arenaCenter.x + arenaHalfWidth - roamPadding;
        float minZ = arenaCenter.z - arenaHalfLength + roamPadding;
        float maxZ = arenaCenter.z + arenaHalfLength - roamPadding;

        roamTarget = new Vector3(
            Random.Range(minX, maxX),
            transform.position.y,
            Random.Range(minZ, maxZ)
        );

        nextRoamRetargetTime = Time.time + Random.Range(
            roamRetargetTimeMin,
            roamRetargetTimeMax
        );
    }

    private bool IsPickupValid(PickupBase pickup)
    {
        return pickup != null && pickup.IsAvailable();
    }

    private void SetState(State s)
    {
        if (state == s) return;

        StopMovement();
        state = s;
    }
}
