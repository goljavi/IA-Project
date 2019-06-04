using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaQuery : MonoBehaviour
{
    public float radius;
    public SpatialGrid grid;
    public static AreaQuery Instance;

    private void Start() => Instance = this;

    public IEnumerable<GridEntity> Query() => 
        grid.Query(
            transform.position + new Vector3(-radius, 0, -radius),
            transform.position + new Vector3(radius, 0, radius),
            x => {
                var position2d = x - transform.position;
                position2d.y = 0;
                return position2d.sqrMagnitude < radius * radius;
            });


    void OnDrawGizmos()
    {
        if (grid == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
