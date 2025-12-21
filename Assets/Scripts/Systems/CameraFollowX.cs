using UnityEngine;

/// <summary>
/// Kamerayı yalnızca X ekseninde hedefi takip edecek şekilde kaydırır.
/// </summary>
public class CameraFollowX : MonoBehaviour
{
    [Tooltip("Takip edilecek karakter/nesne")]
    public Transform target;

    [Tooltip("Takip edilen x değerine uygulanacak offset")]
    public float xOffset = 0f;

    [Tooltip("Takipteki yumuşatma süresi (düşük değer = daha sıkı takip)")]
    public float smoothTime = 0.15f;

    [Tooltip("Kameranın sola/sağa gidebileceği sınırlar (opsiyonel)")]
    public Vector2 xClamp = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

    [Tooltip("Kameranın x ekseninde geçmesine izin verilmeyen minimum değer.")]
    public float minX = 0f;

    [Tooltip("Kameranın gidebileceği maksimum x değeri (opsiyonel).")]
    public float maxX = float.PositiveInfinity;

    private float velocityX;
    private float fixedY;
    private float fixedZ;

    void Awake()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        float desiredX = target.position.x + xOffset;
        float lowerBound = Mathf.Max(minX, xClamp.x);
        float configuredMax = Mathf.Min(xClamp.y, maxX);
        float upperBound = Mathf.Max(lowerBound, configuredMax);
        desiredX = Mathf.Clamp(desiredX, lowerBound, upperBound);
        float newX = Mathf.SmoothDamp(transform.position.x, desiredX, ref velocityX, smoothTime);
        newX = Mathf.Clamp(newX, lowerBound, upperBound);
        transform.position = new Vector3(newX, fixedY, fixedZ);
    }
}
