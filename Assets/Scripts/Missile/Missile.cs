using UnityEngine;

/// <summary>
/// 抛物线导弹
/// <para>计算弹道和转向</para>
/// <para>ZhangYu 2019-02-27</para>
/// </summary>
public class Missile : MonoBehaviour
{

    public Transform target;        // 目标
    public float hight = 16f;       // 抛物线高度
    public float gravity = -9.8f;   // 重力加速度
    private Vector3 position;       // 我的位置
    private Vector3 dest;           // 目标位置
    private Vector3 velocity;       // 运动速度
    private float time = 0;         // 运动时间

    private void Start()
    {
        dest = target.position;
        position = transform.position;
        velocity = PhysicsUtil.GetParabolaInitVelocity(position, dest, gravity, hight, 0);
        transform.LookAt(PhysicsUtil.GetParabolaNextPosition(position, velocity, gravity, Time.deltaTime));
    }

    private void Update()
    {
        // 计算位移
        float deltaTime = Time.deltaTime;
        position = PhysicsUtil.GetParabolaNextPosition(position, velocity, gravity, deltaTime);
        transform.position = position;
        time += deltaTime;
        velocity.y += gravity * deltaTime;

        // 计算转向
        transform.LookAt(PhysicsUtil.GetParabolaNextPosition(position, velocity, gravity, deltaTime));

        // 简单模拟一下碰撞检测
        if (position.y <= dest.y) enabled = false;
    }

}
