using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using static UnityEngine.UI.Image;
using Unity.VisualScripting;
using Unity.Hierarchy;
using UnityEditor.Experimental.GraphView;

public class PlayerControllerMobile : MonoBehaviour
{
    [Header("Movement & Grid")]
    public float moveSpeed = 5f; // Bevægelseshastighed
    public float gridSize = 1f; // Størrelsen af hver celle i gitteret
    public Vector2 gridOrigin = Vector2.zero;  // Startpunktet for gitteret
    public int gridWidth = 10; // Antal celler i bredden
    public int gridHeight = 10; // Antal celler i højden
    private bool lastUpLeft = true;

    private bool lastDownLeft = true;
    [Header("Swipe Settings")]
    public float minSwipeDistance = 50f; // Minimum pixelafstand for at registrere et swipe

    [Header("Push Settings")]
    public Transform pushPoint; // Punkt hvor objektet placeres foran spilleren
    public float pushRange = 1.5f; // Rækkevidde for at "mærke" pushable

    [Header("Highlight Materials")]
    public Material highlightMaterial; // Materiale til highlighting af objekter

    // -- Private variabler til logik --
    private Rigidbody2D rb; // Reference til RigidBody2D
    private Vector2 movement; // Bevægelsesretning
    private Vector2 targetPosition; // Position, spilleren er på vej mod
    private bool isMoving = false; // Bevæger sig i øjeblikket

    private GameObject currentPushable; // Hvilket objekt skubbes pt.
    private bool isPushing = false;  // Skubber vi et objekt?
    private Vector2 pushDirection; // Retningen, der skubbes i
    private Vector2 lockedDirection; // Låst retning til rotation/animation
    private Vector2 lockedPushDirection = Vector2.zero; // Låser bevægelse til skubberetning
    [SerializeField] private Transform pushRayOriginLeft;
    [SerializeField] private Transform pushRayOriginRight;
    [SerializeField] private Transform pushRayOriginTop;
    //[SerializeField] private Transform pushRayOriginBottom;
    
    // Swipe-input
    private Vector2 startTouchPos;
    private Vector2 endTouchPos;
    private bool swipeDetected = false;

    // Tap to push a object. 
    private bool isTapped = false;
    [SerializeField] private GameObject[] pushAbleObject;
    private Vector2 isSelected;
    // Double tap-registrering
    private float tapTime = 0f; // Tidsstempel for sidste tap
    private float doubleTapDelay = 0.3f; // Maksimal tid mellem taps for at registrere dobbelt tap
    [SerializeField] private LayerMask pushableLayer; // LayerMask til at filtrere kun pushable objekter
    public float boxWidth = 1.0f; // Bredden af BoxCast
    public float boxHeight = 0.5f; // Højden af BoxCast
    private GameObject lastTappedPushable;
    public float adaptiveSwipePercentage = 0.04f;  // Fx 4% af skærmbredden.
    public float smoothingFactor = 0.5f;           // Lerp-faktor (prøv evt. 0.3f for mere direkte respons).
    public float swipeCooldown = 0.2f;             // Cooldown-tid mellem swipes.
    public float swipeDurationThreshold = 0.2f;      // Tærskel for meget korte swipes.
    public LayerMask swipeIgnoreLayers;            // Lag, der ignoreres ved swipe (fx UI eller andre uønskede objekter).

    // Private variabler til swipe-håndtering.
    private float lastSwipeTime = 0f;
    private bool fingerDown = false;

    public bool canMoveRight = true;
    public bool canMoveLeft = true;
    public bool canMoveDown = true;
    public bool canMoveUp = true;

    private float fingerStartTime;
    private Vector2 fingerStartPos;
    // Gemmer originalt materiale, så highlight kan nulstilles
    private Material originalMaterial;
    [Header("Grid Settings")]
    public CameraFollow2D cameraController;
    // Data til blokering af celler i grid
    [System.Serializable]
    public class GridCell
    {
        public Vector2Int coordinates;
        public bool isBlocked;
        public bool isPermanent;
    }
    public List<GridCell> blockedCells = new List<GridCell>();
    public void ExpandGrid(Vector2 newOrigin, int newWidth, int newHeight)
    {
        // Kald denne metode når griddet udvides
        if (cameraController != null)
        {
            cameraController.UpdateGridBounds(newOrigin, newWidth, newHeight);
        }

        // Tilføj din grid-udvidelseslogik her
    }
    // Tilføj disse private variabler i klassen

    private float fingerUpTime;
    private Vector2 fingerUpPos;
    private Vector2 swipeStartPos;
    public Animator animator;
    private bool lastLegWasLeft = false;

    // 📌 Offsets for swipe-bevægelse (Justeres i Unity Inspector)
    [SerializeField] private float shortSwipeOffsetMultiplierPublic = 0.3f;
    [SerializeField] private float longSwipeOffsetMultiplierPublic = 1f;
    [SerializeField] private float softSwipeThresholdPublic = 0.5f;
    public List<GameObject> allowedPushableObjects;
    private int activePushTouchId = -1;

    private Vector2 startTouchPosition;
    private float startTime;
    public float tapTimeThreshold = 0.2f; // Adjust as needed
    public float swipeDistanceThreshold = 50f; // Adjust as needed
    // 📌 Lagmasker
    [SerializeField] private LayerMask playerLayer;
    // Offentlige parametre – juster dem i inspektøren:
    // Indsæt dette øverst i klassen
    private const float boxCastDistance = 1.0f; // Justér denne værdi til det ønskede interval

    private Vector2 playerMovementDirection = Vector2.zero;

    [Header("Debug Settings")]
    public bool showDebugGizmos = true; // Toggle debug visualizations

