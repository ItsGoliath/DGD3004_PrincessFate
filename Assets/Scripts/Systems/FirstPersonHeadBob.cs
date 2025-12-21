using UnityEngine;

/// <summary>
/// Head bob + hafif sway/roll: hýzýna göre kamera pivotunu yukarý-aþaðý ve yana oynatýr, küçük roll verir.
/// Mini First Person Controller (FirstPersonMovement + Rigidbody) ile uyumlu.
/// </summary>
public class FirstPersonHeadBob : MonoBehaviour
{
    [SerializeField] private FirstPersonMovement movement; // hýz bilgisi için
    [SerializeField] private Transform cameraPivot;        // genelde kameranýn parent'ý
    [SerializeField] private bool enableBob = true;

    [Header("Genel")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobSpeedThreshold = 0.1f;
    [SerializeField] private float returnLerp = 10f;

    [Header("Genlikler")]
    [SerializeField] private float verticalAmplitude = 0.05f;
    [SerializeField] private float lateralAmplitude = 0.02f;
    [SerializeField] private float rollAngle = 1.5f; // dereceler

    private Vector3 defaultLocalPos;
    private Quaternion defaultLocalRot;
    private float bobTimer;

    private void Awake()
    {
        if (cameraPivot == null)
            cameraPivot = transform;
        defaultLocalPos = cameraPivot.localPosition;
        defaultLocalRot = cameraPivot.localRotation;
        if (movement == null)
            movement = GetComponentInParent<FirstPersonMovement>();
    }

    private void Update()
    {
        if (!enableBob || cameraPivot == null)
            return;

        Vector3 horizVel = Vector3.zero;
        if (movement != null)
        {
            var rb = movement.GetComponent<Rigidbody>();
            if (rb != null)
                horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        float speed = horizVel.magnitude;
        if (speed > bobSpeedThreshold)
        {
            // hýz ile frekans ölçekle
            bobTimer += speed * bobFrequency * Time.deltaTime;

            float bobY = Mathf.Sin(bobTimer * 2f) * verticalAmplitude;   // daha hýzlý yukarý-aþaðý
            float bobX = Mathf.Sin(bobTimer) * lateralAmplitude;         // yana sway
            float roll = Mathf.Sin(bobTimer) * rollAngle;                // küçük roll

            cameraPivot.localPosition = defaultLocalPos + new Vector3(bobX, bobY, 0f);
            cameraPivot.localRotation = defaultLocalRot * Quaternion.Euler(0f, 0f, roll);
        }
        else
        {
            // yumuþak geri dönüþ
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, defaultLocalPos, returnLerp * Time.deltaTime);
            cameraPivot.localRotation = Quaternion.Slerp(cameraPivot.localRotation, defaultLocalRot, returnLerp * Time.deltaTime);
            bobTimer = 0f;
        }
    }
}
