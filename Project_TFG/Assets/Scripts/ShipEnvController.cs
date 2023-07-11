using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class ShipEnvController : MonoBehaviour
{
    [System.Serializable]
    public class ShipInfo
    {
        public ShipControllerAgent ship;
        public string team;
        [HideInInspector]
        public Vector3 iPosition;
        [HideInInspector]
        public Quaternion iRotation;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Transform tf;
    }

    private SimpleMultiAgentGroup blueTeam;
    private SimpleMultiAgentGroup redTeam;

    public int shipRespawnTime = 10;
    public List<ShipInfo> ShipsList = new List<ShipInfo>();

    private void Awake()
    {
        blueTeam = new SimpleMultiAgentGroup();
        redTeam = new SimpleMultiAgentGroup();

        foreach (var item in ShipsList)
        {
            item.tf = item.ship.gameObject.transform;
            item.iPosition = item.tf.position;
            item.iRotation = item.tf.rotation;
            item.Rb = item.ship.GetComponent<Rigidbody>();

            item.ship.SetEnv(this);

            if (item.team == "Blue")
            {
                blueTeam.RegisterAgent(item.ship);
                item.ship.SetTeams(blueTeam, redTeam);
            }
            else if (item.team == "Red")
            {
                redTeam.RegisterAgent(item.ship);
                item.ship.SetTeams(redTeam, blueTeam);
            }
        }
    }

    public void Respawn(ShipControllerAgent ship)
    {
        foreach (var item in ShipsList)
        {
            if (item.ship == ship)
            {
                StartCoroutine(Respawn(item));
            }
        }

    }

    IEnumerator Respawn(ShipInfo ship)
    {
        ship.tf.gameObject.SetActive(false);

        yield return new WaitForSeconds(shipRespawnTime);

        ResetShip(ship);
    }

    public void ResetShip(ShipInfo ship)
    {
        ship.tf.SetPositionAndRotation(ship.iPosition, ship.iRotation);
        ship.Rb.velocity = Vector3.zero;
        ship.Rb.angularVelocity = Vector3.zero;

        ship.tf.gameObject.SetActive(true);

        if (ship.team == "Blue")
        {
            blueTeam.RegisterAgent(ship.ship);
            ship.ship.SetTeams(blueTeam, redTeam);
        }
        else if (ship.team == "Red")
        {
            redTeam.RegisterAgent(ship.ship);
            ship.ship.SetTeams(redTeam, blueTeam);
        }
    }
}