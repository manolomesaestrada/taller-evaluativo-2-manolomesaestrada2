/* ============================================================================
 *  PlayerController.cs
 * ----------------------------------------------------------------------------
 *  ¿QUÉ HACE ESTE SCRIPT?
 *  Controla al jugador de una plataforma 2D. Se encarga de:
 *      1. Moverlo horizontalmente (izquierda / derecha).
 *      2. Hacerlo saltar, pero solo si está pisando el suelo.
 *      3. Voltear el sprite para que mire hacia donde camina.
 *      4. Avisarle al Animator qué animación mostrar.
 *      5. Manejar la vida (recibir daño, curarse) y mostrarla en pantalla.
 *      6. Aplicar un "empujón" (knockback) cuando el jugador recibe un golpe.
 *      7. Detectar cuando el jugador toca la llave para abrir la puerta.
 *
 *  ¿DÓNDE SE PONE?
 *  Este script se arrastra al GameObject del jugador.
 *
 *  COMPONENTES QUE NECESITA EL GAMEOBJECT DEL JUGADOR:
 *      - Rigidbody2D      -> para que la física lo mueva y le aplique gravedad.
 *      - Collider2D       -> para chocar con el mundo y detectar triggers.
 *      - Animator         -> para las animaciones (idle, caminar, etc.).
 *      - PlayerInput      -> componente del New Input System que llama a los
 *                            métodos Move() y Jump() de este script.
 *
 *  QUÉ HAY QUE CONFIGURAR EN EL INSPECTOR (¡no se olviden de esto!):
 *      - Speed, JumpForce, HitForce  -> valores numéricos de movimiento.
 *      - Rb                          -> arrastrar el Rigidbody2D del jugador.
 *      - Ground Check                -> arrastrar un GameObject hijo vacío
 *                                       colocado en los pies del jugador.
 *      - Ground Check Radius         -> tamaño del círculo que detecta el piso.
 *      - Ground Layer                -> la Layer que usan los pisos.
 *      - Health / Max Health         -> vida inicial y vida máxima.
 *      - Health Text                 -> arrastrar el objeto de texto (TMP) de la UI.
 * ========================================================================= */


// ----------------------------------------------------------------------------
// USINGS: son "librerías" que le decimos a C# que queremos usar.
// Sin ellas, el compilador no reconoce las clases que escribimos abajo.
// ----------------------------------------------------------------------------
using UnityEngine;              // Todo lo básico de Unity: MonoBehaviour, Vector2, Rigidbody2D...
using UnityEngine.InputSystem;  // El New Input System: nos da InputAction.CallbackContext.
using TMPro;                    // TextMeshPro: el texto de la UI (TextMeshProUGUI).


// ----------------------------------------------------------------------------
// LA CLASE
// "public class PlayerController : MonoBehaviour" significa:
//   -> Estamos creando un componente llamado PlayerController...
//   -> ...que HEREDA de MonoBehaviour.
// Heredar de MonoBehaviour es lo que permite arrastrar el script a un
// GameObject y que Unity llame automáticamente a Start(), Update(), etc.
// IMPORTANTE: el nombre de la clase debe ser IDÉNTICO al nombre del archivo.
// ----------------------------------------------------------------------------
public class PlayerController : MonoBehaviour
{
    // ========================================================================
    // VARIABLES (los "datos" que guarda nuestro jugador)
    // ------------------------------------------------------------------------
    // Recordatorio de tipos:
    //   float  -> número con decimales (ej: 5.5f). La "f" al final es obligatoria.
    //   bool   -> verdadero o falso (true / false).
    //   public -> se ve en el Inspector y otros scripts pueden usarla.
    //   private-> solo se usa DENTRO de este script (más seguro y ordenado).
    //   [SerializeField] -> truco para que una variable private SÍ aparezca en
    //                       el Inspector, sin dejar que otros scripts la toquen.
    // ========================================================================

    // --- Movimiento horizontal ---
    public float direction;     // Hacia dónde empuja el jugador: -1 izquierda, 0 quieto, 1 derecha.
                                // La llena el método Move() cuando el jugador presiona una tecla.
    public float speed;         // Qué tan rápido se mueve. Se ajusta en el Inspector.
    public Rigidbody2D rb;      // Referencia al Rigidbody2D. Es el componente que mueve
                                // al jugador usando física (velocidad, fuerzas, gravedad).

    // --- Salto ---
    public float jumpForce;     // Fuerza del impulso del salto.
    public bool canJump;        // ¿Puede saltar ahora mismo? Será true solo si está en el piso.

    // --- Detección de suelo ---
    // Para saber si el jugador está pisando el piso, dibujamos un círculo
    // invisible en sus pies y preguntamos: "¿este círculo toca algo del piso?".
    [SerializeField] private Transform groundCheck;      // Objeto vacío colocado en los pies. De él tomamos la POSICIÓN.
    [SerializeField] private float groundCheckRadius;    // Radio (tamaño) de ese círculo invisible.
    [SerializeField] private LayerMask groundLayer;      // Filtro: qué Layers cuentan como "suelo".
                                                         // Así el círculo no detecta enemigos o monedas.

