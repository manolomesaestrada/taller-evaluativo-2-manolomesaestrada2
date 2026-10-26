/* ============================================================================
 *  EnemyController.cs
 * ----------------------------------------------------------------------------
 *  ¿QUÉ HACE ESTE SCRIPT?
 *  Controla a un enemigo que patrulla entre dos puntos de la escena y que
 *  golpea al jugador cuando choca con él. Se encarga de:
 *      1. Moverse hacia un punto objetivo.
 *      2. Detectar cuándo ya llegó a ese punto y cambiar al otro (patrullaje).
 *      3. Voltear el sprite para que mire hacia donde camina.
 *      4. Avisarle al Animator qué animación mostrar.
 *      5. Al chocar con el jugador: quitarle vida, empujarlo y atacar.
 *
 *  ¿DÓNDE SE PONE?
 *  Este script se arrastra al GameObject del enemigo.
 *
 *  COMPONENTES QUE NECESITA EL GAMEOBJECT DEL ENEMIGO:
 *      - Rigidbody2D      -> para moverlo con física. (Se busca solo en Start)
 *      - Collider2D       -> para chocar con el jugador. OJO: NO debe estar
 *                            marcado como "Is Trigger", porque este script usa
 *                            OnCollisionEnter2D, no OnTriggerEnter2D.
 *      - Animator         -> para las animaciones. (Se busca solo en Start)
 *
 *  QUÉ HAY QUE CONFIGURAR EN EL INSPECTOR:
 *      - Enemy Speed         -> qué tan rápido patrulla.
 *      - Detection Radius    -> qué tan cerca debe estar del punto para
 *                             considerar que "ya llegó". Viene en 0.5.
 *      - Enemy Movement Points -> ¡IMPORTANTE! Hay que poner el tamaño del
 *                             arreglo en 2 y arrastrar dos GameObjects vacíos
 *                             que marquen el inicio y el fin de la patrulla.
 *                             Si se deja vacío, el juego da error en Start.
 *      - Enemy Damage        -> cuánta vida le quita al jugador por golpe.
 *      - Enemy Strength      -> con cuánta fuerza empuja al jugador.
 *
 *  OJO: este script le ESCRIBE variables al PlayerController (hitTime,
 *  hitForce, hitFromRight). Por eso esas variables son public allá.
 *  Los dos scripts trabajan juntos: aquí se decide el golpe, allá se ejecuta.
 * ========================================================================= */


using UnityEngine;      // Todo lo básico de Unity: MonoBehaviour, Vector2, Rigidbody2D, Mathf...
                        // Este script NO necesita el Input System ni TMPro:
                        // el enemigo no lo controla el jugador ni escribe en la UI.


public class EnemyController : MonoBehaviour
{
    // ========================================================================
    // VARIABLES
    // ========================================================================

    // Fíjense que esta variable NO dice public ni private.
    // En C#, cuando no se escribe nada, por defecto es private.
    // Por eso no aparece en el Inspector: es de uso interno del script.
    Vector2 movement; // para ustedes, por ahora, esto es direction
                      // Es un Vector2 (X, Y) que guarda hacia dónde se mueve el
                      // enemigo: (-1, 0) izquierda, (1, 0) derecha.

    public float enemySpeed;              // Velocidad de patrullaje.
    public Rigidbody2D enemyRb;           // Referencia al Rigidbody2D del enemigo.

    // Aquí le damos un VALOR POR DEFECTO a la variable (= 0.5f).
    // Eso significa que si no la tocamos en el Inspector, ya arranca en 0.5
    // y no en 0. Muy útil para que el script funcione "de una".
    public float detectionRadius = 0.5f;

    // Transform es el componente que guarda posición, rotación y escala.
    // Si guardamos un Transform, tenemos acceso a la POSICIÓN de ese objeto.
    public Transform actualObjective;     // Hacia qué punto está caminando AHORA MISMO.

    // Los corchetes [] significan que es un ARREGLO (array): una lista de
    // varios Transforms guardados en una sola variable.
    // Se accede a ellos por su número de posición, empezando en 0:
    //       enemyMovementPoints[0] -> el primer punto
    //       enemyMovementPoints[1] -> el segundo punto
    public Transform[] enemyMovementPoints;

    public Animator enemyAnimator;      // Componente de animaciones del enemigo.

    public bool isFacingRight = false;          // ¿El sprite mira a la derecha?

