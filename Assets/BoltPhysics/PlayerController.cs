using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Bolt.EntityBehaviour<IPlayerState> {

    public float moveSpeed = 10f;

    public override void Attached() {

        if(!entity.isOwner) {
            state.AddCallback("position", PositionChanged);
        } else {
            state.playerIndex = BoltPhysicsCallbacks.me;
        }
        state.AddCallback("playerIndex", IndexChanged);
        IndexChanged();

    }

    public void IndexChanged() {
        this.GetComponent<Renderer>().material.color = GetColor();
    }

    public Color GetColor() {
        switch(state.playerIndex) {
            case 0:return Color.red;
            case 1: return Color.blue;
            case 2: return Color.yellow;
            case 3: return Color.green;
            default: return new Color(Random.value, Random.value, Random.value);
        }
    }

    public override void SimulateOwner() {
        base.SimulateOwner();

        if(Input.GetKey(KeyCode.A)) {
            this.transform.position += this.transform.right* moveSpeed * Time.deltaTime;
        }

        if(Input.GetKey(KeyCode.D)) {
            this.transform.position -= this.transform.right * moveSpeed * Time.deltaTime;
        }

        if(Input.GetKey(KeyCode.W)) {
            this.transform.position -= this.transform.forward * moveSpeed * Time.deltaTime;
        }

        if(Input.GetKey(KeyCode.S)) {
            this.transform.position += this.transform.forward * moveSpeed * Time.deltaTime;
        }

        state.position = this.transform.position;
    }

    public void PositionChanged() {
        this.transform.position = state.position;
    }
}
