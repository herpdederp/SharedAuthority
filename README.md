# SharedAuthority

Using a "faked" shared authority we can give rigidbody props immediate collision response even if we are not the owner.
We do this by using an extra state value to determine who is replicating the entity (simulator).  
When a player collides with a physics object, it makes itself the simulator.
The simulator sends pos/rot updates (via events) to the host, who then sets those state values and replicates it back to everyone else.

As the "simulator" is the current authority, we don't apply the replicated state values to them. 
When the simulator stops colliding with a physics object it continues to send updates

Things break down a bit when two players fight over authority
