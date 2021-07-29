using System.Collections;
using UnityEngine;

public class Flag : MonoBehaviour
{
    private bool waiting = false;
    private BoxCollider mBC;
    private MeshRenderer[] mMR;
    private Transform ogp;
    private bool ship = false;

    void Awake()
    {
        ogp = transform.parent;
        mBC = GetComponent<BoxCollider>();
        mMR = GetComponentsInChildren<MeshRenderer>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (waiting && ship)
        {
            StopCoroutine(InWait());
            waiting = false;
        }
    }

    public void Emparent(Transform other)
    {
        transform.parent = other;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        ship = true;
    }

    public void CarrierDestroyed()
    {
        StartCoroutine(InWait());
    }

    public void Point()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        transform.parent = ogp;
        ship = false;
        mBC.enabled = false;
        mMR[0].enabled = false;
        mMR[1].enabled = false;

        yield return new WaitForSeconds(10);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        mBC.enabled = true;
        mMR[0].enabled = true;
        mMR[1].enabled = true;
    }

    IEnumerator InWait()
    {
        transform.parent = ogp;
        ship = false;
        waiting = true;

        yield return new WaitForSeconds(10);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        waiting = false;
    }
}
