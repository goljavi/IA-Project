using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using IA2;

public class Enemy : MonoBehaviour
{
    public enum Feed { IDLE, JUMP, SEARCH, SHOOT, FOLLOW, DIE }
    private EventFSM<Feed> _FSM;
    public GameObject bullet;
    public float followSpeed;
    public float searchSpeed;
    public float searchPointThreshold;
    public float jumpForce;
    public float maxLife;
    public float minLife;
    public Weapon[] weaponArr;
    public int maxBullets;
    public int minBullets;
    public float baseRadius;
    public float enemyRadius;
    public LayerMask enemyLayerMask;
    public LayerMask mapLayerMask;
    public string currentState;
    [HideInInspector] public Weapon weapon;
    [HideInInspector] public int kills;
    public Enemy shootingEnemy;

    Rigidbody _rb;
    public float life;
    float _lifePercentage;
    float _timeStamp;
    int _bullets;
    Vector3 _searchPoint;
    Enemy _lastHit;
    GridEntity _last;
    IEnumerable<GridEntity> _path;
    
    List<Enemy> enemyList;

    private void Awake()
    {
        life = Random.Range(minLife, maxLife);
        _bullets = Random.Range(minBullets, maxBullets);
        weapon = weaponArr[Random.Range(0, weaponArr.Length)];
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        var enemies = GameObject.FindGameObjectsWithTag("Enemy").Where(x => x.gameObject != gameObject);

        //IA2-P3
        var idle = new State<Feed>("IDLE");
        var jumping = new State<Feed>("JUMPING");
        var searching = new State<Feed>("SEARCHING");
        var shooting = new State<Feed>("SHOOTING");
        var following = new State<Feed>("FOLLOWING");
        var dying = new State<Feed>("DYING");


        StateConfigurer.Create(idle)
            .SetTransition(Feed.SEARCH, searching)
            .SetTransition(Feed.DIE, dying)
            .SetTransition(Feed.JUMP, jumping)
            .Done();

        StateConfigurer.Create(searching)
            .SetTransition(Feed.JUMP, jumping)
            .SetTransition(Feed.SEARCH, searching)
            .SetTransition(Feed.SHOOT, shooting)
            .SetTransition(Feed.IDLE, idle)
            .SetTransition(Feed.DIE, dying)
            .Done();

        StateConfigurer.Create(jumping)
            .SetTransition(Feed.SEARCH, searching)
            .SetTransition(Feed.FOLLOW, following)
            .SetTransition(Feed.DIE, dying)
            .Done();

        StateConfigurer.Create(following)
            .SetTransition(Feed.JUMP, jumping)
            .SetTransition(Feed.SHOOT, shooting)
            .SetTransition(Feed.IDLE, idle)
            .SetTransition(Feed.DIE, dying)
            .Done();

        StateConfigurer.Create(shooting)
            .SetTransition(Feed.SEARCH, searching)
            .SetTransition(Feed.JUMP, jumping)
            .SetTransition(Feed.FOLLOW, following)
            .SetTransition(Feed.IDLE, idle)
            .SetTransition(Feed.DIE, dying)
            .Done();

        StateConfigurer.Create(dying).Done();

        //IDLE
        idle.OnUpdate += () =>
        {
            if (enemyList.Count() > 0) FeedFSM(Feed.SEARCH);
            else enemyList = enemies.Where(x => x != null).Select(x => x.GetComponent<Enemy>()).ToList();
        };


        //SEARCHING
        searching.OnEnter += h =>
        {
            enemyList = enemyList.Where(x => x != null).ToList();
            if (enemyList.Count() < 1)
            {
                FeedFSM(Feed.IDLE);
                return;
            }

            shootingEnemy = enemyList.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First();

            (GridEntity first, GridEntity last) = GetFirstAndLastNode();

            if(first != null && last != null)
            {
                _last = last;
                _path = AStar.Run(first, Satisfies, Expand, Heuristic);
            }
            else
            {
                FeedFSM(Feed.SEARCH);
            }
            
        };
        searching.OnUpdate += () =>
        {
            if(shootingEnemy == null)
            {
                FeedFSM(Feed.IDLE);
                return;
            }

            if (Vector3.Distance(transform.position, shootingEnemy.transform.position) < 5)
            {
                FeedFSM(Feed.SHOOT);
                return;
            }

            if (_path != null)
            {
                if (_searchPoint != null && Vector3.Distance(_searchPoint, transform.position) < 0.5f) _path = _path.Skip(1);
                if(_path.Count() > 0)
                {
                    _searchPoint = _path.First().transform.position;
                    _searchPoint.y = transform.position.y;
                    transform.forward = _searchPoint - transform.position;
                    Move(searchSpeed);
                }
                else
                {
                    FeedFSM(Feed.SEARCH);
                }
            }
            
        };

        //JUMPING
        jumping.OnEnter += x =>
        {
            Jump();
        };
        jumping.OnUpdate += () =>
        {
            if (Physics.Raycast(transform.position, Vector3.down, 1.5f) || _rb.velocity.y == 0)
            {
                if (shootingEnemy == null) FeedFSM(Feed.SEARCH);
                else FeedFSM(Feed.FOLLOW);
            }
            
        };

        //SHOOTING
        shooting.OnUpdate += () =>
        {
            if (shootingEnemy == null)
            {
                FeedFSM(Feed.IDLE);
                return;
            }

            if (Vector3.Distance(shootingEnemy.transform.position, transform.position) > enemyRadius)
            {
                FeedFSM(Feed.FOLLOW);
                return;
            }

            Debug.DrawRay(transform.position, shootingEnemy.transform.position - transform.position, Color.green);

            if (_timeStamp <= Time.time)
            {
                _timeStamp = Time.time + weapon.coolDown;

                Vector3 targetPostition = new Vector3(shootingEnemy.transform.position.x, transform.position.y, shootingEnemy.transform.position.z);
                transform.LookAt(targetPostition);

                _bullets -= weapon.bulletsUsedPerShot;
                var bul = Instantiate(bullet);
                bul.transform.position = transform.position + transform.forward;
                bul.transform.forward = transform.forward;
                var bulComp = bul.GetComponent<Bullet>();
                bulComp.numberOne = this;
                bulComp.damage = weapon.damage;
            }
            
        };

        //FOLLOWING
        following.OnUpdate += () =>
        {
            if (shootingEnemy == null)
            {
                FeedFSM(Feed.IDLE);
                return;
            }

            if (Vector3.Distance(shootingEnemy.transform.position, transform.position) <= enemyRadius)
            {
                FeedFSM(Feed.SHOOT);
                return;
            }

            transform.forward = shootingEnemy.transform.position - transform.position;
            Move(followSpeed);
        };

        //DYING
        dying.OnEnter += x =>
        {
            UIManager.Instance.deadOnes.Add((transform.name, kills, weapon.name, life, false));
            _lastHit.SetKill();
            Destroy(gameObject);
        };


        _lifePercentage = (life * 100) / maxLife;

        //////////////////////////
        //IA2-P1 (Operación 1) -> Busqueda de enemigos
        /* Se utilizó Select, Where, SkipWhile (+ Take), OrderBy (+ OrderByDescending) */
        if (_lifePercentage > 80)
        {
            enemyList = enemies.Select(x => x.GetComponent<Enemy>()).Where(x => x != null && _lifePercentage > 30).SkipWhile(x => x.weapon.name == "Pistol").OrderBy(x => x.life).ToList();
        }else if (_lifePercentage > 30)
        {
            enemyList = enemies.Select(x => x.GetComponent<Enemy>()).Where(x => x != null && _lifePercentage < 50).Take(Random.Range(0, 10)).OrderByDescending(x => x.life).ToList();
        }
        else
        {
            enemyList = enemies.Select(x => x.GetComponent<Enemy>()).ToList();
        }
        //////////////////////////

        _FSM = new EventFSM<Feed>(idle);
    }

