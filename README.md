# wRobot Fightclass Framework for Vanilla
Please check the sample code on how to use it.
You can create new implementation of the RotationAction interface, but given implementations should suffice.

It is generally good practise to have several combat iterations to be done under different conditions, for example:
- different stances (stealth, warrior stance, druid forms)
- states, such as running from enemy, kiting, single target, aoe, etc
- current level, if necessary
- enemy level

Each RotationStep consists of 3 fundamental properties:
- an action to be executed
- a priority (list should be sorted by this)
- a function to be executed, which SHOULD return the correct target for this action (default is a friendly target nearby)
