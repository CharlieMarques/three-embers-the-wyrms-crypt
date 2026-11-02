// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Interfaz para que un componente se entere cuando el pool lo saca o lo guarda.

public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}