    bool TargetCheck(GridEntity n) => n == _last;

    IEnumerable<System.Tuple<GridEntity, float>> GetNeighbours(GridEntity n)
    {
        return n.neighbours.Aggregate(new List<System.Tuple<GridEntity, float>>(), (acum, current) =>
        {
            acum.Add(System.Tuple.Create(current, 1f));
            return acum;
        });
    }

    float GetHeuristic(GridEntity n) => Vector3.Distance(transform.position, _last.transform.position);

    bool Satisfies(GridEntity node) =>_last.Equals(node);

    Dictionary<GridEntity, float> Expand(GridEntity node)
    {
        var dictionary = new Dictionary<GridEntity, float>();
        foreach (var item in node.neighbours)
        {
            if(!dictionary.ContainsKey(item)) dictionary.Add(item, 1);
        }
        return dictionary;
    }

    float Heuristic(GridEntity node)
    {
        return Vector3.Distance(node.transform.position, shootingEnemy.transform.position);
    }



    void Move(float sp)
    {
        _rb.velocity = transform.forward * sp;
    }

    void Jump()
    {
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        _rb.AddForce(transform.forward * jumpForce /2 , ForceMode.Impulse);
    }

    private void FeedFSM(Feed inp)
    {
        _FSM.SendInput(inp);
    }

    void Update()
    {
        RaycastHit hit;
        if (life < 1) FeedFSM(Feed.DIE);
        else if (transform.position.y > 1 || Physics.SphereCast(transform.position, 1f, transform.forward * 3f, out hit, 3f, mapLayerMask))
        {
            FeedFSM(Feed.JUMP);
        }

        currentState = _FSM.Current.Name;

        _FSM.Update();
    }

    (GridEntity first, GridEntity last) GetFirstAndLastNode()
    {
        //IA2-P2
        AreaQuery.Instance.transform.position = transform.position;
        var nodes = AreaQuery.Instance.Query();
        return (nodes.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault(), nodes.OrderBy(x => Vector3.Distance(shootingEnemy.transform.position, x.transform.position)).FirstOrDefault());
    }

    private void FixedUpdate()
    {
        _FSM.FixedUpdate();
    }

    public void GetDamage(float damage)
    {
        life -= damage;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyRadius);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Bullet")
        {
            var bulComp = collision.gameObject.GetComponent<Bullet>();
            GetDamage(bulComp.damage);
            _lastHit = bulComp.numberOne;
            Destroy(collision.gameObject);
        }
    }

    public void SetKill()
    {
        kills++;
    }
}
