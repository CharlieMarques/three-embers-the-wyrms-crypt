# Three Embers: The Wyrm's Crypt

Prototipo de nivel de acción **top-down** inspirado en *Diablo II*, hecho en **Unity 6 (URP)**. Recorré la cripta, encendé las **tres llamas** para abrir la cripta del Dragón, derrotá al **Dragón Ancestral** y escapá por el portal.

> 🎮 **Jugalo en el navegador:** [itch.io](https://LINK-A-ITCH.itch.io/three-embers) 

---

## Características

- **Movimiento con click** sobre NavMesh, al estilo ARPG clásico.  
- **Combate cuerpo a cuerpo** con dos animaciones alternadas y daño sincronizado con el golpe.  
- **Ataque a distancia** (daga lanzada) con costo de **maná** y regeneración.  
- **3 altares de llama**: hay que quedarse dentro del círculo para encenderlos; al completarlos se abre la puerta del jefe.  
- **Enemigos con comportamiento propio**: esqueletos que emergen del suelo al acercarte, murciélagos y slimes que patrullan.  
- **Jefe con 3 fases**: ataque cuerpo a cuerpo, ráfagas de fuego con aviso previo, más velocidad e **invocación de esbirros que lo vuelven invulnerable** hasta que mueren.  
- **Puntos de control** (waystones y altares) y reaparición al morir.  
- **Stats de enemigos con ScriptableObjects**: vida, ataque, velocidad y multiplicadores de daño por tipo (cuerpo a cuerpo / distancia).  
- **Object Pooling** para proyectiles, enemigos invocados, pociones, números de daño y efectos.  
- Menú principal, menú de pausa, HUD con orbes de vida/maná, barra del jefe, objetivos y transiciones con fundido.

## Controles

| Acción | Control |
| :---- | :---- |
| Moverse | Click izquierdo sobre el piso (mantener para seguir el cursor) |
| Atacar cuerpo a cuerpo | Click izquierdo sobre un enemigo (mantener para seguir atacando) |
| Lanzar daga | Click derecho hacia el cursor (gasta maná) |
| Agarrar pociones | Caminar por encima |
| Pausa | `ESC` |

## Requisitos

- **Unity 6000.3.10f1** (Unity 6\) o compatible.  
- Paquetes (se instalan solos desde `Packages/manifest.json`):  
  - Universal Render Pipeline  
  - AI Navigation  
  - Input System  
  - uGUI / TextMesh Pro

## Cómo abrirlo

1. Clonar el repositorio:

```sh
git clone https://github.com/USUARIO/REPO.git
```

2. En **Unity Hub**: *Add \> Add project from disk* y elegir la carpeta del proyecto.  
3. Abrir la escena `Assets/_Project/Scenes/MainMenu` y dar **Play**.

Si el NavMesh no aparece, abrí `Level_01`, seleccioná el objeto **NavMesh** y apretá **Bake**.

## Cómo hacer una build

1. *File \> Build Profiles* y verificar el orden de escenas:  
   - `0` → `MainMenu`  
   - `1` → `Level_01`  
2. **Web:** elegir *Web*, y en *Player Settings \> Publishing Settings* usar compresión **Gzip** con **Decompression Fallback** activado.  
3. **Windows:** elegir *Windows* y hacer **Build**.

## Estructura del proyecto

```
Assets/_Project/
├─ Animations/      Animator Controllers y overrides
├─ Data/            ScriptableObjects (EnemyStats de cada enemigo)
├─ Materials/
├─ Models/          Modelos FBX (personaje, monstruos, dungeon)
├─ Prefabs/
├─ Scenes/          MainMenu, Level_01
├─ Settings/        Assets de URP y presets (Tags & Layers)
└─ Scripts/
   ├─ Camera/       Cámara isométrica
   ├─ Combat/       Vida, maná, proyectiles, tipos de daño e interfaces
   ├─ Core/         Pool de objetos, carga de escenas, animación base y utilidades
   ├─ Editor/       Herramientas de editor (Room Builder, Material Extractor)
   ├─ Enemies/      IA, ataque, muerte, botín, stats y jefe
   ├─ Game/         Flujo del nivel
   ├─ Level/        Altares, puerta del jefe, waystones, portal, pociones, respawn
   ├─ Player/       Controlador, movimiento, combate y animaciones del player
   └─ UI/           HUD, menús, barra del jefe y números de daño
```

## Arquitectura

El código prioriza **componentes pequeños con una sola responsabilidad** y comunicación por **eventos**.

| Patrón / concepto | Dónde se usa |
| :---- | :---- |
| **Observer** (eventos C\#) | `Health` avisa daño/muerte; UI, animaciones, efectos, botín y el `GameManager` reaccionan sin acoplarse. Eventos globales para altares, jefe y enemigos derrotados. |
| **Object Pool** | `ObjectPool` \+ `PooledObject` \+ `IPoolable` para reutilizar objetos en lugar de instanciar y destruir. |
| **Interfaces** | `IDamageable`, `IDamageModifier`, `IResource`, `IPoolable`. |
| **Template Method** | `CharacterAnimator` es la base abstracta de `EnemyAnimator` y `PlayerAnimator`. |
| **Herencia para comportamiento común** | `PlayerProximityZone` es la base de pociones, altares, waystones y portal. |
| **Máquina de estados** | IA de enemigos (`Dormant → Spawning → Patrol → Chase → Attack`) y fases del jefe. |
| **Datos en ScriptableObjects** | `EnemyStats` define cada tipo de enemigo; `EnemyProfile` los aplica. |
| **Singleton** | `GameManager`, `ObjectPool`, `RespawnManager`, `SceneLoader`. |
| **Composición** | El player se divide en `PlayerController` (input), `PlayerMovement`, `PlayerCombat` y `PlayerRangedAttack`. |

## Créditos

- Modelos 3D de [**Quaternius**](https://quaternius.com) (licencia CC0):  
  - LowPoly Animated Monsters  
  - LowPoly Modular Dungeon  
  - RPG Characters  
- Motor: [Unity](https://unity.com)

## Autor

**Carlos Matías Marques** 📧 [Charlie.Marques92@gmail.com](mailto:Charlie.Marques92@gmail.com)

Proyecto realizado para la **Licenciatura en Diseño de Videojuegos** (Universidad de la Ciudad de Buenos Aires).