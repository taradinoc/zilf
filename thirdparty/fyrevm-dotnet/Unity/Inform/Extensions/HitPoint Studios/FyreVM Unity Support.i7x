Version 1 of FyreVM Unity Support by HitPoint Studios begins here.

Include FyreVM Support by David Cornelson.

Every Turn (this is the FyreVM Unity Demo Support rule):
	printLocationOutput;
	continue the action.

When play ends:
	printEndGameOutput;
	continue the action.

The nTempNum is a number variable.
The nTempNum is 0.

The nRoom is a room that varies.

To printEndGameOutput:
[--output for the game ending]
	select the EndGame channel;
	say "GameEnded";
	select the main channel.

To printLocationOutput:
[--output the location of the player]
	select the Location channel;
	Let zxcRoom be location of player;
	say "[zxcRoom]";
	select the main channel.

FyreVM Unity Support ends here.