// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Interfaz para recursos con valor actual y máximo (vida, maná) que la UI y las pociones tratan igual.

using System;

public interface IResource
{
    float Current { get; }
    int Max { get; }
    bool IsFull { get; }

    // (actual, máximo)
    event Action<float, int> OnValueChanged;

    void Restore(int amount);
}