    // Estas dos son private + [SerializeField]: aparecen en el Inspector para
    // poder ajustarlas, pero ningún otro script puede modificarlas por error.
    [SerializeField] private float enemyDamage;      // Daño que hace al jugador.
    [SerializeField] private float enemyStrength;   // Fuerza del empujón al jugador.

    // Variable interna para llevar el control del punto actual de la ruta
    private int currentPointIndex = 0;


    // ========================================================================
    // START()
    // Se ejecuta UNA SOLA VEZ, antes del primer frame. Aquí dejamos al enemigo
    // listo para empezar a patrullar.
    // ========================================================================
    void Start()
    {
        // GetComponent<T>() busca el componente en ESTE MISMO GameObject.
        // Al hacerlo por código, no hay que arrastrarlos en el Inspector
        // y evitamos olvidos.
        enemyRb = GetComponent<Rigidbody2D>();
        enemyAnimator = GetComponent<Animator>();

        // Verificamos que existan puntos configurados para evitar errores
        if (enemyMovementPoints != null && enemyMovementPoints.Length > 0) {
            currentPointIndex = 0;
            actualObjective = enemyMovementPoints[currentPointIndex];
        }

        // Asumimos que el sprite del enemigo, tal como está hecho, mira a la
        // derecha. Si el arte estuviera volteado, esto iría en false.
        isFacingRight = true;
    }


    // ========================================================================
    // UPDATE()
    // Se ejecuta una vez POR CADA FRAME. Toda la lógica de patrullaje vive aquí.
    // ========================================================================
    void Update()
    {
        if (enemyMovementPoints == null || enemyMovementPoints.Length == 0 || actualObjective == null) return;

        // ---- 1. ¿QUÉ TAN LEJOS ESTOY DE MI OBJETIVO? ----
        // Vector2.Distance(a, b) devuelve la distancia en línea recta entre
        // dos puntos, como un número (float). Siempre es positiva.
        // transform.position = MI posición.  actualObjective.position = la del punto.
        float distanceToObjective = Vector2.Distance(transform.position, actualObjective.position);

        // ---- 2. ¿YA LLEGUÉ? ENTONCES CAMBIO DE OBJETIVO ----
        // No preguntamos si la distancia es EXACTAMENTE 0, porque con números
        // decimales eso casi nunca pasa. En su lugar preguntamos si ya está
        // "suficientemente cerca" (más cerca que detectionRadius).
        if (distanceToObjective < detectionRadius) {
            
            currentPointIndex = (currentPointIndex + 1) % enemyMovementPoints.Length;
            actualObjective = enemyMovementPoints[currentPointIndex];
        }

        // ---- 3. ¿EN QUÉ DIRECCIÓN QUEDA MI OBJETIVO? ----
        // Restar dos posiciones da un vector que apunta del segundo al primero:
        //       (destino - origen) = "flecha" que va desde donde estoy hacia allá.
        // .normalized convierte esa flecha en una de LONGITUD 1, conservando
        // solo la dirección. Así la velocidad no depende de qué tan lejos esté
        // el punto: siempre camina igual de rápido.
        Vector2 direction = (actualObjective.position - transform.position).normalized;

        // Mathf.RoundToInt() redondea un decimal al entero más cercano.
        // Convertimos algo como 0.98 o -0.87 en un limpio 1 o -1.
        // ¿Por qué? Porque solo nos interesa "izquierda o derecha", no una
        // dirección diagonal. Además el Animator trabaja más fácil con -1/0/1.
        int roundDirection = Mathf.RoundToInt(direction.x);

        // Armamos el vector de movimiento: solo en X, el Y queda en 0.
        // Así el enemigo nunca sube ni baja, solo camina horizontalmente.
        movement = new Vector2(roundDirection, 0);

        // ---- 4. ¿DEBO VOLTEAR EL SPRITE? ----
        // Misma lógica que en el jugador: comparamos hacia dónde MIRA con
        // hacia dónde SE MUEVE. Si no coinciden, lo volteamos.
        if (roundDirection < 0 && !isFacingRight)        // Va a la izquierda pero mira a la derecha.
        {
            Flip();
        }
        else if (roundDirection > 0 && isFacingRight)  // Va a la derecha pero mira a la izquierda.
        {
            Flip();
        }

        // ---- 5. ANIMACIÓN ----
        // Le pasamos la dirección al parámetro float "Direction" del Animator,
        // igual que en el PlayerController.
        enemyAnimator.SetFloat("Direction", roundDirection);

        // ---- 6. MOVIMIENTO ----
        // MovePosition() mueve el Rigidbody2D a una posición nueva.
        // Desarmemos la cuenta de adentro hacia afuera:
        //       enemyRb.position                    -> donde estoy ahora
        //       movement * enemySpeed               -> cuánto me quiero mover por segundo
        //       * Time.deltaTime                    -> lo convierte a "por frame"
        //       enemyRb.position + (todo eso)       -> mi posición nueva
        //
        // ¿Por qué Time.deltaTime? Porque Update corre más veces por segundo en
        // un PC rápido que en uno lento. Multiplicar por deltaTime hace que el
        // enemigo se mueva a la MISMA velocidad real en cualquier computador.
        // Sin él, el enemigo sería más rápido en los equipos más potentes.
        enemyRb.MovePosition(enemyRb.position + movement * enemySpeed * Time.deltaTime);
    }


