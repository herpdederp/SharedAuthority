using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkedPhysicsSharedControl:Bolt.EntityEventListener<INetworkedPhysicsSharedControl> {

    //requires a boltentity with a state replicating:
    //position
    //rotation
    //simulator (int)


    public float lerpTime = 0f;
    public AnimationCurve lerpSpeed;

    public float simulationTime = 0f;

    public Vector3 targetPosition;
    public Quaternion targetRotation;

    public int controllingPlayer = 0;

    //this only ever gets changed on the server, because the server only ever recieves the events.
    //everyone can send them though, and decide whether or not to USE the replicated state info.
    public int localCollisions = 0;

    public float sendRate = 1f / 25f;
    private float sendTimer = 0f;
    private float lastSendTime = 0f;

    public Vector3 lastSentPosition;

    public float localStateLerp = 0.2f;
    //public Vector3 lastSentRotation;
    public float attachForceLerpTime = 1f;

    public bool cull = false;

    public int me = 0;

    public override void Attached() {
        lastSentPosition = this.transform.position;
        //lastSentRotation = this.transform.rotation;

        me = BoltPhysicsCallbacks.me;

        //don't send i
        if(!entity.isOwner) {
            this.transform.position = state.position;
            this.transform.rotation = state.rotation;
        } else {
            state.position = this.transform.position;
            state.rotation = this.transform.rotation;
        }
    }

    public void PositionChanged() {
        //if(replicate) {
        //    targetPosition = state.position;
        //}

    }

    public void RotationChanged() {
        //if(replicate) {
        //    targetRotation = state.rotation;
        //}
    }

    public void FixedUpdate() {
        if(!entity.isAttached) return;
    }

    void OnDrawGizmos() {
        if(!entity.isAttached) return;
        Gizmos.color = Color.red;
        Gizmos.DrawCube(this.transform.position, Vector3.one);
        Gizmos.color = Color.green;
        Gizmos.DrawCube(state.position, Vector3.one);
    }

    public void SendTransformEvent(int onlineIndex = 0, bool simulatingNonInteraction = false) {
        //if we are interacting with something we will send updates as fast as possible
        //otherwise we just send updates every so often to make sure things are synced
        if(simulatingNonInteraction) {
            if(Time.realtimeSinceStartup - lastSendTime < sendRate) return;
            Vector3 sending = new Vector3((float)System.Math.Round(this.transform.position.x, 2), (float)System.Math.Round(this.transform.position.y, 2), (float)System.Math.Round(this.transform.position.z, 2));
            //if(sending == lastSentPosition) return;
            lastSentPosition = sending;
        }

        NetworkedTransformInfo e = NetworkedTransformInfo.Create(Bolt.GlobalTargets.OnlyServer);
        e.position = this.transform.position;
        e.rotation = this.transform.rotation;
        e.accumulator = onlineIndex;
        e.id = this.entity;
        e.Send();
        //DLog.Log("Sending NetworkedTransformInfo: " + e.position.ToString() + " : " + e.id.ToString());
        lastSendTime = Time.realtimeSinceStartup;
        //can we check to see if we NEEd to send this? e.g. values haven't changed?
    }

    public Color GetColor() {
        switch(state.simulator) {
            case 0: return Color.red;
            case 1: return Color.blue;
            case 2: return Color.yellow;
            case 3: return Color.green;
            default: return new Color(Random.value, Random.value, Random.value);
        }
    }

    public void RecEvent(NetworkedTransformInfo evnt) {
        Debug.Log("Rec NetworkedTransformInfo: " + evnt.position.ToString() + " " + evnt.accumulator);

        state.simulator = evnt.accumulator;
        state.position = evnt.position;
        state.rotation = evnt.rotation;

        this.GetComponent<Renderer>().material.color = GetColor();

        //if we've recieved this event late...what then?  
        //If player 0 asks for control, and sets control then recieves player 1's old "HEY IM STILL DOING THIS"
        //hand off isn't smooth.  How do we decide if we should STOP sending events

        //need a "local" control, if we're colliding with an object ignore all state info we get
    }
    //we can do this by adding a "owner" state variable.
    //when we touch it send an event saying HEY WE OWN THIS
    //then check the state every update or whatever to see if we own it,
    //if we do, send updates from us.

    //Everyone else (who doesn't own it)
    //should simulate physics, but lerp between their simulations and the "correct" state
    //this will allow for prediction, but gradual corrections 

    void LateUpdate() {
        me = BoltPhysicsCallbacks.me;
        if(!entity.isAttached) return;


        //we do need to check our culling every while though to see if we need to fix this

        if(cull) return; //if no one is somewhat close, just ignore everything with these...
        //culling is set by a custom cull component that does some *magic*


        float dt = Time.deltaTime;
        if(attachForceLerpTime >= 0f) {
            attachForceLerpTime -= dt;
            this.GetComponent<Rigidbody>().position = state.position;
            this.GetComponent<Rigidbody>().rotation = Quaternion.Slerp(state.rotation, Quaternion.identity, 0f); //slerping this against nothing to normalize it?
            this.GetComponent<Rigidbody>().isKinematic = true;
            return;
        } else {
            this.GetComponent<Rigidbody>().isKinematic = false;
        }

        
        if(state.simulator == BoltPhysicsCallbacks.me) {
            //we are simulating this physics locally, so lets send state updates to the server
            //and NOT lerp the physics (as we are the authority, so where it is is where it is)
            SendTransformEvent(controllingPlayer, true);
        } else {
            //we are getting physics update from someone else, replicated through the server
            //lerp towards it if there are no local things going on.

            if(localCollisions == 0) {
                //if we're close ish do this, otherwise just teleport
                if(Vector3.Distance(this.transform.position, state.position) > 5f) {
                    this.GetComponent<Rigidbody>().position = state.position;
                    this.GetComponent<Rigidbody>().rotation = state.rotation;
                } else {
                    this.GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(this.transform.position, state.position, localStateLerp));
                    this.GetComponent<Rigidbody>().MoveRotation(Quaternion.Slerp(this.transform.rotation, state.rotation, localStateLerp));
                }
            } else {
                //do nothing, because we're simulating locally and ignoring the state updates because
                //the state says someone else is controlling it (but it just hasn't updated with OUR ownership)
            }
            //
        }

        
        //DLog.Log("Local collisions: " + localCollisions);
        localCollisions = 0;
    }

    //these need to check for the object root (or the collider needs to be on the root..)
    public void OnCollisionStay(Collision c) {
        //is it the player?
        //are we collding with a local player?
        //we need to check this in update to see if we're colliding with more than one player..
        //if we are this will be overwritten
        //DLog.Log("NTLB::Stay");

        
        if(cull) return;
  
        BoltEntity e = c.gameObject.GetComponent<BoltEntity>();
        if(e != null) {
            if(e.isOwner) {
                //DLog.Log("NTLB::Stay2");
                localCollisions = 1;
                SendTransformEvent(BoltPhysicsCallbacks.me);
                controllingPlayer = BoltPhysicsCallbacks.me;
            }
        }
    }

    public void OnCollisionEnter(Collision c) {
        if(cull) return;
        BoltEntity e = c.gameObject.GetComponent<BoltEntity>();
        if(e != null) {
            if(e.isOwner) {
                //DLog.Log("NTLB::Stay2");
                localCollisions++;
                SendTransformEvent(BoltPhysicsCallbacks.me);
                controllingPlayer = BoltPhysicsCallbacks.me;
            }
        }
    }

    public void OnCollisionExit(Collision c) {
        if(cull) return;
        BoltEntity e = c.gameObject.GetComponent<BoltEntity>();
        if(e != null) {
            if(e.isOwner) {
                //DLog.Log("NTLB::Stay2");
                lerpTime = 0f;
            }
        }

    }
}
