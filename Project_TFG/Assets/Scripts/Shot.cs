using UnityEngine;

public class Shot : MonoBehaviour
{
    private Rigidbody mRB;
    private float speed = 2000f;
    private ShipControllerAgent shooter;

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }

    private void Awake()
    {
        mRB = GetComponent<Rigidbody>();
        mRB.AddRelativeForce(Vector3.up * speed);
        shooter = transform.parent.GetComponent<ShipControllerAgent>();
        transform.parent = null;
    }

    public ShipControllerAgent GetShooter()
    {
        return shooter;
    }

}
