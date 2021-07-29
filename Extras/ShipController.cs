using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class ShipController : MonoBehaviour
{
    private Rigidbody mRB;
    private float verticalSpeed = 1.0f;
    private float horizontalSpeed = 0.5f;
    private float maxSpeed = 10.0f;
    private Vector3 angleSpeed = new Vector3(0, 150, 0);
    private Flag flag = null;
    private bool wpcd = true;
    private ShipAgent agent;
    private SimpleMultiAgentGroup allyTeam;
    private SimpleMultiAgentGroup enemyTeam;

    public ShipEnvController env;
    public GameObject shot;
    public Transform shooter;
    public GameObject enemyFlag;
    public GameObject allyBase;

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject == allyBase && flag != null)
        {
            flag.Point();
            flag = null;
            agent.AddReward(10);
            allyTeam.AddGroupReward(100);
            enemyTeam.AddGroupReward(-10);
        }
        else if (other.gameObject == enemyFlag)
        {
            flag = other.GetComponent<Flag>();
            flag.Emparent(transform);
            agent.AddReward(1);
            allyTeam.AddGroupReward(10);
            enemyTeam.AddGroupReward(-1);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Shot"))
        {
            if (collision.gameObject.GetComponent<Shot>().GetShooter().CompareTag(tag)){
                if (flag != null)
                {
                    flag.CarrierDestroyed();
                    flag = null;
                    allyTeam.AddGroupReward(-5);
                }
                collision.gameObject.GetComponent<Shot>().GetShooter().AddReward(-1);
            }
            else
            {
                if (flag != null)
                {
                    flag.CarrierDestroyed();
                    flag = null;
                    allyTeam.AddGroupReward(-5);
                    enemyTeam.AddGroupReward(5);
                }
                collision.gameObject.GetComponent<Shot>().GetShooter().AddReward(1);
            }
        } else {
            if (flag != null)
            {
                flag.CarrierDestroyed();
                flag = null;
                allyTeam.AddGroupReward(-1);
            }
        }
        agent.AddReward(-1);
        wpcd = true;
        //env.Respawn(this);
    }

    void Awake()
    {
        mRB = GetComponent<Rigidbody>();
        agent = GetComponent<ShipAgent>();
    }


    void FixedUpdate()
    {
        Vector3 vel = mRB.velocity;
        if (vel.magnitude > maxSpeed)
        {
            mRB.velocity = vel.normalized * maxSpeed;
        }
    }

    public void MFB(float direction)
    {
        mRB.AddRelativeForce(Vector3.forward * direction * verticalSpeed, ForceMode.Impulse);
    }

    public void MLR(float direction)
    {
        mRB.AddRelativeForce(Vector3.right * direction * horizontalSpeed, ForceMode.Impulse);
    }

    public void Turn(float direction)
    {
        Quaternion deltaRotation = Quaternion.Euler(angleSpeed * direction * Time.fixedDeltaTime);
        mRB.MoveRotation(mRB.rotation * deltaRotation);
    }

    public void Firing()
    {
        if (wpcd)
        {
            Instantiate(shot, shooter.position, shooter.rotation, transform);
            StartCoroutine(WPCD());
        }
    }

    public void setTeams(SimpleMultiAgentGroup newAllyTeam, SimpleMultiAgentGroup newEnemyTeam)
    {
        allyTeam = newAllyTeam;
        enemyTeam = newEnemyTeam;
    }

    public bool hasFlag()
    {
        bool result = false;
        if (flag != null) result = true;
        return result;
    }

    public SimpleMultiAgentGroup GetTeam()
    {
        return allyTeam;
    }

    public void SetEnv(ShipEnvController newEnv)
    {
        env = newEnv;
    }

    public bool GetWPCD()
    {
        return wpcd;
    }
    public void SetWPCD(bool nwpcd)
    {
        wpcd = nwpcd;
    }

    IEnumerator WPCD()
    {
        wpcd = false;
        yield return new WaitForSeconds(1);
        wpcd = true;
    }

}
