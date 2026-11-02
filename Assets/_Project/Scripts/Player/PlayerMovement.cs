// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Movimiento del player con NavMeshAgent: ir a un punto, frenar, girar y teletransportarse.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float turnSpeed = 720f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 point)
    {
        agent.isStopped = false;
        agent.SetDestination(point);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    public void FaceTowards(Vector3 point)
    {
        TransformUtil.RotateTowardsFlat(transform, point, turnSpeed);
    }

    public void Teleport(Vector3 position)
    {
        agent.Warp(position);
        agent.ResetPath();
        agent.isStopped = false;
    }
}
