// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Vida reutilizable de player, enemigos y boss: recibe daño, cura, invulnerabilidad y avisa con eventos al cambiar o morir.

using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable, IResource
{
    [SerializeField] private int maxHealth = 100;

    // Visible en el Inspector para debuggear (no lo edites a mano en Play).
    [SerializeField] private int currentHealth;

    public int Current => currentHealth;
    public int Max => maxHealth;
    public bool IsDead { get; private set; }
    public bool IsFull => currentHealth >= maxHealth;

    // IResource pide el valor como float (el maná tiene decimales): acá lo convertimos
    float IResource.Current => currentHealth;

    // Mientras sea true, los golpes no hacen daño (ej. enemigo saliendo del piso, boss con minions vivos)
    public bool Invulnerable { get; set; }

    // (vidaActual, vidaMaxima) -> la UI se suscribe a esto
    public event Action<int, int> OnHealthChanged;
    // Lo mismo, con la firma de IResource (la usa OrbUI)
    public event Action<float, int> OnValueChanged;
    // Se dispara cada vez que recibe daño (cantidad) -> feedback visual, aggro de enemigos
    public event Action<int> OnDamaged;
    // Se dispara cuando un golpe no hace daño por estar invulnerable (cantidad que se bloqueó)
    public event Action<int> OnDamageBlocked;
    // Se dispara una sola vez al morir
    public event Action OnDeath;

    private IDamageModifier modifier; // opcional

    private void Awake()
    {
        modifier = GetComponent<IDamageModifier>();
        ResetHealth();
    }

    public void TakeDamage(int amount, DamageType type)
    {
        if (IsDead || amount <= 0) return;

        if (Invulnerable)
        {
            OnDamageBlocked?.Invoke(amount);
            return;
        }

        // Resistencias / debilidades (ej. multiplicadores del EnemyStats)
        if (modifier != null) amount = modifier.ModifyDamage(amount, type);
        if (amount <= 0) return; // inmune a este tipo de daño

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        NotifyChanged();
        OnDamaged?.Invoke(amount);

        if (currentHealth == 0)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        NotifyChanged();
    }

    // IResource: para la vida, "restaurar" es curar
    public void Restore(int amount)
    {
        Heal(amount);
    }

    // Cambia la vida máxima y la llena. La usa EnemyProfile para aplicar la vida del EnemyStats.
    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        ResetHealth();
    }

    // Se usa al respawnear al Player y al reutilizar enemigos del pool.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnValueChanged?.Invoke(currentHealth, maxHealth);
    }
}
