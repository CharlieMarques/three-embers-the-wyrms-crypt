// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Maná del player: se gasta en el ataque a distancia, se regenera con el tiempo y se recupera con pociones.

using System;
using UnityEngine;

public class Mana : MonoBehaviour, IResource
{
    [SerializeField] private int maxMana = 100;
    [SerializeField] private float regenPerSecond = 4f;

    [SerializeField] private float currentMana; // visible para debuggear

    public float Current => currentMana;
    public int Max => maxMana;
    public bool IsFull => currentMana >= maxMana;

    // (manaActual, manaMaximo)
    public event Action<float, int> OnValueChanged;

    private void Awake()
    {
        ResetMana();
    }

    private void Update()
    {
        if (IsFull || regenPerSecond <= 0f) return;

        currentMana = Mathf.Min(currentMana + regenPerSecond * Time.deltaTime, maxMana);
        OnValueChanged?.Invoke(currentMana, maxMana);
    }

    // Devuelve true si había maná suficiente y lo gastó.
    public bool TrySpend(int amount)
    {
        if (currentMana < amount) return false;

        currentMana -= amount;
        OnValueChanged?.Invoke(currentMana, maxMana);
        return true;
    }

    public void Restore(int amount)
    {
        if (amount <= 0) return;

        currentMana = Mathf.Min(currentMana + amount, maxMana);
        OnValueChanged?.Invoke(currentMana, maxMana);
    }

    public void ResetMana()
    {
        currentMana = maxMana;
        OnValueChanged?.Invoke(currentMana, maxMana);
    }
}
