# LethalMoonUnlocks

Permanently unlock moons and more. No issues with special characters in custom moon names.

Credits for the original idea to [**Permanent Moons by BULLETBOT**](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/).

Unfortunately Permanent Moons is not maintained anymore and has some issues where it's not working with certain custom moons (`Atlas Abyss`, `Outpost-31`, ..) due to the non alphanumeric charaters in their names.

That is why I decided to make this mod. You can seamlessly transition your savefiles to LethalMoonUnlocks. Read more below.

All clients need to have this mod installed!

Report issues on GitHub: [**LethalMoonUnlocks**](https://github.com/YoBii/LethalMoonUnlocks)

---
This mod requires:
- LethalNetwork API - Used for networking the necessary data between host and clients.
- LLL - Allows us to easily access and change moon prices.

If you don't use LLL, you don't have custom moons, so you can use Permanent Moons (link above) just fine.

## Seamless migration from Permanent Moons
LethalMoonUnlocks automatically imports your Permanent Moons data from existing savefiles and migrates it to the LethalMoonUnlocks format when you load into them.

Any data for moons that are not installed (or enabled) at the time or can't be matched for other reasons will be discarded.

Once you've installed LethalMoonUnlocks you can disable/uninstall Permanent Moons. No need to start the game with both installed once or anything like that.

# Additional features and config options
You can configure the mod to do a little more than just setting the price of visited moons to 0.  

Currently there are two additional features:
- a discount mode where each moon goes through a customizable list of discount rates. Each time you buy a moon it unlocks the next discount rate.
- a randomly selected unlock (or discount) each time you make the quota.

Full description below

## Feature: Reset when fired

When enabled will reset all your moon unlocks (or discounts) when your crew is fired.  
Disable to keep all your unlocks (or discounts) through all quotas on the same savefile.
> Enabled by default (Config value: True)

## Feature: Unlock on new quota  
When enabled a random moon will be unlocked every time you complete a quota.  
You can configure this in two ways. 
> Disabled by default (Config value: False)


### Config: Max unlock price  
This will limit the random unlock to moons below this price. Of course free moons are always excluded.
> Disabled by default (Config value: 0)


### Config: Limit number of unlocks  
Simply limit how often this unlock can occur in a single run.  
For example if you set this to '3', you will get three random unlocks: Upon First day of second, third and fourth quota
> Disabled by default (Config value: 0)


## Feature: Discount mode  
Instead of permanently unlocking moons when bought once, unlock a new discount rate each time you buy a moon.
> Disabled by default (Config value: False)


### Config: Discount rates
Set your preferred discount rates. Values are given as '% off' of the original price and seperated by comma.  
Each time you visit a moon the next discount rate from the list will be applied (the list is basically discount after 1st visit, discountafter 2nd visit) ..  
Only affects prices when discount mode is enabled.
> 50%, 75%, 100% by default (Config value: 50,75,100)

# Special Thanks

* [Permanent Moons](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/)
* [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/)
* [LethalNetworkAPI](https://thunderstore.io/c/lethal-company/p/xilophor/LethalNetworkAPI/)