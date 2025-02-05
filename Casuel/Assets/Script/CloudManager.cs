using UnityEngine;

public class CloudManager: MonoBehaviour
{
    [SerializeField] GameObject[] _clouds;
    [SerializeField] float _cloudSpeed;
    [SerializeField] float _cloudDelay;
    [SerializeField] float _xSpawningRange;
    [SerializeField] float _ySpawningRange;
    float timer;


    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0 )
        {
            int tempX;
            int tempRand = Random.Range(1, 3);
            float tempXStart;
            if ( tempRand == 1 )
            {
                tempX = -1;
                tempXStart = transform.position.x + _xSpawningRange;
            }
            else
            {
                tempX = 1;
                tempXStart = transform.position.x - _xSpawningRange;
            }


            GameObject tempCloud = Instantiate(_clouds[Random.Range(0, _clouds.Length)],new Vector3(tempXStart,transform.position.y + Random.Range(-_ySpawningRange, _ySpawningRange), 0), transform.rotation);

            tempCloud.GetComponent<Cloud>().speed = _cloudSpeed;
            tempCloud.GetComponent<Cloud>().xDirection = tempX;
            tempCloud.GetComponent<Cloud>().bounds =  transform.position.x + _xSpawningRange + 5;
            tempCloud.GetComponent<Cloud>().boundsMinus = transform.position.x - (_xSpawningRange + 5);
            timer = _cloudDelay;

        
        }
    }
}
