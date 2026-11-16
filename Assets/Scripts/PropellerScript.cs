using UnityEngine;

public class PropellerSpin : MonoBehaviour
{
    public float spinSpeed = 1500f;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
    }
}
