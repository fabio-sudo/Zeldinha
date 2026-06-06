using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SlimeIA : MonoBehaviour
{
    private Animator m_Animator;
    public int hp;
    private bool isDead = false;

    [Header("Enemy State")]
    public enemyState state;
    public const float idelWaitTime = 3f;//Tempo de espera no Idle
    public const float patrolWaitTime = 5f;//Tempo de espera no Patrol

    [Header("Enemy IA")]
    [SerializeField] GameManager _gameManager;
    private NavMeshAgent agent;
    private int idWayPoint;
    private Coroutine stateCouritine;

    private void Start()
    {
        m_Animator = GetComponent<Animator>();
        hp = 3;
        isDead = false;

        agent = GetComponent<NavMeshAgent>();

        ChangeState(enemyState.IDLE);//Inicia parado
    }

    IEnumerator Died()
    {
        m_Animator.SetTrigger("DieTrigger");
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    public void GetHit(int amount)
    {
        if (!isDead)
        {
            hp -= amount;
            if (hp <= 0)
            {
                isDead = true;
                StartCoroutine(Died());
            }
            else
            {
                m_Animator.SetTrigger("GetHitTrigger");
            }
        }
    }

    private void ChangeState(enemyState newState)
    {
        if (isDead) return;

        if (stateCouritine != null)
            StopCoroutine(stateCouritine);

        state = newState;

        switch (state)
        {
            case enemyState.IDLE:
                stateCouritine = StartCoroutine(IDLE());
                break;

            case enemyState.PATROL:
                stateCouritine = StartCoroutine(PATROL());
                break;

            default:
                stateCouritine = StartCoroutine(IDLE());
                break;

        }

    }

    private IEnumerator IDLE()//Parado
    {
        agent.isStopped = true;

        //Parar de Andar
        if (m_Animator != null)
        {
            m_Animator.SetBool("isWalk", false);
        }

        yield return new WaitForSeconds(idelWaitTime);

        ChangeState(enemyState.PATROL);
    }

    private IEnumerator PATROL()//Patrulhando
    {
        agent.isStopped = false;

        if (m_Animator != null)
        {
            m_Animator.SetBool("isWalk", true);
        }


        EscolherNovoDestino();//Movimentar para um destino aleatório


        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) 
        {
            yield return null;
        }

        m_Animator.SetBool("isWalk", false);

        yield return new WaitForSeconds(patrolWaitTime);
        ChangeState(enemyState.IDLE);
    }

    private void EscolherNovoDestino()
    {
        if(_gameManager.slimeWayPoints == null || _gameManager.slimeWayPoints.Length == 0)
        {
            ChangeState(enemyState.IDLE);
            return;
        }

        idWayPoint = Random.Range(0, _gameManager.slimeWayPoints.Length);
        agent.SetDestination(_gameManager.slimeWayPoints[idWayPoint].position);

    }//Método Que vai escolher onde o enemy vai


}