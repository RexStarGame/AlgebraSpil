using System.Threading;
using Unity.Cinemachine;
using UnityEngine;

public class FancyCam : MonoBehaviour
{
    private CinemachineCamera cam;
    private GameObject player;
    [SerializeField] private GameObject WinScreen;
    [SerializeField] private float timer;
    private float tempTimer;
    private bool spamPrev;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        player = GameObject.Find("PlayerBody");
        GameObject brazier = GameObject.Find("Brazier");
        cam = GameObject.Find("CinemachineCamera").GetComponent<CinemachineCamera>();
        cam.Follow = brazier.transform;
        player.GetComponent<PlayerControllerMobile>().enabled = false;
        tempTimer = timer;
        spawmPrev = true;
    }



    // Update is called once per frame
    void Update()
    {

        if (tempTimer > 0)
        {
            tempTimer -= Time.deltaTime;
        }else if (tempTimer <=0 && spamPrev)
        {
            WinScreen.SetActive(true);
            spamPrev = false;
        }
    }
}
