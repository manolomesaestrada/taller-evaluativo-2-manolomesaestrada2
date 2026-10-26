using Unity.Cinemachine; 
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Configuración de Cantidades")]
    [SerializeField] private float smallAmount = 10f;
    [SerializeField] private float largeAmount = 40f;

    



    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null) {
            string itemTag = gameObject.tag;
            

            switch (itemTag) {
                case "a1":
                    player.AddHealth(smallAmount);
                    break;

                case "a2":
                    player.AddHealth(largeAmount);
                    break;

                case "a3": 
                    player.TakeDamage(smallAmount);
                    GetComponent<CinemachineImpulseSource>().GenerateImpulse(smallAmount);
                    break;

                case "a4": 
                    player.TakeDamage(largeAmount);
                    GetComponent<CinemachineImpulseSource>().GenerateImpulse(largeAmount);
                    break;

                default:
                    Debug.LogWarning("Tag no reconocido: " + itemTag);
                    break;
            }

            


            
            Destroy(gameObject);
        }
    }
}