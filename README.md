# LethalMoonUnlocks

Permanently unlock moons and more. No issues with special characters in custom moon names.

Credits for the original idea to [**Permanent Moons by BULLETBOT**](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/).

Unfortunately Permanent Moons is not maintained anymore and has some issues where it's not working with certain custom moons (`Atlas Abyss`, `Outpost-31`, ..) due to the non alphanumeric charaters in their names.

That is why I decided to make this mod. You can seamlessly transition your savefiles to LethalMoonUnlocks. Read more below.

---
This mod requires:
- LethalNetwork API - Used for networking the necessary data between host and clients.
- LLL - Allows us to easily access and change moon prices.

If you don't use LLL, you don't have custom moons, so you can use Permanent Moons (link above) just fine.

## Seamless migration from Permanent Moons
LethalMoonUnlocks automatically imports your Permanent Moons data from existing savefiles and migrates it to the LethalMoonUnlocks format when you load into them.

Any data for moons that are not installed (or enabled) at the time or can't be matched for other reasons will be discarded.

Once you've installed LethalMoonUnlocks you can disable/uninstall Permanent Moons. No need to start the game with both installed once or anything like that.

## Additional features and config options

### Reset when fired

When enabled (default) will reset all your moon unlocks or discounts when your crew is fired. Disable to keep all your unlocks/discounts through all quotas on the same savefile.

### Unlock on new quota
When enabled a random moon will be unlocked every time you complete a quota.
You can configure this in two ways. 

Disabled by default (Config value: False)

#### Max unlock price
This will limit the random unlock to moons below this price. Of course free moons are always excluded.

Disabled by default (Config value: 0).

#### Limit number of unlocks
Simply limit how often this unlock can occur in a single run.
For example if you set this to '3', you will get three random unlocks: Upon First day of second, third and fourth quota

Disabled by default (Config value: 0)

### Discount mode
Instead of permanently unlocking moons (price 0) after buying them once, unlock a new discount rate each time you buy a moon.

Disabled by default (Config value: False)

#### Discount rates
Set your preferred discount rates. Values are given as % off of the original price and seperated by comma.
Only affects prices when discount mode is enabled.

## Thanks

* [Permanent Moons](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/)
* [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/)
* [LethalNetworkAPI](https://thunderstore.io/c/lethal-company/p/xilophor/LethalNetworkAPI/)