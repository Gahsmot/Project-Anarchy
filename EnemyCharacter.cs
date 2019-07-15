using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCharacter : MonoBehaviour
{
    public Transform player;
    public Animator enemyAnim;
    public CapsuleCollider enemyCollider;
    private NavMeshAgent nav;

    public float walkRadius = 5;
    public float rangeFromPlayerDetect = 10;
    public float minimumWalkDistance = 3;
    public float stopDistance = 1;

    public int damageToPlayer = 1;

    public float walkTimer;
    private float timeToWalk;

    private float enemyHitTimer = 2;
    private float timeToEnemyHit;

    private float enemyAttackTimer = 2.71f;
    private float timeToEnemyAttack;

    private float distanceToPlayer;

    public Rigidbody[] rbTransform;
    private int transformLength;

    public int enemyHealth;
    public int enemyMaxHealth = 100;

    Camera mainCamera;
    PlayerHealth playerHealthScript;

    SystemManager systemManager;

    [SerializeField] bool isMoving;

    public enum EnemyBehaviour
    {
        enemyFollowing, enemyAttacking, enemyDead
    }
    public EnemyBehaviour enemyBehaviour;
    [Header("Material Dissolve")]
    private float dissolve;
    public float dissolveTime;
    public Renderer[] meshRenderer;

    private void Start ()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealthScript = FindObjectOfType<PlayerHealth>();
        nav = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        systemManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<SystemManager>();
        enemyCollider = GetComponent<CapsuleCollider>();

        stopDistance = nav.stoppingDistance;

        enemyHealth = enemyMaxHealth;

        walkTimer = Random.Range(3, 10);
        timeToWalk = walkTimer;

        timeToEnemyHit = enemyHitTimer;
        timeToEnemyAttack = enemyAttackTimer;

        transformLength = rbTransform.Length;
        for (int i = 0; i < transformLength; i++)
        {
            rbTransform[i].isKinematic = true;
        }

        enemyBehaviour = EnemyBehaviour.enemyFollowing;
    }

    public void DealPlayerDamage ()
    {
        playerHealthScript.TakeDamage(damageToPlayer);
    }

    private void Update()
    {
        timeToEnemyHit += Time.deltaTime;

        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        switch (enemyBehaviour)
        {
            case EnemyBehaviour.enemyFollowing:

                MoveToPlayer();
                break;

            case EnemyBehaviour.enemyAttacking:

                Attack();
                break;

            case EnemyBehaviour.enemyDead:

                StartCoroutine(Dissolve());
                Destroy(gameObject, 5.5f);
                break;
        }
    }

    public void ApplyDamage (int amount, float impactForce, float upliftForce)
    {
        if (enemyHealth <= 0)
        {
            return;
        }

        enemyHealth -= amount;
        enemyHealth = Mathf.Clamp(enemyHealth, 0, enemyMaxHealth);

        if (enemyHealth <= 0)
        {
            OnDeath(impactForce, upliftForce);
        }
    }

    private void OnDeath (float impactForce, float upliftForce)
    {
        enemyBehaviour = EnemyBehaviour.enemyDead;
        enemyAnim.enabled = false;
        nav.isStopped = true;
        enemyCollider.enabled = false;

        foreach (var v in rbTransform)
        {
            v.useGravity = true;
            v.isKinematic = false;
            if (upliftForce > 0)
            {
                v.AddForce(Vector3.up * upliftForce + mainCamera.transform.forward * impactForce);
            }
        }

        systemManager.currentScore += systemManager.enemyDeathScore;

        systemManager.enemiesAlive--;
    }

    public IEnumerator Dissolve()
    {
        yield return new WaitForSeconds(dissolveTime);
        dissolve = Mathf.Lerp(dissolve, 1, Time.deltaTime);
        foreach (var mesh in meshRenderer)
        {
            mesh.material.SetFloat("_dissolveTime", dissolve);
        }
    }

    private void MoveToPlayer()
    {
        nav.SetDestination(player.position);
        enemyAnim.SetBool("Attack", false);
        nav.isStopped = false;

        if(distanceToPlayer <= stopDistance)
        {
            enemyBehaviour = EnemyBehaviour.enemyAttacking;
        }
    }

    private void Attack()
    {
        timeToEnemyAttack += Time.deltaTime;
        nav.isStopped = true;
        enemyAnim.SetBool("Attack", true);
        if (timeToEnemyAttack >= enemyAttackTimer)
        {
            timeToEnemyAttack = 0;
            enemyBehaviour = EnemyBehaviour.enemyFollowing;
        }
    }
}

