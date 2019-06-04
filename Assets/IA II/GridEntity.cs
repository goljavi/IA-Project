using System;
using System.Collections.Generic;
using UnityEngine;

public class GridEntity : MonoBehaviour
{
	public event Action<GridEntity> OnMove = delegate {};
	public Vector3 velocity = new Vector3(0, 0, 0);
    public List<GridEntity> neighbours;

    void Awake()
    {
        if (neighbours != null) neighbours.ForEach(x => x.neighbours.Add(this));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1);
        if (neighbours != null) neighbours.ForEach(x => Gizmos.DrawRay(transform.position, x.transform.position - transform.position));
    }
}
