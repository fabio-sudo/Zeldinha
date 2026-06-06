using UnityEngine;

//MAQUINA DE ESTADOS
public enum enemyState
{
   IDLE,
   EXPLORE,
   ALERT,
   PATROL,
   FURY
}

public class GameManager : MonoBehaviour
{

    [Header("Slime IA")]
    public Transform[] slimeWayPoints;//Posições de Patrulha do slime


}
