// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Interfaz para modificar el daño recibido (resistencias y debilidades) antes de aplicarlo.

public interface IDamageModifier
{
    // Devuelve el daño final (0 = no recibe daño)
    int ModifyDamage(int amount, DamageType type);
}
