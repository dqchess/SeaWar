using UnityEngine;

/// <summary> 物理计算工具
/// <para>ZhangYu 2018-05-10</para>
/// </summary>
public static class PhysicsUtil
{

    /**findInitialVelocity
     * Finds the initial velocity of a projectile given the initial positions and some offsets
     * @param Vector3 startPosition - the starting position of the projectile
     * @param Vector3 finalPosition - the position that we want to hit
     * @param float maxHeightOffset (default=0.6f) - the amount we want to add to the height for short range shots. We need enough clearance so the
     * ball will be able to get over the rim before dropping into the target position
     * @param float rangeOffset (default=0.11f) - the amount to add to the range to increase the chances that the ball will go through the rim
     * @return Vector3 - the initial velocity of the ball to make it hit the target under the current gravity force.
     * 
     *      Vector3 tt = findInitialVelocity (gameObject.transform.position, target.transform.position);
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody> ();
            Debug.Log (tt);
            rigidbody.AddForce(tt*rigidbody.mass,ForceMode.Impulse);
     */
    public static Vector3 GetParabolaInitVelocity(Vector3 from, Vector3 to, float gravity = 9.8f, float heightOff = 0.0f, float rangeOff = 0.11f)
    {
        // get our return value ready. Default to (0f, 0f, 0f)
        Vector3 newVel = new Vector3();
        // Find the direction vector without the y-component
        /// /找到未经y分量的方向矢量//
        Vector3 direction = new Vector3(to.x, 0f, to.z) - new Vector3(from.x, 0f, from.z);
        // Find the distance between the two points (without the y-component)
        //发现这两个点之间的距离（不y分量）//
        float range = direction.magnitude;
        // Add a little bit to the range so that the ball is aiming at hitting the back of the rim.
        // Back of the rim shots have a better chance of going in.
        // This accounts for any rounding errors that might make a shot miss (when we don't want it to).
        range += rangeOff;
        // Find unit direction of motion without the y component
        Vector3 unitDirection = direction.normalized;
        // Find the max height
        // Start at a reasonable height above the hoop, so short range shots will have enough clearance to go in the basket
        // without hitting the front of the rim on the way up or down.
        float maxYPos = to.y + heightOff;
        // check if the range is far enough away where the shot may have flattened out enough to hit the front of the rim
        // if it has, switch the height to match a 45 degree launch angle
        //if (range / 2f > maxYPos)
        //  maxYPos = range / 2f;
        if (maxYPos < from.y)
            maxYPos = from.y;

        // find the initial velocity in y direction
        /// /发现在y方向上的初始速度//
        float ft;
        ft = -2.0f * gravity * (maxYPos - from.y);
        if (ft < 0) ft = 0f;
        newVel.y = Mathf.Sqrt(ft);
        // find the total time by adding up the parts of the trajectory
        // time to reach the max
        //发现的总时间加起来的轨迹的各部分//
        //时间达到最大//

        ft = -2.0f * (maxYPos - from.y) / gravity;
        if (ft < 0)
            ft = 0f;

        float timeToMax = Mathf.Sqrt(ft);
        // time to return to y-target
        //时间返回到y轴的目标//

        ft = -2.0f * (maxYPos - to.y) / gravity;
        if (ft < 0)
            ft = 0f;

        float timeToTargetY = Mathf.Sqrt(ft);
        // add them up to find the total flight time
        //把它们加起来找到的总飞行时间//
        float totalFlightTime;

        totalFlightTime = timeToMax + timeToTargetY;

        // find the magnitude of the initial velocity in the xz direction
        /// /查找的初始速度的大小在xz方向//
        float horizontalVelocityMagnitude = range / totalFlightTime;
        // use the unit direction to find the x and z components of initial velocity
        //使用该单元的方向寻找初始速度的x和z分量//
        newVel.x = horizontalVelocityMagnitude * unitDirection.x;
        newVel.z = horizontalVelocityMagnitude * unitDirection.z;
        return newVel;
    }

    /// <summary> 计算抛物线物体在下一帧的位置 </summary>
    /// <param name="position">初始位置</param>
    /// <param name="velocity">移动速度</param>
    /// <param name="gravity">重力加速度</param>
    /// <param name="time">飞行时间</param>
    /// <returns></returns>
    public static Vector3 GetParabolaNextPosition(Vector3 position, Vector3 velocity, float gravity, float time)
    {
        velocity.y += gravity * time;
        return position + velocity * time;
    }
}
