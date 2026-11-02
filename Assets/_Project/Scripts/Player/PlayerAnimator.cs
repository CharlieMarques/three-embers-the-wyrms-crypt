// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Animaciones del player: hereda lo común de CharacterAnimator y agrega el lanzamiento de daga y el reinicio al reaparecer.

using UnityEngine;

public class PlayerAnimator : CharacterAnimator
{
    [Header("Player")]
    [SerializeField] private string castTrigger = "Cast";

    // Valores por defecto al agregar el componente: el player alterna sus dos ataques
    private void Reset()
    {
        alternateAttacks = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RespawnManager.OnPlayerRespawned += ResetAnimator;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RespawnManager.OnPlayerRespawned -= ResetAnimator;
    }

    // Lo llama PlayerRangedAttack al lanzar la daga
    public void PlayCast()
    {
        SetTrigger(castTrigger);
    }

    protected override void HandleDeath()
    {
        ResetTrigger(castTrigger);
        base.HandleDeath();
    }
}