    // ========================================================================
    // FLIP()
    // Método propio (lo llamamos nosotros desde Update, no Unity).
    // Voltea el sprite como un espejo, multiplicando la escala en X por -1.
    // Es EXACTAMENTE el mismo método que tiene el PlayerController.
    // ========================================================================
    private void Flip()
    {
        isFacingRight = !isFacingRight;                 // Cambia el estado de la variable

        Vector3 localScale = transform.localScale;      // Guarda la escala actual
        localScale.x *= -1f;                            // Invierte el eje X (espejo)
        transform.localScale = localScale;              // Aplica la nueva escala
    }


    // ========================================================================
    // ONCOLLISIONENTER2D()
    // ------------------------------------------------------------------------
    // Unity lo llama AUTOMÁTICAMENTE en el instante en que este objeto CHOCA
    // con otro collider sólido (uno que NO tiene "Is Trigger" activado).
    //
    // DIFERENCIA CLAVE (esto suele confundir):
    //       OnCollisionEnter2D -> choque REAL, los objetos se empujan. Parámetro: Collision2D
    //       OnTriggerEnter2D   -> se atraviesan, solo avisa del contacto. Parámetro: Collider2D
    //
    // El parámetro "collision" trae la información del choque: con quién choqué,
    // en qué punto, con cuánta fuerza, etc.
    // ========================================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Preguntamos: "¿con lo que choqué tiene un componente PlayerController?"
        // Si GetComponent no encuentra el componente, devuelve null (nada).
        // Entonces "!= null" significa "SÍ lo encontró" -> choqué con el jugador.
        //
        // Esta es una alternativa a usar CompareTag("Player"). Es más segura,
        // porque garantiza que el objeto realmente tiene el script que vamos a
        // usar y no solo una etiqueta que alguien pudo asignar mal.
        if (collision.gameObject.GetComponent<PlayerController>() != null) {
            // Guardamos la referencia al script del jugador en una variable,
            // para no tener que escribir GetComponent una y otra vez.
            // Ahora, a través de "player", podemos usar sus métodos y variables.
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            // Llamamos al método TakeDamage() que está EN EL OTRO SCRIPT.
            // Le enviamos enemyDamage como parámetro: el enemigo decide cuánto
            // daño hace, y el jugador se encarga de restarlo y actualizar la UI.
            player.TakeDamage(enemyDamage);

            // Le ESCRIBIMOS variables al jugador para configurar su empujón.
            // Recuerden que en el PlayerController, mientras hitTime sea mayor
            // que 0, el jugador pierde el control y sale volando.
            player.hitTime = 0.5f;              // Medio segundo de aturdimiento.
            player.hitForce = enemyStrength;    // Qué tan fuerte sale volando.

            // ---- ¿HACIA DÓNDE DEBE SALIR VOLANDO EL JUGADOR? ----
            // Comparamos las posiciones en X para saber quién está a la izquierda.
            // collision.transform.position.x -> X del JUGADOR
            // transform.position.x            -> X del ENEMIGO (o sea, la mía)

            if (collision.transform.position.x <= transform.position.x) {
                // El jugador está a MI izquierda, así que el golpe le llegó
                // desde su derecha -> debe salir volando hacia la izquierda.
                player.hitFromRight = true;
            }
            else if (collision.transform.position.x > transform.position.x) {
                // El jugador está a MI derecha: el golpe le llegó por la
                // izquierda -> debe salir volando hacia la derecha.
                player.hitFromRight = false;
            }

            // SetTrigger() activa un parámetro de tipo Trigger en el Animator.
            // A diferencia de SetFloat (que guarda un valor), un Trigger es como
            // un botón: se activa, dispara la animación una vez y se apaga solo.
            // Ideal para acciones puntuales como atacar, saltar o morir.
            enemyAnimator.SetTrigger("IsAttacking");
        }
    }
}