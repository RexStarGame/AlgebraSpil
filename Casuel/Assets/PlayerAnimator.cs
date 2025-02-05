using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] front;
    [SerializeField] private Sprite[] leftside;
    [SerializeField] private Sprite[] rightSide;
    [SerializeField] private Sprite[] back;
    bool nextStep;
    bool stepSpamPrevention;
    private string lastDirection;
    private bool rightLeg;
    private float playerX;
    private float playerY;
    private Transform player;
    private SpriteRenderer spriteRenderer;

    public bool directionLocked;

    private void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        player = GetComponentInParent<Transform>();
        spriteRenderer.sprite = front[0];
        lastDirection = "Front";
    }

    private void Update()
    {

        if (playerX < player.position.x)
        {
            Animate("Right", true, directionLocked);
        }
        else if (playerX > player.position.x)
        {
            Animate("Left", true, directionLocked);
        }
        else if (playerY > player.position.y)
        {
            Animate("Front", true, directionLocked);
        }
        else if (playerY < player.position.y)
        {
            Animate("Back", true, directionLocked);
        }
        else if (playerY == player.position.y && playerX == player.position.x)
        {
            Animate(lastDirection, false, directionLocked);
        }
        playerX = player.position.x;
        playerY = player.position.y;

    }

    private void Animate(string direction, bool isMoving, bool directionLocked)
    {

        if (!isMoving)
        {
            if(direction == "Front")
            {
                spriteRenderer.sprite = front[0];
            }
            else if (direction == "Back")
            {
                spriteRenderer.sprite = back[0];
            }
            else if (direction == "Left")
            {
                spriteRenderer.sprite = leftside[0];
            }
            else if (direction == "Right")
            {
                spriteRenderer.sprite = rightSide[0];
            }

            if(stepSpamPrevention)
            {
                nextStep = !nextStep;
                stepSpamPrevention = false;
            }


        }
        else if (isMoving)
        {

            if (!directionLocked)
            {
                lastDirection = direction;
            }



            if (lastDirection == "Left")
            {
                spriteRenderer.sprite = leftside[1];
                
            }
            else if (lastDirection == "Right")
            {
                spriteRenderer.sprite = rightSide[1];
                
            }

            if (lastDirection == "Front")
            {
                if (rightLeg && nextStep)
                {
                    spriteRenderer.sprite = front[1];
                    rightLeg = false;
                }
                else if (!rightLeg && !nextStep)
                {
                    spriteRenderer.sprite = front[2];
                    rightLeg |= true;
                }
                
            }
            else if (lastDirection == "Back")
            {
                if (rightLeg && nextStep)
                {
                    spriteRenderer.sprite = back[1];
                    rightLeg = false;
                }
                else if (!rightLeg && !nextStep)
                {
                    spriteRenderer.sprite = back[2];
                    rightLeg |= true;
                }
                
            }

            if (!stepSpamPrevention)
            {
                stepSpamPrevention = true;
            }
        }




        return;
    }
}
