using UnityEngine;

public class CloudManager: MonoBehaviour
{
    [SerializeField] GameObject[] _clouds;
    [SerializeField] float _cloudSpeed;
    [SerializeField] float _cloudDelay;
    float timer;


    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0 )
        {
            Debug.Log("pp");
            int tempX;
            int tempRand = Random.Range(1, 3);
            int tempXStart;
            if ( tempRand == 1 )
            {
                tempX = -1;
                tempXStart = 5;
            }
            else
            {
                tempX = 1;
                tempXStart = -5;
            }


            GameObject tempCloud = Instantiate(_clouds[Random.Range(0, _clouds.Length)],new Vector3(tempXStart, Random.Range(-5, 5), 0), transform.rotation);

            tempCloud.GetComponent<Cloud>().speed = _cloudSpeed;
            tempCloud.GetComponent<Cloud>().xDirection = tempX;

            timer = _cloudDelay;

        
        }
    }
}
