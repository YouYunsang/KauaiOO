using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MosquitoAI : MonoBehaviour
{
    private Transform player;
    private enum State { Patrol, Chase, ReturnToRoute }

    [Header("Detection")]
    public float detectRange = 5.0f; //플레이어 탐지 범위
    public float loseRange = 10.0f; //추격 끊김 거리
    public float checkInterval = 0.2f;

    [Header("Patrol Route (Ping-Pong)")]
    public Transform pointA;
    public Transform pointB;
    public float patrolPointTolerance = 0.4f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 3.0f;

    [Header("Route Constraint")]
    public float routeRadius = 18.0f;

    [Header("Component")]
    private NavMeshAgent agent;
    private Animator animator;

    private State state;

    private Transform currentPatrolTarget;
    private Vector3 routeCenter;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;

        currentPatrolTarget = pointA;
        agent.speed = patrolSpeed;
        state = State.Patrol;

        routeCenter = (pointA.position + pointB.position) * 0.5f;
        StartCoroutine(DetectionLoop());
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.ReturnToRoute:
                UpdateReturn();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (currentPatrolTarget == null) return;

        agent.SetDestination(currentPatrolTarget.position);

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }
    }

    IEnumerator DetectionLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            CheckDetection();
            yield return wait;
        }
    }

    private void CheckDetection()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (state != State.Chase)
        {
            if (dist <= detectRange)
            {
                if (dist <= detectRange)
                {
                    StartChase();
                }
            }
        }
        else
        {
            if (dist >= loseRange)
            {
                StopChase();
            }
        }
    }

    private void StartChase()
    {
        state = State.Chase;
        agent.speed = chaseSpeed;
        animator.SetBool("isChasing", state == State.Chase);
    }

    private void UpdateChase()
    {
        if (player == null) return;
        agent.SetDestination(player.transform.position);
    }

    private void StopChase()
    {
        agent.speed = patrolSpeed;

        if (IsOutsideRoute())
        {
            StartReturnToRoute();
        }
        else
        {
            state = State.Patrol;
        }
        animator.SetBool("isChasing", state == State.Chase);
    }

    private bool IsOutsideRoute()
    {
        float d = Vector3.Distance(transform.position, routeCenter);
        return d > routeRadius;
    }

    private void StartReturnToRoute()
    {
        state = State.ReturnToRoute;

        float distanceA = Vector3.Distance(transform.position, pointA.position);
        float distanceB = Vector3.Distance(transform.position, pointB.position);

        currentPatrolTarget = (distanceA <= distanceB) ? pointA : pointB;

        agent.SetDestination(currentPatrolTarget.position);
    }

    private void UpdateReturn()
    {
        if (currentPatrolTarget == null) return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
        {
            state = State.Patrol;
        }
    }
}
