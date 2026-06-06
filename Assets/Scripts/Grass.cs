using UnityEngine;

public class Grass : MonoBehaviour
{

    public ParticleSystem fxGrass;
    private bool isCut = false;

    public void GetHit(int damage)
    {
        if (!isCut)
        {
            isCut = true;
            fxGrass.Emit(Random.Range(10, 20));
            transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        }

    }
}
