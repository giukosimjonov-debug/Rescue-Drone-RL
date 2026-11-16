using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DroneAgent : Agent
{
    [Header("References")]
    public Transform target;

    [Header("Flight Settings")]
    public float moveSpeed = 7f;

    private Rigidbody rb;
    private Vector3 startPos;
    private float bestDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localPosition = startPos + new Vector3(
            Random.Range(-1.5f, 1.5f),
            0f,
            Random.Range(-1.5f, 1.5f)
        );

        if (target != null)
        {
            target.localPosition = GetSafeRandomTargetPosition();
            bestDistance = Vector3.Distance(transform.localPosition, target.localPosition);
        }
    }

    private Vector3 GetSafeRandomTargetPosition()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 candidateLocal = new Vector3(
                Random.Range(-7.5f, 8.0f),
                Random.Range(1.2f, 2.5f),
                Random.Range(-7.5f, 8.0f)
            );

            if (Vector3.Distance(candidateLocal, transform.localPosition) < 4.0f)
                continue;

            Vector3 worldPos = transform.parent != null
                ? transform.parent.TransformPoint(candidateLocal)
                : candidateLocal;

            Collider[] hits = Physics.OverlapSphere(worldPos, 0.8f);
            bool hitWall = false;
            foreach (var h in hits)
            {
                if (h.CompareTag("Wall"))
                {
                    hitWall = true;
                    break;
                }
            }

            if (!hitWall)
                return candidateLocal;
        }

        // Zaxira nuqta
        return new Vector3(7f, 1.5f, 7f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Jami 9 ta kuzatuv (Space Size = 9)
        Vector3 toTarget = (target.localPosition - transform.localPosition) / 20f;
        sensor.AddObservation(toTarget);
        sensor.AddObservation(rb.linearVelocity / moveSpeed);
        sensor.AddObservation(transform.localPosition / 10f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);

        rb.linearVelocity = new Vector3(moveX, moveY, moveZ) * moveSpeed;

        AddReward(-0.001f);

        float currentDistance = Vector3.Distance(transform.localPosition, target.localPosition);
        if (currentDistance < bestDistance)
        {
            float progress = bestDistance - currentDistance;
            AddReward(progress * 0.1f);
            bestDistance = currentDistance;
        }

        if (transform.localPosition.y > 4.2f || transform.localPosition.y < 0.25f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(2.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        float x = 0f, y = 0f, z = 0f;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) z -= 1f;
            if (kb.spaceKey.isPressed) y += 1f;
            if (kb.leftShiftKey.isPressed) y -= 1f;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        if (Input.GetKey(KeyCode.Space)) y = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) y = -1f;
#endif

        continuousActions[0] = x;
        continuousActions[1] = y;
        continuousActions[2] = z;
    }
}