    // --- Animación y orientación ---
    public Animator playerAnimator; // Componente que reproduce las animaciones.
    public bool isFacingRight;      // ¿El sprite está mirando a la derecha? Sirve para no voltearlo dos veces.

    // --- Vida ---
    public float health;                        // Vida actual del jugador.
    [SerializeField] private float maxHealth;   // Vida máxima. Es private porque no debería
                                                // cambiar durante el juego, solo se configura.

    // --- Golpe / knockback (el empujón al recibir daño) ---
    public float hitForce;      // Con cuánta fuerza sale volando el jugador al ser golpeado.
    public float hitTime;       // Cuántos segundos dura el empujón. Mientras sea > 0, el
                                // jugador NO puede controlarse (está "aturdido").
    public bool hitFromRight;   // ¿El golpe vino desde la derecha? Define hacia dónde sale volando.

    // --- Interfaz de usuario (UI) ---
    public TextMeshProUGUI healthText;  // El texto en pantalla donde mostramos la vida.
    


    // ========================================================================
    // START()
    // Unity lo llama UNA SOLA VEZ, automáticamente, antes del primer Update.
    // Se usa para "preparar" todo antes de que empiece el juego:
    // buscar componentes, poner valores iniciales, actualizar la UI, etc.
    // ========================================================================
    void Start()
    {
        // GetComponent<T>() busca un componente de tipo T en ESTE MISMO GameObject.
        // Aquí guardamos el Animator del jugador para poder usarlo después.
        // (Hacerlo así evita tener que arrastrarlo a mano en el Inspector.)
        playerAnimator = GetComponent<Animator>();
        health = maxHealth;  // Al iniciar el juego, la vida es la máxima.
        healthText.text = $"Health: {health}/{maxHealth}";

        // Escribimos la vida en el texto de la UI.
        // El signo $ antes de las comillas crea un "string interpolado":
        // todo lo que va entre { } se reemplaza por el valor de la variable.
        // Resultado en pantalla, por ejemplo: "Health: 100/100"
        healthText.text = $"Health: {health}/{maxHealth}";
    }


    // ========================================================================
    // UPDATE()
    // Unity lo llama AUTOMÁTICAMENTE una vez POR CADA FRAME (cuadro) del juego.
    // Si el juego corre a 60 FPS, este método se ejecuta 60 veces por segundo.
    // Aquí va todo lo que debe revisarse constantemente.
    // ========================================================================
    void Update()
    {
        // ---- 1. ¿ESTOY EN EL PISO? ----
        // Physics2D.OverlapCircle() dibuja un círculo invisible y devuelve
        // true si ese círculo está tocando algún collider de la Layer indicada.
        // Parámetros: (posición del círculo, radio del círculo, layers a detectar)
        // Lo guardamos en canJump: si toca el piso -> true, si está en el aire -> false.
        canJump = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // ---- 2. ¿ME MUEVO YO, O ME ESTÁN EMPUJANDO? ----
        // Usamos hitTime como un "cronómetro". Si está en 0 o menos, significa
        // que no me han golpeado (o el golpe ya pasó) y tengo el control.
        if (hitTime <= 0)
        {
            // MOVIMIENTO NORMAL
            // linearVelocity es la velocidad del Rigidbody2D: un Vector2 (X, Y).
            // Le asignamos un Vector2 nuevo donde:
            //   X = direction * speed  -> la dirección que presiona el jugador por su velocidad.
            //   Y = rb.linearVelocityY -> ¡OJO! Conservamos la velocidad vertical que ya tenía.
            // Si en Y pusiéramos 0, borraríamos el salto y la gravedad cada frame.
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocityY);

            // Le pasamos el valor de "direction" al Animator.
            // En el Animator creamos un parámetro float llamado "Direction" y con él
            // decidimos si se reproduce la animación de quieto o de caminar.
            playerAnimator.SetFloat("direction", direction);
        }
        else // Me golpearon: el jugador pierde el control por unos instantes.
        {
            if (hitFromRight) // Me pegaron por la derecha, entonces salgo hacia la IZQUIERDA.
            {
                // hitForce negativo en X = empujón hacia la izquierda.
                // hitForce positivo en Y = también lo levanta un poco (efecto de "salto" del golpe).
                rb.linearVelocity = new Vector2(-hitForce, hitForce);
            }
            else if (!hitFromRight) // Me pegaron por la izquierda...
            {
                // Me pegaron por la izquierda, entonces salgo hacia la DERECHA.
                rb.linearVelocity = new Vector2(hitForce, hitForce);
            }

            // Descontamos el tiempo del cronómetro del golpe.
            // Time.deltaTime = segundos que pasaron desde el frame anterior.
            // Restarlo cada frame hace que hitTime baje en SEGUNDOS REALES,
            // sin importar si el juego corre a 30, 60 o 144 FPS.
            hitTime -= Time.deltaTime;
        }

        // ---- 3. ¿DEBO VOLTEAR EL SPRITE? ----
        // Comparamos hacia dónde MIRA con hacia dónde SE MUEVE.
        // El signo ! significa "NO" (niega el valor del bool).

