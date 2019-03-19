using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour("BoltPhysics")]
public class BoltPhysicsCallbacks : Bolt.GlobalEventListener {

    public static int numPlayers = 0;
    public static List<BoltConnection> connectionMeta = new List<BoltConnection>();
    public static int me; //our player num
    public static bool assigned = false;

    public override void SceneLoadLocalDone(string map) {
        base.SceneLoadLocalDone(map);


        BoltNetwork.Instantiate(BoltPrefabs.Player, new Vector3(0f, 10f, 0f), Quaternion.identity);
        Debug.Log("Me: " + me);
    }

    public override void OnEvent(NetworkedTransformInfo evnt) {
        if(evnt.id != null) {
            evnt.id.gameObject.GetComponent<NetworkedPhysicsSharedControl>().RecEvent(evnt);
        }
    }
}

