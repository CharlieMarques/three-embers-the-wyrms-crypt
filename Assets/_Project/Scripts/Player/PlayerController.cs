// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Lee el mouse y decide qué hace el player: moverse, marcar un enemigo para atacarlo o lanzar la daga.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Click")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float maxRayDistance = 200f;

    private PlayerMovement movement;
    private PlayerCombat combat;
    private PlayerRangedAttack ranged; // opcional: si no está, el click derecho no hace nada
    private Health health;
    private Camera cam;
    private Health currentTarget;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        ranged = GetComponent<PlayerRangedAttack>();
        health = GetComponent<Health>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (health.IsDead) return;

        ReadInput();
        UpdateTarget();
    }

    private void ReadInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Si el mouse está sobre un botón de la UI, no hacemos nada.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Click derecho (o mantenerlo) -> habilidad a distancia hacia el mouse
        if (ranged != null && mouse.rightButton.isPressed)
        {
            Ray castRay = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(castRay, out RaycastHit castHit, maxRayDistance, groundMask | enemyMask))
            {
                currentTarget = null;
                movement.Stop();
                ranged.TryCast(castHit.point);
            }
            return;
        }

        bool clicked = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;
        if (!clicked && !held) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

        // 1) Click sobre un enemigo -> lo marcamos como objetivo
        if (clicked && Physics.Raycast(ray, out RaycastHit enemyHit, maxRayDistance, enemyMask))
        {
            Health target = enemyHit.collider.GetComponentInParent<Health>();
            if (target != null && !target.IsDead)
            {
                currentTarget = target;
                return;
            }
        }

        // Mantener apretado después de marcar un enemigo = seguir atacándolo
        if (currentTarget != null && !clicked) return;

        // 2) Click (o mantener apretado) sobre el piso -> movernos ahí
        if (Physics.Raycast(ray, out RaycastHit groundHit, maxRayDistance, groundMask))
        {
            currentTarget = null;
            movement.MoveTo(groundHit.point);
        }
    }

    private void UpdateTarget()
    {
        if (currentTarget == null) return;

        // El objetivo murió o volvió al pool
        if (currentTarget.IsDead || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            return;
        }

        if (!combat.IsInRange(currentTarget.transform))
        {
            movement.MoveTo(currentTarget.transform.position);
        }
        else
        {
            movement.Stop();
            movement.FaceTowards(currentTarget.transform.position);
            combat.TryAttack(currentTarget);
        }
    }

    // Lo usa el RespawnManager al reaparecer.
    public void Teleport(Vector3 position)
    {
        currentTarget = null;
        movement.Teleport(position);
    }
}
