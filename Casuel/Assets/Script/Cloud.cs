using UnityEngine;

public class Cloud : MonoBehaviour
{
    public float speed;
    public int xDirection;
    public float bounds;
    public float boundsMinus;

    void Update()
    {
        transform.Translate(xDirection * speed * Time.deltaTime, 0, 0);
        if (transform.position.x < boundsMinus || transform.position.x > bounds)
        {
            Destroy(gameObject);
        }
    }
}
