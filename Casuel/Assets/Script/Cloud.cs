using UnityEngine;

public class Cloud : MonoBehaviour
{
    public float speed;
    public int xDirection;
    public int bounds;

    void Update()
    {
        transform.Translate(xDirection * speed * Time.deltaTime, 0, 0);
        if (transform.position.x < -bounds || transform.position.x > bounds)
        {
            Destroy(gameObject);
        }
    }
}
