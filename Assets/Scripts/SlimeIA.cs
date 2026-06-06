using System.Collections;
using Unity.VisualScripting;
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
    private bool isPlayerVisible = false;
    private float loseTimer = 0f;
    public float slimeRotationSpeed = 5f;

    [Header("Enemy Attack")]
    public bool isAttack = false;//Está atancando
    public float attackDelay = 1.5f;//Tempo entre os ataques
    private Coroutine attackCoroutine;//Instancia o ataque


    private void Start()
    {
        m_Animator = GetComponent<Animator>();
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
                agent.isStopped = true;
                m_Animator.SetBool("isWalk", false);
                StartCoroutine(Died());
            }
            else
            {
                m_Animator.SetTrigger("GetHitTrigger");
                ChangeState(enemyState.FURY);//Leva dano e fica furioso
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

            case enemyState.ALERT:
                stateCouritine = StartCoroutine(ALERT());
                break;

            case enemyState.FURY:
                stateCouritine = StartCoroutine(FURY());
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
        agent.speed = 2.0f; //Diminuir a velocidade do Slime
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

    private IEnumerator FURY()
    {
        loseTimer = 0f;
        agent.speed = 3.0f; //Aumenta a velocidade do Slime
        agent.isStopped = false;
        agent.stoppingDistance = _gameManager.slimeDistanceAttack;

        if(m_Animator != null)
        {
            m_Animator.SetBool("isAlert", true);
            m_Animator.SetBool("isWalk", true);
        }

        while(!isDead)
        {
            if(_gameManager.player != null)
            {

                if (!isPlayerVisible)
                {
                    loseTimer += 0.1f;

                    if(loseTimer >= _gameManager.slimeLosePlayerTime)
                    {
                        ChangeState(enemyState.PATROL);
                        yield break;
                    }
                }
                else
                {
                    loseTimer = 0f;
                }


                    float distanceToPlayer =
                        Vector3.Distance(transform.position, _gameManager.player.position);

                if (distanceToPlayer > agent.stoppingDistance)
                {
                    agent.isStopped = false;
                    agent.SetDestination(_gameManager.player.position);
                    m_Animator.SetBool("isWalk", true);
                    m_Animator.SetBool("isAlert", true);
                }
                else
                {
                    m_Animator.SetBool("isWalk", false);
                    agent.isStopped = true;
                    agent.ResetPath();
                    
                    Attack();//realiza o ataque quando estiver próximo o suficiente
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator ALERT()
    {
        agent.isStopped = true;
        agent.ResetPath();

        m_Animator.SetBool("isWalk", false);
        m_Animator.SetBool("isAlert", true);

        float timer = 0f;

        while (timer < _gameManager.slimeAlertTime)
        {
            if (_gameManager.player != null)
            {
                Vector3 direction =
                    _gameManager.player.position - transform.position;

                direction.y = 0; // não inclina para cima/baixo

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(direction);

                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        5f * Time.deltaTime);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        m_Animator.SetBool("isAlert", false);

        if (isPlayerVisible)
            ChangeState(enemyState.FURY);
        else
            ChangeState(enemyState.PATROL);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerVisible = true;

            if(state == enemyState.IDLE || state == enemyState.PATROL)
            {
                ChangeState(enemyState.ALERT);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerVisible = false;
        }
    }

    public void EnemyAttack()//Disparado pelo evento de animação "Script de Animação do Attack2"
    {
        if(attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AttackCooldown());
    }

    public void Attack()
    {
        if(isAttack || isDead) return;

        isAttack = true;
        m_Animator.SetTrigger("AttackTrigger");
        //Dano do player 
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackDelay);
        isAttack = false;
    }

}