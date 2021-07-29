using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ShipControllerAgent : Agent
{
    private Rigidbody mRB;
    private float verticalSpeed = 1.0f;
    private float horizontalSpeed = 0.5f;
    private float maxSpeed = 10.0f;
    private Vector3 angleSpeed = new Vector3(0, 150, 0);
    private Flag flag = null;
    private bool wpcd = true;
    private SimpleMultiAgentGroup allyTeam;
    private SimpleMultiAgentGroup enemyTeam;
    private ShipEnvController env;

    public GameObject shot;
    public Transform shooter;
    public GameObject enemyFlag;
    public GameObject allyFlag;
    public GameObject allyBase;

    private void OnTriggerEnter(Collider other)
    {
        //triggering ally base carrying enemy flag
        if (other.gameObject == allyBase && flag != null)
        {
            //give reward to ship and ally team, penalize enemy team, reset flag
            flag.Point();
            flag = null;
            AddReward(10);
            allyTeam.AddGroupReward(100);
            enemyTeam.AddGroupReward(-10);
        }
        //or if triggering the enemy flag
        else if (other.gameObject == enemyFlag)
        {
            //give reward to ship and ally team, penalize enemy team, grab flag
            flag = other.GetComponent<Flag>();
            flag.Emparent(transform);
            AddReward(1);
            allyTeam.AddGroupReward(10);
            enemyTeam.AddGroupReward(-1);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        //if the ship is hit by a shot...
        if (collision.gameObject.CompareTag("Shot"))
        {
            //...and is fired by an ally ship
            if (collision.gameObject.GetComponent<Shot>().GetShooter().CompareTag(tag))
            {
                //if this ship has the flag, reduce ally team points, drop flag
                if (flag != null)
                {
                    flag.CarrierDestroyed();
                    flag = null;
                    allyTeam.AddGroupReward(-5);
                }
                //penalize firing ship
                collision.gameObject.GetComponent<Shot>().GetShooter().AddReward(-1);
            }
            else //...or is fired by an enemy ship
            {
                //if this ship has the flag, reduce ally team points, give enemy team points, drop flag
                if (flag != null)
                {
                    flag.CarrierDestroyed();
                    flag = null;
                    allyTeam.AddGroupReward(-5);
                    enemyTeam.AddGroupReward(5);
                }
                //bonus point for firing ship
                collision.gameObject.GetComponent<Shot>().GetShooter().AddReward(1);
            }
        }
        //if hit by another ship while carrying the flag...
        else if(collision.gameObject.GetComponent<ShipControllerAgent>() && flag != null)
        {
            //...and is an ally ship, reduce ally team points, penalize hitting ship
            if (collision.gameObject.CompareTag(tag))
            {
                allyTeam.AddGroupReward(-5);
                collision.gameObject.GetComponent<ShipControllerAgent>().AddReward(-1);
            }
            else //...or is an enemy ship, reduce ally team points, give enemy team points, give bonus to hitting ship
            {
                allyTeam.AddGroupReward(-5);
                enemyTeam.AddGroupReward(5);
                collision.gameObject.GetComponent<ShipControllerAgent>().AddReward(2);
            }
            //drop flag
            flag.CarrierDestroyed();
            flag = null;
        }
        //when hit by anything else
        else
        {
            //if carrying the flag, reduce ally team points, drop flag
            if (flag != null)
            {
                flag.CarrierDestroyed();
                flag = null;
                allyTeam.AddGroupReward(-1);
            }
        }

        //reduce own points and respawn
        AddReward(-1);
        env.Respawn(this);
    }

    public override void Initialize()
    {
        mRB = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //regulate the max velocity the ship can take to increase manoeuvrability
        Vector3 vel = mRB.velocity;
        if (vel.magnitude > maxSpeed)
        {
            mRB.velocity = vel.normalized * maxSpeed;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //get 1 to fire a shot
        if (actionBuffers.DiscreteActions[0] == 1 && wpcd)
        {
            Firing();
            //AddReward(0.001f);
        }

        //get 1 to move backwards and 2 to move forward
        if (actionBuffers.DiscreteActions[1] == 1)
        {
            MFB(-1);
        }
        else if (actionBuffers.DiscreteActions[1] == 2)
        {
            MFB(1);
        }

        //get 1 to move left and 2 to move right
        if (actionBuffers.DiscreteActions[2] == 1)
        {
            MLR(-1);
        }
        else if (actionBuffers.DiscreteActions[2] == 2)
        {
            MLR(1);
        }

        //get 1 to turn left and 2 to turn right
        if (actionBuffers.DiscreteActions[3] == 1)
        {
            Turn(-1);
        }
        else if (actionBuffers.DiscreteActions[3] == 2)
        {
            Turn(1);
        }

        //rewards upon action
        //if not carrying flag, existential malus to ally team and ship
        if (!hasFlag())
        {
            AddReward(-0.00001f * Vector3.Distance(enemyFlag.transform.position, transform.position));
            //AddReward(0.00001f * (100 - Vector3.Distance(enemyFlag.transform.position, transform.position)));
            allyTeam.AddGroupReward(-0.00001f * Vector3.Distance(enemyFlag.transform.position, transform.position));
        }
        //if carrying flag, existential malus to enemy team and ship, bonus to ally team
        else
        {
            AddReward(-0.00001f * Vector3.Distance(allyBase.transform.position, transform.position));
            //AddReward(0.00001f * (100 - Vector3.Distance(allyBase.transform.position, transform.position)));
            allyTeam.AddGroupReward(0.0001f * (100 - Vector3.Distance(allyBase.transform.position, transform.position)));
            enemyTeam.AddGroupReward(-0.0001f * (100 - Vector3.Distance(allyBase.transform.position, transform.position)));
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //orientation and velocity of the ship
        sensor.AddObservation(transform.forward);

        sensor.AddObservation(mRB.velocity);

        //distance and direction to ally base
        sensor.AddObservation(Vector3.Distance(allyBase.transform.position, transform.position));

        sensor.AddObservation((allyBase.transform.position - transform.position).normalized);

        //distance and direction to enemy flag
        sensor.AddObservation(Vector3.Distance(enemyFlag.transform.position, transform.position));

        sensor.AddObservation((enemyFlag.transform.position - transform.position).normalized);

        //distance and direction to ally flag
        sensor.AddObservation(Vector3.Distance(allyFlag.transform.position, transform.position));

        sensor.AddObservation((allyFlag.transform.position - transform.position).normalized);

        //weapon availability
        sensor.AddObservation(wpcd);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int firing = 0;
        int mfb = 0;
        int mlr = 0;
        int turn = 0;

        //Space to shoot
        if (Input.GetKey(KeyCode.Space)) firing = 1;

        //W to move forwards, S for backwards
        if (Input.GetKey(KeyCode.W)) mfb = 2;
        if (Input.GetKey(KeyCode.S)) mfb = 1;

        //D to move right, A to move left
        if (Input.GetKey(KeyCode.D)) mlr = 2;
        if (Input.GetKey(KeyCode.A)) mlr = 1;

        //E to turn right, Q to turn left
        if (Input.GetKey(KeyCode.E)) turn = 2;
        if (Input.GetKey(KeyCode.Q)) turn = 1;

        actionsOut.DiscreteActions.Array[0] = firing;
        actionsOut.DiscreteActions.Array[1] = mfb;
        actionsOut.DiscreteActions.Array[2] = mlr;
        actionsOut.DiscreteActions.Array[3] = turn;
    }

    //move forward with direction>0, backwards with direction<0
    public void MFB(float direction)
    {
        mRB.AddRelativeForce(Vector3.forward * direction * verticalSpeed, ForceMode.Impulse);
    }

    //move right with direction>0, left with direction<0
    public void MLR(float direction)
    {
        mRB.AddRelativeForce(Vector3.right * direction * horizontalSpeed, ForceMode.Impulse);
    }

    //turn right with direction>0, left with direction<0
    public void Turn(float direction)
    {
        Quaternion deltaRotation = Quaternion.Euler(angleSpeed * direction * Time.fixedDeltaTime);
        mRB.MoveRotation(mRB.rotation * deltaRotation);
    }

    //fire a shot and start weapon cooldown
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

    //returns true if ship has the flag, false otherwise
    public bool hasFlag()
    {
        bool result = false;
        if (flag != null) result = true;
        return result;
    }

    public void SetEnv(ShipEnvController newEnv)
    {
        env = newEnv;
    }

    public void SetWPCD(bool nwpcd)
    {
        wpcd = nwpcd;
    }

    //the ship must wait 1s between shots.
    IEnumerator WPCD()
    {
        wpcd = false;
        yield return new WaitForSeconds(1);
        wpcd = true;
    }

}