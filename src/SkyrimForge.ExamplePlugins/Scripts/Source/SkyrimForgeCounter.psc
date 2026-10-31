Scriptname SkyrimForgeCounter extends ObjectReference

; Counts each activation of the object this script is bound to.
int Property Counter Auto

Event OnActivate(ObjectReference akActionRef)
	Counter += 1
EndEvent

; Describes the player's gold through GetPlayerGold, a native function SkyrimForgeExamples implements.
string Function DescribePlayerGold() global
	Return "The player has " + SkyrimForgeExamples.GetPlayerGold() + " gold."
EndFunction
