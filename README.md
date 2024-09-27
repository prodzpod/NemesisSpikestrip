# Nemesis Spikestrip

Reworks and rebalances to [Spikestrip 2.0](https://thunderstore.io/package/SpikestripModding/Spikestrip2_0/) elites that makes them closer in power to vanilla elites.

Also try: [Nemesis Rising Tides](https://thunderstore.io/package/prodzpod/Nemesis_Rising_Tides/)

## T1: Plated Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/icon1.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/1.png)
- Reworked: Upon damage, increases its stack. only the 6th attack deals damage and resets the stack.
- On death, provides small amounts of armor to enemies around itself. (buff to compensate)
- Stack is visible in health, hopefully makes the effect less frustrating to deal with.

You might want to **disable / "zero out" ZetAspects settings** if this setting is on, as plated elites become absolutely unkillable early game with this combined.

## T1: Warped Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/icon2.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/2.png)
- Debuff duration configurable
- Ability to "break out" faster by not moving (up to 6x, configurable)  

Note: Might still be too close to [Aquamarine](https://thunderstore.io/package/prodzpod/Nemesis_Rising_Tides/) elites? let me know if you have a better rework idea (3). I think theyre distinct enough though considering veiled / celestine. different tiers too

## T1: Veiled Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/icon3.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/3.png)
- Increased distortion VFX. this hopefully allows keen eyes users to spot invisible enemy.
- Enemy is visible for a short period upon taking damage. this is to encourage focusing down on a revealed enemy.
- [Assassins](https://thunderstore.io/package/prodzpod/RecoveredAndReformed/) cannot be veiled as they can inherently go invisible.

## T2: Aragonite Elites <img src="https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/icon4.png" width="24">
![Elite](https://raw.githubusercontent.com/prodzpod/NemesisSpikestrip/master/4.png)
- Attack does not trigger off attacking drones / followers.
- Renamed affix equipment name.

## Other changes
- compatibility with affix-in-logbook mods ([WolfoQoL](https://thunderstore.io/package/Wolfo/WolfoQualityOfLife/), [ZetAspects](https://thunderstore.io/package/William758/ZetAspects/)).
- Miscellaneous name changes.
- some configs for Sigma Construct and Lively Pot. by default, buffs sigma construct's beams and makes lively pot's creep larger but last much shorter.

## Changelog
- 1.1.1: i dont know whats going on exactly but i removed all null checks. wont fix nre nonsense unless i get reproducible steps if it still persists
- 1.1.0: enemy config
- 1.0.6: fixed veiled captain shock and readme
- 1.0.5: works for sots
- 1.0.4: fixed crash due to aragonite change of main mod
- 1.0.3: Automatically disables each module if corresponding config is disabled on the main mod.
- 1.0.2: s <img src="https://cdn.discordapp.com/attachments/781570609729372253/1112438647036334100/SE.jpg" width="24">
- 1.0.1: Assassins actually cannot be veiled, bugfixes, Read Me (RM reference) update