    private GameObject playerAnimation;

    // ***** Add this missing variable *****
    private Vector2 lastGridCell;
    [Header("Background Settings")]
    [SerializeField] private string backgroundTag = "Backgrund";
    [SerializeField] private float backgroundZPosition = 10f;
    [SerializeField] private Vector2 backgroundOffset = Vector2.zero; // Juster i Inspect
    public GameObject[] allBackgrounds;
    [SerializeField] private bool useGridUnitsForOffset = true;
    // Tilføj TryMove-metoden
    private void TryMove(Vector2 direction)
    {
        Vector2 potentialTarget = RoundToGrid((Vector2)transform.position + direction * gridSize);

        // Check if the target cell is blocked
        if (IsCellBlocked(potentialTarget))
        {
            Debug.Log($"Cannot move to {potentialTarget}: Cell is blocked.");
            isMoving = false; // Stop any attempt to move
            return;
        }
        if (isPushing && currentPushable != null)
        {
            // Objektets nuværende position
            Vector2 objPos = currentPushable.transform.position;
            // Objektets potentielle position
            Vector2 objectTarget = RoundToGrid(objPos + direction * gridSize);

            // Tjek om objektets celle er fri
            // (Evt. kald: IsCellBlocked(objectTarget, "Pushable") 
            //  hvis du har speciel logik for pushable)
            if (IsCellBlocked(objectTarget))
            {
                Debug.Log("Objektet er blokeret ⇒ Spiller kan heller ikke bevæge sig.");
                isMoving = false;
                return;
            }
            /*
            if (movementIsValid)
            {
                targetPosition = ...;
                isMoving = true;
                return true;
            }
            */
            // Hvis objektet KAN flyttes, gem destinationen til "FixedUpdate"/coroutine
            // objectTargetPosition = objectTarget; // fx
        }
        // If the target cell is valid, proceed with movement
        if (IsInsideGrid(potentialTarget))
        {
            targetPosition = potentialTarget;
            isMoving = true;

        }
        else
        {
            Debug.Log($"Cannot move to {potentialTarget}: Out of bounds.");
        }
    }
    void Start()
    {
        playerAnimation = GameObject.Find("PlayerGraphic");

        InitializeBlockedCells(); // Din eksisterende initialisering
        CacheBackgrounds();
        ProcessAllBackgrounds();
        rb = GetComponent<Rigidbody2D>();

        // Justér spillerens startposition til grid
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;
        animator = GetComponent<Animator>(); // Ensure your Animator component is attached or assigned.
        // (Valgfrit) Debug-liste over pushables
        DebugPushableObjects();

        // Bloker celler ud fra eksisterende objekter
        InitializeBlockedCells();
    }

    private void DebugPushableObjects()
    {
        GameObject[] pushableObjects = GameObject.FindGameObjectsWithTag("Pushable");
        foreach (GameObject obj in pushableObjects)
        {
            Debug.Log($"Pushable Object: {obj.name}, " +
                      $"Position: {obj.transform.position}, " +
                      $"Collider: {obj.GetComponent<Collider2D>() != null}, " +
                      $"Rigidbody: {obj.GetComponent<Rigidbody2D>() != null}");
        }
    }
    void Update()
    {

        if (IsPushing && currentPushable != null)
        {
            float rayDistance = 0.25f;      // How far to check for obstacles

            LayerMask obstacleMask = LayerMask.GetMask("PushableObjects");
            string pushableTag = "Pushable";

            Vector2 upOrigin = pushRayOriginTop.position;
            
            // Calculate ray origins
            Vector2 rightOrigin = pushRayOriginRight.position;

            Vector2 leftOrigin = pushRayOriginLeft.position;
            RaycastHit2D upHit = Physics2D.Raycast(upOrigin, pushRayOriginTop.right,
                rayDistance, obstacleMask);
            
               
            // Cast rays in both directions
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, pushRayOriginRight.right,
                                    rayDistance, obstacleMask);

            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, -pushRayOriginLeft.right,
                                   rayDistance, obstacleMask);
            float tempDirection = Mathf.RoundToInt(transform.rotation.eulerAngles.z);

          

            // Debug visualization

