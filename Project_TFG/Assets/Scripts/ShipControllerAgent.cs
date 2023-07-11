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
    private SimpleMultiAgentGroup allyTeam;
    private SimpleMultiAgentGroup enemyTeam;
    private ShipEnvController env;
    private float rewardSize = 0.01f;

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
            AddReward(10 * rewardSize);
            allyTeam.AddGroupReward(100 * rewardSize);
            enemyTeam.AddGroupReward(-10 * rewardSize);
        }
        //or if triggering the enemy flag
        else if (other.gameObject == enemyFlag)
        {
            //give reward to ship and ally team, penalize enemy team, grab flag
            flag = other.GetComponent<Flag>();
            flag.Emparent(transform);
            AddReward(1 * rewardSize);
            allyTeam.AddGroupReward(10 * rewardSize);
            enemyTeam.AddGroupReward(-1 * rewardSize);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        //if hit by another ship while carrying the flag...
        if (collision.gameObject.GetComponent<ShipControllerAgent>() && flag != null)
        {
            //...and is an ally ship, reduce ally team points, penalize hitting ship
            if (collision.gameObject.CompareTag(tag))
            {
                allyTeam.AddGroupReward(-5 * rewardSize);
                collision.gameObject.GetComponent<ShipControllerAgent>().AddReward(-1 * rewardSize);
            }
            else //...or is an enemy ship, reduce ally team points, give enemy team points, give bonus to hitting ship
            {
                allyTeam.AddGroupReward(-5 * rewardSize);
                enemyTeam.AddGroupReward(5 * rewardSize);
                collision.gameObject.GetComponent<ShipControllerAgent>().AddReward(2 * rewardSize);
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
                allyTeam.AddGroupReward(-1 * rewardSize);
            }
        }

        //reduce own points and respawn
        AddReward(-1 * rewardSize);
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
        //get 1 to turn left and 2 to turn right
        if (actionBuffers.DiscreteActions[0] == 1)
        {
            Turn(-1);
        }
        else if (actionBuffers.DiscreteActions[0] == 2)
        {
            Turn(1);
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

        //rewards upon action
        //if not carrying flag, existential malus to ally team and ship
        if (!HasFlag())
        {
            AddReward(-0.00001f * rewardSize);
            allyTeam.AddGroupReward(-0.00001f * rewardSize);
        }
        //if carrying flag, existential malus to enemy team, bonus to ally team and ship
        else
        {
            AddReward(0.00001f * rewardSize);
            allyTeam.AddGroupReward(0.0001f * rewardSize);
            enemyTeam.AddGroupReward(-0.0001f * rewardSize);
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

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int turn = 0;
        int mfb = 0;
        int mlr = 0;

        //E to turn right, Q to turn left
        if (Input.GetKey(KeyCode.E)) turn = 2;
        if (Input.GetKey(KeyCode.Q)) turn = 1;

        //W to move forwards, S for backwards
        if (Input.GetKey(KeyCode.W)) mfb = 2;
        if (Input.GetKey(KeyCode.S)) mfb = 1;

        //D to move right, A to move left
        if (Input.GetKey(KeyCode.D)) mlr = 2;
        if (Input.GetKey(KeyCode.A)) mlr = 1;

        actionsOut.DiscreteActions.Array[0] = turn;
        actionsOut.DiscreteActions.Array[1] = mfb;
        actionsOut.DiscreteActions.Array[2] = mlr;
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

    public void SetTeams(SimpleMultiAgentGroup newAllyTeam, SimpleMultiAgentGroup newEnemyTeam)
    {
        allyTeam = newAllyTeam;
        enemyTeam = newEnemyTeam;
    }

    //returns true if ship has the flag, false otherwise
    public bool HasFlag()
    {
        bool result = false;
        if (flag != null) result = true;
        return result;
    }

    public void SetEnv(ShipEnvController newEnv)
    {
        env = newEnv;
    }
}