using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class ShipAgent : Agent
{
    private ShipController sc;
    private Rigidbody rb;
    private SimpleMultiAgentGroup allyTeam, enemyTeam;

    public Transform enemyFlag;
    public Transform allyBase;

    public override void Initialize()
    {
        sc = GetComponent<ShipController>();
        rb = GetComponent<Rigidbody>();
    }   

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //get 1 to fire a shot
        if (actionBuffers.DiscreteActions[0] == 1 && sc.GetWPCD())
        {
            sc.Firing();
            AddReward(0.01f);
        }

        //get 1 to move backwards and 2 to move forward
        if (actionBuffers.DiscreteActions[1] == 1)
        {
            sc.MFB(-1);
        } else if (actionBuffers.DiscreteActions[1] == 2)
        {
            sc.MFB(1);
        }

        //get 1 to move left and 2 to move right
        if (actionBuffers.DiscreteActions[2] == 1)
        {
            sc.MLR(-1);
        }
        else if (actionBuffers.DiscreteActions[2] == 2)
        {
            sc.MLR(1);
        }

        //get 1 to turn left and 2 to turn right
        if (actionBuffers.DiscreteActions[3] == 1)
        {
            sc.Turn(-1);
        }
        else if (actionBuffers.DiscreteActions[3] == 2)
        {
            sc.Turn(1);
        }

        //rewards upon action
        if (!sc.hasFlag())
        {
            AddReward(- 0.00001f * Vector3.Distance(enemyFlag.transform.position, transform.position));
        }
        else
        {
            AddReward(-0.00001f * Vector3.Distance(allyBase.transform.position, transform.position));
            allyTeam.AddGroupReward(0.00001f * (100 - Vector3.Distance(allyBase.transform.position, transform.position)));
            enemyTeam.AddGroupReward(-0.00001f *  Vector3.Distance(allyBase.transform.position, transform.position));
        }

        sc.GetTeam().AddGroupReward(-0.00001f);

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //orientation and velocity of the ship
        sensor.AddObservation(transform.forward);

        sensor.AddObservation(rb.velocity);

        //distance and direction to ally base
        sensor.AddObservation(Vector3.Distance(allyBase.transform.position, transform.position));

        sensor.AddObservation((allyBase.transform.position - transform.position).normalized);

        //distance and direction to enemy flag
        sensor.AddObservation(Vector3.Distance(enemyFlag.transform.position, transform.position));

        sensor.AddObservation((enemyFlag.transform.position - transform.position).normalized);

        //disponibilidad del arma
        sensor.AddObservation(sc.GetWPCD());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int firing = 0;
        int mfb = 0;
        int mlr = 0;
        int turn = 0;

        if (Input.GetKey(KeyCode.Space)) firing = 1;

        if (Input.GetKey(KeyCode.W)) mfb = 2;
        if (Input.GetKey(KeyCode.S)) mfb = 1;

        if (Input.GetKey(KeyCode.D)) mlr = 2;
        if (Input.GetKey(KeyCode.A)) mlr = 1;

        if (Input.GetKey(KeyCode.E)) turn = 2;
        if (Input.GetKey(KeyCode.Q)) turn = 1;

        actionsOut.DiscreteActions.Array[0] = firing;
        actionsOut.DiscreteActions.Array[1] = mfb;
        actionsOut.DiscreteActions.Array[2] = mlr;
        actionsOut.DiscreteActions.Array[3] = turn;
    }

    public void setTeams(SimpleMultiAgentGroup newAllyTeam, SimpleMultiAgentGroup newEnemyTeam)
    {
        allyTeam = newAllyTeam;
        enemyTeam = newEnemyTeam;
    }
}
