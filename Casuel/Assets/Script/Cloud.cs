using UnityEngine;

public class Cloud : MonoBehaviour
{
    public float speed;
    public int xDirection;

    void Update()
    {
        transform.Translate(xDirection * speed * Time.deltaTime, 0, 0);
        if (transform.position.x < -15 || transform.position.x > 15)
        {
            Destroy(gameObject);
        }
    }
}