        // Si NO está mirando a la derecha PERO se mueve a la derecha -> voltear.
        if (!isFacingRight && direction > 0f)
        {
            Flip();
        }
        // Si SÍ está mirando a la derecha PERO se mueve a la izquierda -> voltear.
        else if (isFacingRight && direction < 0f)
        {
            Flip();
        }
        // Nota: si direction == 0 (está quieto) no entra a ninguno de los dos,
        // así que el jugador se queda mirando hacia donde iba. Correcto.
    }


    // ========================================================================
    // MOVE()  -- Método público llamado por el New Input System
    // ------------------------------------------------------------------------
    // Este método NO lo llama Unity solo: lo llama el componente PlayerInput.
    // En el Inspector, dentro de PlayerInput > Events > Move, se arrastra el
    // jugador y se selecciona PlayerController > Move.
    //
    // InputAction.CallbackContext context = "el paquete de información" que
    // manda el Input System: qué valor tiene la tecla/joystick, en qué estado
    // está la acción (iniciada, ejecutada, cancelada), etc.
    // ========================================================================
    public void Move(InputAction.CallbackContext context)
    {
        // ReadValue<Vector2>() lee el valor de la acción como un Vector2 (X, Y).
        // Lo usamos así porque la acción Move está configurada como Vector2
        // (sirve para WASD, flechas y joystick por igual).
        // Como este es un juego 2D de plataformas, solo nos interesa el eje X,
        // así que le pedimos ".x" y descartamos el Y.
        direction = context.ReadValue<Vector2>().x;
    }


    // ========================================================================
    // JUMP()  -- También lo llama el Input System (evento Jump)
    // ========================================================================
    public void Jump(InputAction.CallbackContext context)
    {
        // Doble condición unida por && ("Y"): AMBAS deben ser verdaderas.
        //   canJump           -> solo salta si está pisando el piso.
        //   context.performed -> solo en el instante en que se PRESIONA la tecla.
        // ¿Por qué context.performed? Porque el Input System llama a este método
        // varias veces (al presionar, al mantener, al soltar). Sin este filtro,
        // el jugador saltaría 2 o 3 veces con un solo toque.
        if (canJump && context.performed)
        {
            // AddForce() aplica una fuerza al Rigidbody2D.
            //   Vector2.up * jumpForce -> un empujón hacia arriba (0, jumpForce).
            //   ForceMode2D.Impulse    -> la fuerza se aplica DE GOLPE, en un
            //                             instante, ideal para un salto.
            //                             (Force, en cambio, la aplica gradualmente.)
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }


    // ========================================================================
    // FLIP()
    // ------------------------------------------------------------------------
    // Método PROPIO (no lo llama Unity, lo llamamos nosotros desde Update).
    // Voltea el sprite del jugador como si fuera un espejo.
    // El truco: multiplicar la escala en X por -1.
    // ========================================================================
    private void Flip()
    {
        isFacingRight = !isFacingRight;             // Cambia el estado de la variable
                                                    // (si era true pasa a false y al revés).

        Vector3 localScale = transform.localScale;  // Guarda la escala actual
                                                    // (transform es el componente de posición,
                                                    //  rotación y escala del GameObject).
        localScale.x *= -1f;                        // Invierte el eje X (espejo)
                                                    // *= significa "multiplícate por".
        transform.localScale = localScale;          // Aplica la nueva escala
                                                    // Hay que reasignarlo completo porque
                                                    // no se puede modificar localScale.x directo.
    }


    // ========================================================================
    // TAKEDAMAGE()
    // ------------------------------------------------------------------------
    // Método público que OTROS scripts llaman (por ejemplo, el enemigo o los
    // pinchos) para hacerle daño al jugador.
    // El parámetro "damage" es cuánta vida se le quita.
    // ========================================================================
    public void TakeDamage(float damage)
    {
        health -= damage;   // -= significa "réstate a ti mismo".
                            // Equivale a: health = health - damage;

        // Actualizamos el texto de la UI para que el jugador vea su vida nueva.
        healthText.text = $"Health: {health}/{maxHealth}";
        playerAnimator.SetTrigger("IsHurt");
    }


    // ========================================================================
    // ADDHEALTH()
    // ------------------------------------------------------------------------
    // Cura al jugador (por ejemplo, al recoger un botiquín).
    // El guion bajo en "_health" es una convención para diferenciar el
    // PARÁMETRO que llega del método, de la VARIABLE "health" de la clase.
    // ========================================================================
    public void AddHealth(float _health)
    {
        // Antes de curar, revisamos que no nos pasemos de la vida máxima.
        if (health + _health > maxHealth)
        {
            health = maxHealth;     // Si se pasaría, la dejamos exactamente en el tope.
        }
        else
        {
            health += _health;      // Si no se pasa, sumamos normalmente.
                                    // += significa "súmate a ti mismo".
        }

        // Y de nuevo actualizamos la UI.
        healthText.text = $"Health: {health}/{maxHealth}";
    }
}