            // Check left side
            if (leftHit.collider != null &&
                (leftHit.collider.CompareTag(pushableTag) ||
                 leftHit.collider.gameObject.CompareTag("Wall")))
            {
                if (tempDirection == 0) // up
                {
                    
                    DisablelMovementleft();
                }
                else if (tempDirection == 270) // right
                {
                    DisablelMovementUp();
                }
                else if (tempDirection == 90) // left
                {
                    DisablelMovementDown();
                }
                else if(tempDirection == 180) // down
                {
                    DisablelMovementRight();
                }
            }
            else
            {
                if (tempDirection == 0) // up
                {
                    EnableMovementLeft();
                }
                else if (tempDirection == 270) // right
                {
                    EnableMovementUp();
                }
                else if (tempDirection == 90) // left
                {
                    EnableMovementDown();

                }
                else if (tempDirection == 180) // down
                {
                    EnableMovementRight();
                }
            }
            if (rightHit.collider != null &&
                (rightHit.collider.CompareTag(pushableTag) ||
                 rightHit.collider.gameObject.CompareTag("Wall")))
            {
                if (tempDirection == 0) // up
                {
                    DisablelMovementRight();
                }
                else if (tempDirection == 270) // right
                {
                    DisablelMovementDown();
                }
                else if (tempDirection == 90) // left
                {
                    DisablelMovementUp();
                }
                else if (tempDirection == 180) // down
                {
                    DisablelMovementleft();
                }
            }
            else
            {
                if (tempDirection == 0) // up
                {
                    EnableMovementRight();
                }
                else if (tempDirection == 270) // right
                {
                    EnableMovementDown();
                }
                else if (tempDirection == 90) // left
                {
                    EnableMovementUp();

                }
                else if (tempDirection == 180) // down
                {
                    EnableMovementLeft();
                }
            }
            if (upHit.collider != null &&
                (upHit.collider.CompareTag(pushableTag) ||
                 upHit.collider.gameObject.CompareTag("Wall")))
            {
                if (tempDirection == 0) // up
                {
                    DisablelMovementUp();
                }
                else if (tempDirection == 270) // right
                {
                    DisablelMovementRight();
                }
                else if (tempDirection == 90) // left
                {
                    DisablelMovementleft();
                }
                else if (tempDirection == 180) // down
                {
                    DisablelMovementDown();
                }
            }
            else
            {
                if (tempDirection == 0) // up
                {
                    EnableMovementUp();
                }
                else if (tempDirection == 270) // right
                {
                    EnableMovementRight();
                }
                else if (tempDirection == 90) // left
                {
                    EnableMovementLeft();

                }
                else if (tempDirection == 180) // down
                {
                    EnableMovementDown();
                }
            }
        }

        if (Input.touchCount == 1)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startTouchPosition = touch.position;
                    startTime = Time.time;
                    break;

                case TouchPhase.Ended:
                    float touchDuration = Time.time - startTime;
                    Vector2 endTouchPosition = touch.position;
                    Vector2 touchDelta = endTouchPosition - startTouchPosition;

                    if (touchDuration <= tapTimeThreshold && touchDelta.magnitude < swipeDistanceThreshold)
                    {
                        // Tap detected
                        OnTap(touchDelta);
                    }
                    else if (touchDelta.magnitude >= swipeDistanceThreshold)
                    {
                        // Swipe detected
                        OnSwipe(touchDelta);
                    }
                    break;
            }
        }
        HandleTouch();
        //HandlePushInteraction();
        UpdateDynamicBlockers();
        // Only cancel pushing when the touch that initiated it has ended.
        if (activePushTouchId != -1)
        {
            // Look through all touches for the one with our recorded fingerId.
            bool pushTouchStillActive = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == activePushTouchId)
                {
                    // If this touch has ended, cancel the push.
                    if (Input.GetTouch(i).phase == TouchPhase.Ended ||
                        Input.GetTouch(i).phase == TouchPhase.Canceled)
                    {
                        StopPushingObject();
                        activePushTouchId = -1;
                    }
                    else
                    {
                        pushTouchStillActive = true;
                    }
                    break;
                }
            }

            // Optionally, if the recorded touch is not found among current touches,
            // it means it ended already so we cancel pushing:
            if (!pushTouchStillActive)
            {
                StopPushingObject();
                activePushTouchId = -1;
            }
        }
    }
    private void OnTap(Vector2 tapPosition)
    {
        HandlePushInteraction();
    }

    private void OnSwipe(Vector2 swipeDelta)
    {
        HandleSwipeMovement();
    }

    private void ResetMovementBools()
    {
        animator.SetBool("MoveLeftRight", false);
        animator.SetBool("MoveUpRight", false);
        animator.SetBool("MoveUpLeft", false);
        animator.SetBool("DownRightFeet", false);
        animator.SetBool("DownLeftFeet", false);
        animator.SetBool("IsMoving", false);
    }

    // Klassefelter – placer disse uden for HandleTouch() i din klasse

    private IEnumerator ResetAfterMovement(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetMovementBools();
        animator.SetTrigger("Idle");
    }

    private void HandleTouch()
    {
        if (Input.touchCount > 0)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    fingerDown = true;
                    swipeDetected = false;
                    fingerStartTime = Time.time;
                    fingerStartPos = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (fingerDown)
                    {
                        Vector2 swipeDelta = touch.position - fingerStartPos;
                        if (swipeDelta.magnitude >= minSwipeDistance)
                        {
                            Vector2 centerPos = RoundToGrid(transform.position);
                            float tolerance = 0.1f;
                            if (Vector2.Distance(transform.position, centerPos) > tolerance)
                            {
                                return;
                            }
                            swipeDetected = true;
                            Vector2 direction = Vector2.zero;

                            // Nulstil alle bevægelsesparametre før ny input
                            ResetMovementBools();

                            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                            {
                                // Horizontal swipe.
                                direction = (swipeDelta.x > 0) ? Vector2.right : Vector2.left;
                                Flip(direction);
                                animator.SetBool("MoveLeftRight", true);
                            }
                            else
                            {
                                // Vertical swipe.
                                if (swipeDelta.y > 0)
                                {
                                    // Opadgående swipe.
                                    direction = Vector2.up;
                                    Flip(direction);
                                    if (lastUpLeft)
                                    {
                                        animator.SetBool("MoveUpRight", true);
                                        animator.SetBool("MoveUpLeft", false);
                                    }
                                    else
                                    {
                                        animator.SetBool("MoveUpLeft", true);
                                        animator.SetBool("MoveUpRight", false);
                                    }
                                    lastUpLeft = !lastUpLeft;
                                }
                                else
                                {
                                    // Nedadgående swipe.
                                    direction = Vector2.down;
                                    Flip(direction);
                                    if (lastDownLeft)
                                    {
                                        animator.SetBool("DownRightFeet", true);
                                        animator.SetBool("DownLeftFeet", false);
                                    }
                                    else
                                    {
                                        animator.SetBool("DownLeftFeet", true);
                                        animator.SetBool("DownRightFeet", false);
                                    }
                                    lastDownLeft = !lastDownLeft;
                                    animator.SetTrigger("Idle");
                                }
                            }
                            TryMove(direction);
                            animator.SetBool("IsMoving", true);

                            Vector2 currentGridCell = RoundToGrid(transform.position);
                            // Her kan du tilføje yderligere logik, hvis nødvendigt.
                            lastGridCell = currentGridCell;

                            fingerDown = false;
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    fingerDown = false;
                    if (!swipeDetected)
                    {
                        ResetMovementBools();
                        animator.SetTrigger("Idle");
                    }
                    else
                    {
                        StartCoroutine(ResetAfterMovement(0.2f));
                    }
                    break;
            }
        }
        else
        {
            fingerDown = false;
        }
    }

    private void HandlePushInteraction()
    {
        if (Input.touchCount > 0)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                swipeStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                Vector2 swipeDelta = touch.position - swipeStartPos;
                float softSwipeThreshold = softSwipeThresholdPublic;
                Vector2 forwardDirection = lockedDirection;

                if (forwardDirection == Vector2.zero)
                {
                    forwardDirection = (swipeDelta.magnitude > 0)
                        ? GetCardinalDirection(swipeDelta)
                        : Vector2.up;
                }

                bool isTap = swipeDelta.magnitude < softSwipeThreshold;
                Vector2 tapWorldPos = Camera.main.ScreenToWorldPoint(touch.position);

                // Bestem parametre baseret på om det er et tap eller swipe
                Vector2 origin = isTap
                    ? tapWorldPos
                    : (Vector2)transform.position + forwardDirection * (gridSize * (isTap ? shortSwipeOffsetMultiplierPublic : longSwipeOffsetMultiplierPublic));

                Vector2 boxSize = isTap
                    ? new Vector2(0.1f, 0.1f)  // Lille boks til tap-detektion
                    : new Vector2(boxWidth * 0.5f, boxHeight * 0.5f);

                Vector2 direction = isTap ? Vector2.zero : forwardDirection;
                float distance = isTap ? 0f : gridSize;

                LayerMask effectivePushableLayer = pushableLayer & ~playerLayer;
                RaycastHit2D hit = Physics2D.BoxCast(origin, boxSize, 0f, direction, distance, effectivePushableLayer);

                GameObject tappedObject = null;
                if (hit.collider != null && hit.collider.CompareTag("Pushable"))
                {
                    // Ekstra check for tap-position inden i collider
                    if (isTap)
                    {
                        Collider2D tappedCollider = hit.collider;
                        if (!tappedCollider.bounds.Contains(tapWorldPos))
                        {
                            return;
                        }
                    }
                    tappedObject = hit.collider.gameObject;
                }

                if (tappedObject != null)
                {
                    bool isAllowed = allowedPushableObjects.Contains(tappedObject);
                    if (!isAllowed) return;

                    if (Time.time - tapTime < doubleTapDelay && lastTappedPushable == tappedObject)
                    {
                        if (currentPushable == null)
                        {
                            TryStartPushing(tappedObject);
                        }
                        else if (currentPushable == tappedObject)
                        {
                            StopPushingObject();
                        }
                        lastTappedPushable = null;
                    }
                    else
                    {
                        lastTappedPushable = tappedObject;
                        tapTime = Time.time;
                    }
                }
            }
        }
    }
    private void FixedUpdate()
    {
        if (isPushing && currentPushable != null)
        {
            // Her er push logic, men uden lockedDirection/pushDirection-lås
            HandleSwipeMovement();

            if (movement != Vector2.zero)
            {
                Vector2 targetPos = RoundToGrid((Vector2)transform.position + movement * gridSize);

                if (IsInsideGrid(targetPos))
                {
                    StartCoroutine(MoveToTarget(rb.position, targetPos, 0.20f));
                    animator.SetBool("IsMoving", true);

                    Vector2 objectTargetPos = RoundToGrid(targetPos + pushDirection * gridSize);

                    if (IsInsideGrid(objectTargetPos, false) && !IsCellBlocked(objectTargetPos, "Pushable"))
                    {
                        StartCoroutine(MovePushableToTarget(currentPushable, currentPushable.transform.position, objectTargetPos, 0.20f));
                        pushPoint.position = objectTargetPos;
                    }
                    else
                    {
                        Debug.Log("Pushable movement blocked.");
                    }
                }
                else
                {
                    Debug.Log("Blocked movement: Target cell is blocked or out of grid.");
                }
                movement = Vector2.zero;
            }
        }
        else
        {
            if (isMoving)
            {
                Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);

                if (IsCellBlocked(targetPosition))
                {
                    Debug.Log("Movement blocked: Target position is occupied.");
                    isMoving = false;
                    targetPosition = rb.position;
                }
                else
                {
                    rb.MovePosition(newPosition);

                    if (Vector2.Distance(rb.position, targetPosition) < 0.01f)
                    {
                        rb.position = targetPosition;
                        isMoving = false;
                    }
                }
            }
        }

        // Endnu en push-linje: 
        if (isPushing && currentPushable != null)
        {
            HandleSwipeMovement();

            if (movement != Vector2.zero)
            {
                Vector2 targetPos = RoundToGrid((Vector2)transform.position + movement * gridSize);

                if (IsInsideGrid(targetPos))
                {
                    rb.MovePosition(targetPos);
                    Vector2 objectTargetPos = RoundToGrid(targetPos + pushDirection * gridSize);

                    if (IsInsideGrid(objectTargetPos, false) && !IsCellBlocked(objectTargetPos, "Pushable"))
                    {
                        currentPushable.transform.position = objectTargetPos;
                        pushPoint.position = objectTargetPos;
                    }
                    else
                    {
                        Debug.Log("Pushable movement blocked again.");
                    }
                }
                else
                {
                    Debug.Log("Player movement blocked.");
                }
                movement = Vector2.zero;
            }
        }
    }
    private void UpdateAnimatorBools(Vector2 direction)
    {
        animator.SetBool("Right", direction == Vector2.right);
        animator.SetBool("Left", direction == Vector2.left);
        animator.SetBool("Up", direction == Vector2.up);
        animator.SetBool("Down", direction == Vector2.down);
    }
    // Tilføj disse klassevariable, hvis de ikke allerede findes:
    private void HandleSwipeMovement()
    {
        // Calculate adaptive swipe threshold based on screen width
        float adaptiveMinSwipe = Screen.width * adaptiveSwipePercentage;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Check if touch hits ignored layers
            Vector2 worldTouchPos = Camera.main.ScreenToWorldPoint(touch.position);
            if (Physics2D.OverlapPoint(worldTouchPos, swipeIgnoreLayers) != null)
            {
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (Time.time - lastSwipeTime < swipeCooldown) return;
                    fingerDown = true;
                    swipeDetected = false;
                    fingerStartTime = Time.time;
                    fingerStartPos = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (fingerDown)
                    {
                        Vector2 currentTouchPos = touch.position;
                        Vector2 swipeDelta = currentTouchPos - fingerStartPos;

                        Vector2 finalSwipeDirection = Vector2.Lerp(swipeDelta,
                            swipeDelta.normalized * adaptiveMinSwipe, smoothingFactor);

                        float swipeDuration = Time.time - fingerStartTime;

                        if (finalSwipeDirection.magnitude >= adaptiveMinSwipe ||
                            swipeDuration < swipeDurationThreshold)
                        {
                            lastSwipeTime = Time.time;
                            swipeDetected = true;
                            fingerDown = false;

                            Handheld.Vibrate();

                            Vector2 direction = Vector2.zero;
                            bool isValidSwipe = true;

                            if (Mathf.Abs(finalSwipeDirection.x) > Mathf.Abs(finalSwipeDirection.y))
                            {
                                // Horizontal movement check
                                if (finalSwipeDirection.x > 0)
                                {
                                    if (!canMoveRight) isValidSwipe = false;
                                    direction = Vector2.right;
                                }
                                else
                                {
                                    if (!canMoveLeft) isValidSwipe = false;
                                    direction = Vector2.left;
                                }

                                UpdateAnimatorBools(direction);
                            }
                            else
                            {
                               
                                    // Vertical movement (add canMoveUp/canMoveDown if needed)
                                direction = finalSwipeDirection.y > 0 ? Vector2.up: Vector2.down;
                                if (direction == Vector2.up && !canMoveUp) isValidSwipe = false;
                                if (direction == Vector2.down && !canMoveDown) isValidSwipe = false;   
                                UpdateAnimatorBools(direction);
                            }

                            if (!isValidSwipe)
                            {
                                animator.SetBool("IsMoving", false);
                                return;
                            }

                            // Unified movement handling for both input methods
                            HandleMovementInput(direction);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    fingerDown = false;
                    if (!swipeDetected)
                    {
                        animator.SetBool("IsMoving", false);
                    }
                    break;
            }
        }
        else
        {
            // Handle keyboard/controller input
            float moveInput = Input.GetAxis("Horizontal");
            Vector2 keyboardDirection = Vector2.zero;

            if (moveInput > 0 && canMoveRight)
            {
                keyboardDirection = Vector2.right;
            }
            else if (moveInput < 0 && canMoveLeft)
            {
                keyboardDirection = Vector2.left;
            }

            if (keyboardDirection != Vector2.zero)
            {
                HandleMovementInput(keyboardDirection);
            }
            else
            {
               // animator.SetBool("IsMoving", false);
            }
        }
    }
    private void HandleMovementInput(Vector2 direction)
    { 
        movement = direction;
        if (isMoving)  // Now properly returns a boolean
        {
            StartCoroutine(MoveToTarget(rb.position, targetPosition, 0.20f));
            animator.SetBool("IsMoving", true);
        }
        else
        {
            // Optional: Handle invalid movement
            animator.SetBool("IsMoving", false);
        }     
    }
    // Coroutine til glidende bevægelse for spilleren.
    private IEnumerator MoveToTarget(Vector2 startPos, Vector2 targetPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Simpel ease-out funktion: t -> 1 - (1-t)^2.
            float t = elapsed / duration;
            t = 1 - Mathf.Pow(1 - t, 2);
            rb.position = Vector2.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.position = targetPos;
        isMoving = false;
        animator.SetBool("IsMoving", false);
    }

    // Coroutine til glidende bevægelse for et pushable objekt.
    private IEnumerator MovePushableToTarget(GameObject pushable, Vector2 startPos, Vector2 targetPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            pushable.transform.position = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        pushable.transform.position = targetPos;
    }
    private bool IsInsideGrid(Vector2 position, bool checkBlocked = true)
    {
        // Tjek grid-grænser
        float xMin = gridOrigin.x;
        float xMax = gridOrigin.x + gridWidth * gridSize;
        float yMin = gridOrigin.y;
        float yMax = gridOrigin.y + gridHeight * gridSize;

        // Tjek, om positionen er indenfor grid-grænserne
        if (position.x < xMin || position.x >= xMax ||
            position.y < yMin || position.y >= yMax)
        {
            return false;
        }

        // Hvis checkBlocked er true, tjek om cellen er blokeret
        if (checkBlocked && IsCellBlocked(position))
        {
            Debug.Log($"Blocked cell at {position}");
            return false;
        }

        return true;
    }

    private Vector2 RoundToGrid(Vector2 position)
    {
        float x = Mathf.Round((position.x - gridOrigin.x) / gridSize) * gridSize + gridOrigin.x;
        float y = Mathf.Round((position.y - gridOrigin.y) / gridSize) * gridSize + gridOrigin.y;
        return new Vector2(x, y);
    }

    private bool IsCellBlocked(Vector2 position, string tagToCheck = null)
    {
        Vector2Int cellCoords = WorldToGridCoordinates(position);

        foreach (GridCell cell in blockedCells)
        {
            if (cell.coordinates == cellCoords && cell.isBlocked)
            {
                if (tagToCheck == "Pushable")
                {
                    // Allow Pushables to move into Obstacles
                    GameObject obstacle = GetObjectAtCell(cellCoords, "Obstacle");
                    if (obstacle != null) continue; // Ignore and allow movement

                    // Prevent Pushables from entering other Pushables
                    GameObject pushable = GetObjectAtCell(cellCoords, "Pushable");
                    if (pushable != null) return true;
                }
                else
                {
                    // Always block for the player
                    return true;
                }
            }
        }

        return false;
    }


    private GameObject GetObjectAtCell(Vector2Int cellCoords, string tag)
    {
        // Adjust the check area to be slightly larger than a point
        Vector2 worldPosition = new Vector2(
            gridOrigin.x + cellCoords.x * gridSize,
            gridOrigin.y + cellCoords.y * gridSize
        );

        Vector2 checkSize = new Vector2(gridSize * 0.8f, gridSize * 0.8f); // Slightly smaller than grid cell
        Collider2D[] colliders = Physics2D.OverlapBoxAll(worldPosition, checkSize, 0f);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(tag))
            {
                return collider.gameObject;
            }
        }

        return null; // No object of the specified tag found
    }


    private Vector2Int WorldToGridCoordinates(Vector2 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / gridSize);
        int y = Mathf.RoundToInt((worldPosition.y - gridOrigin.y) / gridSize);
        return new Vector2Int(x, y);
    }

    private void RemoveFromBlockedCells(Vector2Int cellCoords)
{
    // Only remove non-permanent (pushable) cells
    blockedCells.RemoveAll(cell => cell.coordinates == cellCoords && !cell.isPermanent);
}

    private void AddToBlockedCells(Vector2Int cellCoords, bool isPermanent)
    {
        GridCell existingCell = blockedCells.FirstOrDefault(c => c.coordinates == cellCoords);

        if (existingCell != null)
        {
            if (isPermanent)
            {
                existingCell.isBlocked = true;
                existingCell.isPermanent = true;
            }
            else if (!existingCell.isPermanent) // Only update if not already permanently blocked
            {
                existingCell.isBlocked = true;
                existingCell.isPermanent = false;
            }
        }
        else
        {
            blockedCells.Add(new GridCell
            {
                coordinates = cellCoords,
                isBlocked = true,
                isPermanent = isPermanent
            });
        }
    }

    private void UpdateBlockedCells(GameObject pushableObject, Vector2 oldPosition, Vector2 newPosition)
    {
        Vector2Int oldCoords = WorldToGridCoordinates(oldPosition);
        Vector2Int newCoords = WorldToGridCoordinates(newPosition);

        // Remove pushable block from the old cell (if not permanent)
        RemoveFromBlockedCells(oldCoords);

        // Reblock the old cell if it contains an obstacle
        GameObject obstacle = GetObjectAtCell(oldCoords, "Obstacle");
        if (obstacle != null)
        {
            AddToBlockedCells(oldCoords, true);
        }

        // Add the pushable block to the new cell
        AddToBlockedCells(newCoords, false);

        Debug.Log($"Pushable moved: Old {oldCoords} -> New {newCoords}");
    }

    private bool HasOtherPushableAtPosition(Vector2 position, GameObject currentObject)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(position);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Pushable") && collider.gameObject != currentObject)
                return true;
        }
        return false;
    }
    private void UpdateDynamicBlockers()
    {
        // Fjern midlertidige blokeringer
        blockedCells.RemoveAll(cell => !cell.isPermanent);

        // Tilføj blokering for alle Pushable-objekter
        GameObject[] pushableObjects = GameObject.FindGameObjectsWithTag("Pushable");

        foreach (GameObject pushable in pushableObjects)
        {
            Vector2Int coords = WorldToGridCoordinates(pushable.transform.position);

            // Ensure currentPushable is not null before accessing its position
            if (currentPushable != null && coords == WorldToGridCoordinates(currentPushable.transform.position))
                continue;

            AddToBlockedCells(coords, false);
        }
    }

    private void TryStartPushing(GameObject pushableObject)
    {
        if (pushableObject == null) return; // Sørg for, at objektet ikke er null

        // Sæt det aktuelle objekt som det pushbare
        currentPushable = pushableObject;
        isPushing = true;
        playerAnimation.GetComponent<PlayerAnimator>().directionLocked = true;
        // Find retningen fra spiller -> objekt => nærmeste kardinal
        Vector2 pushDir = (currentPushable.transform.position - transform.position).normalized;
        lockedPushDirection = GetCardinalDirection(pushDir);
        pushDirection = lockedPushDirection;

        Debug.Log($"Starter med at skubbe: {currentPushable.name}, Retning: {lockedPushDirection}");
        
        // Highlight
        HighlightObject(currentPushable);

        // Flip retning
        Flip(lockedPushDirection);
        RemoveFromBlockedCells(WorldToGridCoordinates(currentPushable.transform.position));
    }
    void DisablelMovement()
    {
        /*
       if(transform.rotation.z == 0)
        {
            canMoveUp = false;
        }
       else if (transform.rotation.z == -90)
        {
            canMoveRight = false; // kan ikke gå til højer vis sand.
        }
       else if(transform.rotation.z == -180)
        {
            canMoveDown = false;
        }
       else if (transform.rotation.z == 90)
        {
            canMoveLeft = false;
        }
        */
    }
    void DisablelMovementRight()
    {
        
        
            canMoveRight = false; // kan ikke gå til højer vis sand.
        
    }
    void DisablelMovementleft()
    {
        
        
            canMoveLeft = false;
        
    }
    void DisablelMovementUp()
    {
        
            canMoveUp = false;
     
    }
    void DisablelMovementDown()
    {
      
     
            canMoveDown = false;
      
    }
    void EnableMovementRight()
    {
       
      
            canMoveRight = true; // kan ikke gå til højer vis sand.
        
    }
    void EnableMovementUp()
    {
        
        
            canMoveUp = true;
       
    }
    void EnableMovementLeft()
    {
       
      
            canMoveLeft = true;
      
    }
    void EnableMovementDown()
    {
        
      
            canMoveDown = true;
        
    }
    void EnableMovement()
    {
        /*
        if (transform.rotation.z == 0)
        {
            canMoveUp = true;
        }
        else if (transform.rotation.z == -90)
        {
            canMoveRight = true; // kan ikke gå til højer vis sand.
        }
        else if (transform.rotation.z == -180)
        {
            canMoveDown = true;
        }
        else if (transform.rotation.z == 90)
        {
            canMoveLeft = true;
        }
        */
    }
    
    

    private void StopPushingObject()
    {
        if (currentPushable != null)
        {
            // Remove highlight effect
            RemoveHighlight(currentPushable);

            // Get the grid coordinates of the current pushable
            Vector2Int coords = WorldToGridCoordinates(currentPushable.transform.position);

            // Ensure the pushable cell remains dynamically blocked
            AddToBlockedCells(coords, false); // Mark as a temporary block

            Debug.Log($"Cell at {coords} updated for Pushable object: {currentPushable.name}");
        }
       
        // Reset pushing state
        playerAnimation.GetComponent<PlayerAnimator>().directionLocked = false;
        isPushing = false;
        currentPushable = null;
        pushDirection = Vector2.zero;
        lockedPushDirection = Vector2.zero;

        // Force dynamic blockers to update immediately
        UpdateDynamicBlockers();
    }
    private Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return new Vector2(Mathf.Sign(direction.x), 0f); // Horisontalt
        else
            return new Vector2(0f, Mathf.Sign(direction.y)); // Vertikalt
    }
    private void Flip(Vector2 direction)
    {
        if (direction == Vector2.zero) return; // Ingen retning at flippe til

        // Behold låst retning, hvis spilleren skubber
        if (isPushing && lockedDirection != Vector2.zero)
        {
            direction = lockedDirection;
        }
        else
        {
            lockedDirection = direction; // Opdater låst retning
        }

        // Beregn vinkel og rotér spilleren
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (Mathf.RoundToInt(transform.rotation.eulerAngles.z) == 0)
        {
            Debug.Log("ARRRRRRRR!!!!!!!");
            playerAnimation.GetComponent<PlayerAnimator>().TurnBack();
        }
        else if (Mathf.RoundToInt(transform.rotation.eulerAngles.z) == 270)
        {
            Debug.Log("ARRRRRRRR!!!!!!!");
            playerAnimation.GetComponent<PlayerAnimator>().TurnRight();
        }
        else if (Mathf.RoundToInt(transform.rotation.eulerAngles.z) == 90)
        {
            Debug.Log("ARRRRRRRR!!!!!!!");
            playerAnimation.GetComponent<PlayerAnimator>().TurnLeft();
        }
        else if (Mathf.RoundToInt(transform.rotation.eulerAngles.z) == 180)
        {
            Debug.Log("ARRRRRRRR!!!!!!!");
            playerAnimation.GetComponent<PlayerAnimator>().TurnFront();
        }
        // Opdater pushPoint-position baseret på den nye retning
        UpdatePushPointPosition(direction);
    }
    private void UpdatePushPointPosition(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)transform.position + direction * gridSize;
        pushPoint.position = RoundToGrid(newPosition);
    }

    private void HighlightObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (originalMaterial == null)
                originalMaterial = renderer.material;

            renderer.material = highlightMaterial;
        }
    }
    private void RemoveHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
            originalMaterial = null;
        }
    }

    private void InitializeBlockedCells()
    {
        // 1) Tilføj "BlockEverything" som permanent block
        GameObject[] blockEverything = GameObject.FindGameObjectsWithTag("BlockEverything");
        foreach (GameObject blockObj in blockEverything)
        {
            Vector2Int coords = WorldToGridCoordinates(blockObj.transform.position);
            AddToBlockedCells(coords, true); // Altid permanent, så ingen kan rydde det
        }
        // Add obstacles as permanent blocks
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            Vector2Int coords = WorldToGridCoordinates(obstacle.transform.position);
            AddToBlockedCells(coords, true); // Permanent block
        }

        // Add pushables as dynamic blocks
        GameObject[] pushables = GameObject.FindGameObjectsWithTag("Pushable");
        foreach (GameObject pushable in pushables)
        {
            Vector2Int coords = WorldToGridCoordinates(pushable.transform.position);
            AddToBlockedCells(coords, false); // Dynamic block
        }

        // Iterate through the grid for additional validation
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector2 cellPosition = new Vector2(
                    gridOrigin.x + x * gridSize,
                    gridOrigin.y + y * gridSize
                );

                Collider2D[] colliders = Physics2D.OverlapPointAll(cellPosition);

                foreach (Collider2D col in colliders)
                {
                    Vector2Int cellCoords = new Vector2Int(x, y);

                    // Ensure permanent blocking for obstacles
                    if (col.CompareTag("Obstacle"))
                    {
                        AddToBlockedCells(cellCoords, true);
                    }
                    // Ensure permanent blocking for "BlockEverything"
                    else if (col.CompareTag("BlockEverything"))
                    {
                        AddToBlockedCells(cellCoords, true);
                    }
                    // Ensure dynamic blocking for pushables
                    else if (col.CompareTag("Pushable"))
                    {
                        AddToBlockedCells(cellCoords, false);
                    }
                }
            }
        }
    }
    private void ProcessAllBackgrounds()
    {
        if (allBackgrounds == null) return;

        foreach (GameObject bg in allBackgrounds.Where(b => b != null))
        {
            Vector2Int nearestCell = FindNearestGridCell(bg.transform.position);
            Vector2 cellCenter = GridCellToWorldCenter(nearestCell);

            // Ændring: Inverter Y-aksen for grid offset
            Vector2 adjustedOffset = useGridUnitsForOffset ?
                new Vector2(
                    backgroundOffset.x * gridSize,
                    -backgroundOffset.y * gridSize // Inverter Y-retning
                ) :
                new Vector2(backgroundOffset.x, -backgroundOffset.y);

            bg.transform.position = new Vector3(
                cellCenter.x + adjustedOffset.x,
                cellCenter.y + adjustedOffset.y,
                backgroundZPosition
            );

            FitToGridCell(bg);
        }
    }

    private Vector2Int FindNearestGridCell(Vector2 worldPosition)
    {
        Vector2 gridRelativePos = worldPosition - gridOrigin;
        return new Vector2Int(
            Mathf.RoundToInt(gridRelativePos.x / gridSize),
            Mathf.RoundToInt(gridRelativePos.y / gridSize)
        );
    }

    private Vector2 GridCellToWorldCenter(Vector2Int gridCoords)
    {
        return new Vector2(
            gridOrigin.x + (gridCoords.x * gridSize) + (gridSize * 0.5f),
            gridOrigin.y + (gridCoords.y * gridSize) + (gridSize * 0.5f)
        );
    }
    private void CacheBackgrounds()
    {
        allBackgrounds = GameObject.FindGameObjectsWithTag(backgroundTag);
    }
    private void FitToGridCell(GameObject background)
    {
        if (background == null) return;

        SpriteRenderer sr = background.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Bounds spriteBounds = sr.sprite.bounds;
        float maxDimension = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);
        if (maxDimension <= 0) return;

        float scaleFactor = gridSize / maxDimension;
        background.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
    }

    public void RefreshBackgrounds()
    {
        CacheBackgrounds();
        ProcessAllBackgrounds();
    }
    private void OnDrawGizmos()
    {
        // 🔲 Draw Grid
        Gizmos.color = Color.green;
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector2 cellPos = new Vector2(gridOrigin.x + x * gridSize, gridOrigin.y + y * gridSize);
                Gizmos.DrawWireCube(cellPos, new Vector3(gridSize, gridSize, 0));
            }
        }

        // 🟥 Draw Blocked Cells
        foreach (GridCell cell in blockedCells)
        {
            Vector2 cellPos = new Vector2(
                gridOrigin.x + cell.coordinates.x * gridSize,
                gridOrigin.y + cell.coordinates.y * gridSize
            );

            if (cell.isPermanent)
            {
                Gizmos.color = Color.red; // Obstacles 🟥 (Always blocked)
            }
            else
            {
                Gizmos.color = Color.yellow; // Pushables 🔲 (Dynamically blocked)
            }

            Gizmos.DrawCube(cellPos, new Vector3(gridSize * 0.9f, gridSize * 0.9f, 0)); // Slightly smaller to visualize grid
        }

        // 🔴 If pushing a block, draw a line from player to pushable
        if (isPushing && currentPushable != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentPushable.transform.position);
        }
        else
        {
            // 🔵 Draw a ray in the player's direction (push range)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.up * pushRange);
        }

        // ---- Added BoxCast Visualization for OnTap() ----
        // This part visualizes the BoxCast used in OnTap().
        // For visualization, we use the current mouse position as a stand-in for a tap position.
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Brug spillerens position og retning til at bestemme origin og retning
        Vector2 playerPosition = (Vector2)transform.position;
        Vector2 forwardDirection = transform.up; // Bruger nu spillerens op-retning
        Vector2 forwardOffset = forwardDirection * 0.5f;
        Vector2 boxCastOrigin = playerPosition + forwardOffset;

        // Brug den samme boxstørrelse som i OnTap()
        Vector2 baseBoxSize = new Vector2(boxWidth * 0.5f, boxHeight * 0.5f);
        Vector2 boxSize = baseBoxSize;

        // Tegn BoxCast som en wireframe
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boxCastOrigin, boxSize);

        // Tegn en linje for at repræsentere BoxCast-afstanden (nu i spillerens retning)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(boxCastOrigin, boxCastOrigin + forwardDirection * boxCastDistance);

        if (allBackgrounds == null) return;

        Gizmos.color = Color.magenta;
        foreach (var bg in allBackgrounds.Where(b => b != null))
        {
            Gizmos.DrawWireCube(bg.transform.position, new Vector3(gridSize, gridSize, 0.1f));
        }
    }
    public bool IsPushing
    {
    get 
    { 
        return isPushing;
    }
}
}