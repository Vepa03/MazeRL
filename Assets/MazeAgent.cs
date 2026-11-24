using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class MazeAgent : Agent
{
    [Header("Sahne referansları")]
    [Tooltip("Hedef küp (Tag = Goal, Collider Is Trigger = açık)")]
    public Transform goal;

    [Header("Hareket")]
    public float moveSpeed = 2.5f;     // birim / saniye
    public float turnSpeed = 180f;     // derece / saniye

    [Header("Başlangıç bölgeleri (TrainingArea'ya göre yerel)")]
    public Vector2 agentSpawnX = new Vector2(-4f, 4f);
    public Vector2 agentSpawnZ = new Vector2(-4.3f, -2.5f);
    public Vector2 goalSpawnX  = new Vector2(-4f, 4f);
    public Vector2 goalSpawnZ  = new Vector2(2.5f, 4.3f);

    [Header("İsteğe bağlı görsel geri bildirim")]
    public Renderer floorRenderer;
    public Material winMaterial;
    public Material loseMaterial;

    private Rigidbody rb;
    private const float ArenaHalfSize = 5f;   // zemin 10x10 => yarısı 5

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        
        rb.linearVelocity = Vector3.zero;      // Unity 6. Unity 2022 kullanıyorsanız: rb.velocity
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(
            Random.Range(agentSpawnX.x, agentSpawnX.y), 0.4f,
            Random.Range(agentSpawnZ.x, agentSpawnZ.y));
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        
        goal.localPosition = new Vector3(
            Random.Range(goalSpawnX.x, goalSpawnX.y), 0.4f,
            Random.Range(goalSpawnZ.x, goalSpawnZ.y));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 2 + 2 + 2 + 2 = 8 gözlem (hepsi yaklaşık -1..1 aralığında)
        Vector3 aPos = transform.localPosition / ArenaHalfSize;
        Vector3 gPos = goal.localPosition / ArenaHalfSize;
        sensor.AddObservation(new Vector2(aPos.x, aPos.z));
        sensor.AddObservation(new Vector2(gPos.x, gPos.z));
        sensor.AddObservation(new Vector2(transform.forward.x, transform.forward.z));

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity) / moveSpeed;
        sensor.AddObservation(new Vector2(localVel.x, localVel.z));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        Vector3 moveDir = Vector3.zero;
        float turn = 0f;

        switch (action)
        {
            case 1: moveDir = transform.forward;  break;
            case 2: moveDir = -transform.forward; break;
            case 3: turn = -1f; break;
            case 4: turn = 1f;  break;
        }

        
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f));

        
        if (MaxStep > 0) AddReward(-1f / MaxStep);

        
        if (transform.localPosition.y < -1f)
        {
            SetReward(-1f);
            EndWithColor(loseMaterial);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            SetReward(1f);
            EndWithColor(winMaterial);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall")) AddReward(-0.05f);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall")) AddReward(-0.001f);
    }

    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        if (Input.GetKey(KeyCode.W)) d[0] = 1;
        else if (Input.GetKey(KeyCode.S)) d[0] = 2;
        else if (Input.GetKey(KeyCode.A)) d[0] = 3;
        else if (Input.GetKey(KeyCode.D)) d[0] = 4;
    }

    private void EndWithColor(Material m)
    {
        if (floorRenderer != null && m != null)
        {
            floorRenderer.material = m;
            CancelInvoke(nameof(ResetFloorColor));
            Invoke(nameof(ResetFloorColor), 0.5f);
        }
        EndEpisode();
    }

    private Material defaultFloorMaterial;
    private void Start()
    {
        if (floorRenderer != null) defaultFloorMaterial = floorRenderer.sharedMaterial;
    }
    private void ResetFloorColor()
    {
        if (floorRenderer != null && defaultFloorMaterial != null)
            floorRenderer.material = defaultFloorMaterial;
    }
}
