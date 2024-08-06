# LethalMoonUnlocks

Permanently unlock moons or progressively unlock discounts.  
Enhance variety of your custom moon collection as you keep discovering new moons for travel.
Progress from free moons to more expensive and difficult ones.

- Featuring custom moon groups, displaying tags in terminal, random moon sales and more  

- Well suited for modpacks with a lot of moons  

- Works with all custom moons (even those with special characters or whitespaces)


Credits for the original idea to [**Permanent Moons by BULLETBOT**](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/).
<details>
	<summary><strong>Why make LethalMoonUnlocks when Permanent Moons exists?</strong></summary>

Unfortunately Permanent Moons is not maintained anymore and has some issues with certain custom moons (`Atlas Abyss`, `Outpost-31`, ..) due to their names.  
This was really bugging me in my personal modpack where all the moon prices were balanced around permanently unlocking them.  
So I took this as a learning opportunity. 
</details>

<details>
	<summary><strong>Can I use my Permanent Moons savegame?</strong></summary>

Yes, LethalMoonUnlocks automatically imports your Permanent Moons data from existing save files when you load into them.  
Any data for moons that are not installed (or enabled) at the time or can't be matched for other reasons will be discarded.  

You can uninstall Permanent Moons directly after installing this. There's no need to start the game with both installed once or anything like that.

</details>

All clients need to have this mod installed!

Report issues on GitHub: [**LethalMoonUnlocks**](https://github.com/YoBii/LethalMoonUnlocks)

<details>
	<summary><strong>Dependencies</strong></summary>

- LethalNetwork API - Used for sending data between host and clients.
- LLL - used for changing moon prices and visibility (discoverability), as well as adding tags to the moon catalogue.

If you don't use LLL, you don't have custom moons, so you can use Permanent Moons (link above) just fine.
</details>


## Unlock Mode (Default)
Unlock mode is the default mode akin to the original Permanent Moons mod.
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

In Unlock Mode you unlock paid moons by routing to them once. From that point on the moon will be free to route to. 

There are config options available to customize your experience in Unlock Mode.  
Read this section for more information.

### (Optional) Unlocks Expire
You can set unlocks to expire. This means you can only make use of an unlock a limited number of times.  
Once you unlocked a moon you can route to it for free. Every time you do your unlock comes closer to expiring.  
After the set limit of uses has been reached, the unlock expires - resetting the moon to its original price.

### Discovery Mode related
These options provide customization for when you're using Unlock Mode together with Discovery Mode.  
For more information read the section about [**Discovery Mode**](#discovery-mode).
<details>
  <summary>Show discovery related options</summary>

#### (Optional) Unlocked Moons are Permanently Discovered
When a moon is unlocked it will be permanently discovered. That means it's added to the moons available in the Terminal's moon catalogue - on top of your base selection.  
Essentially this will make it so any moon you've unlocked will always be available for travel in Discovery Mode.

#### (Optional) Reset Permanent Discoveries on Expiry
When the unlock for a permanently discovered moon expires it will also reset that moon's permanent discovery status making it disappear from the moon catalogue.  
> This is the only way permanent discoveries can vanish during a run making it useful to increase variety in the late game.
</details>

### (Optional) Quota Unlocks
Quota Unlocks are a mechanic that rewards your for meeting the quota by granting random unlocks for free.  
<details>
  <summary>Quota Unlock Configuration</summary>

- set the chance to trigger Quota Unlocks
- set a min and max for the amount of moons to unlock when triggered
- limit Quota Unlocks to moons up to a certain price
- limit the total number of times Quota Unlocks can be triggered
</details>

</details>

## Discount Mode
In Discount Mode instead of directly unlocking moons you will unlock a new discount rate each time you buy a moon eventually making it free.  
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

Discount Mode provides a middle-ground for when you think directly unlocking moons is a bit too easy or just doesn't fit your balancing.  

There are config options available to customize your experience in Discount Mode.  
Read this section for more information.

### Discount rates
Moons will progress through a configurable list of discount rates. With each discount received the next rate is unlocked.  
You can set up as many discount rates as you like and they don't necessarily have to go from low to high.

For example you could set up discounts so that moons are 50% off after your first purchase, 75% after the second and free after the third.
That would be achieved by setting the discount rates config option to `50,75,100`.
> Typically the final rate would be 100 - making the moon free like a normal 'Unlock'.  

### (Optional) Discounts Expire
You can set discounts to expire. This means you can only make use of a fully discounted moon a limited number of times.  
Once a moon is fully discounted you can route to it for free. Every time you do your discount comes closer to expiring.  
After the set limit of uses has been reached, the discount expires - resetting the moon to its original price.
> This option requires you to set up discount rates so that the final rate is free (100)

### Discovery Mode related
These options provide customization for when you're using Discount Mode together with Discovery Mode.  
For more information read the section about [**Discovery Mode**](#discovery-mode).

<details>
  <summary>Show discovery related options</summary>

#### (Optional) Discounted Moons are Permanently Discovered
When a moon is discounted it will be permanently discovered. That means it's added to the moons available in the Terminal's moon catalogue - on top of your base selection.  
Essentially this will make it so any moon you've unlocked a discount for will always be available for travel in Discovery Mode.

#### (Optional) Reset Permanent Discoveries on Expiry
When the discount for a permanently discovered moon expires it will also reset that moon's permanent discovery status making it disappear from the moon catalogue.  
> This is the only way permanent discoveries can vanish during a run making it useful to increase variety in the late game.
</details>

### (Optional) Quota Discounts
Quota Discounts are a mechanic that rewards your for meeting the quota by granting random discounts for free.  
<details>
  <summary>Quota Discounts Configuration</summary>

- set the chance to trigger Quota Discounts
- set a min and max for the amount of moons to receive a discount (rate) when triggered
- limit the total number of times Quota Unlocks can be triggered
</details>

### (Optional) Quota Full Discounts
Quota Full Discounts are a mechanic that rewards your for meeting the quota by unlocking the final discount rate for random moons for free.  
<details>
	<summary>Quota Discounts Configuration</summary>

- set the chance to trigger Quota Full Discounts
- set a min and max for the amount of moons receive the final discount rate when triggered
- limit Quota Full Discounts to moons up to a certain price
- limit the total number of times Quota Full Discounts can be triggered
</details>

</details>


## Discovery Mode
In Discovery Mode you start with a limited selection of moons for travel. You will discover new moons as you play.
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

The configuration options for this mode are plentiful.  
Read this section for more information on Discovery mode.

### General
Discovery Mode is all about exploration and progression and synergizes very well with having a lot of custom moons.  

There are various ways to discover new moons - adding a completely new aspect to the game: *Moon Progression*
New moons can be discovered regularly, as a reward, randomly - or any combination of those.

There's even support for moon group matching using custom defined groups.
This means you can set up your own moon groups (galaxies, solar systems, tiers, etc.) and when you travel to a moon you might discover more moons of the same group.

But first let's start with the basics or rather - the base selections.  

### Base Selections (Moon rotation)
When you start a new game in Discovery Mode your selection of moons available for travel in the Terminal's moon catalogue are limited.  
How many moons are available is determined by your moon base counts. There are three:
- Free moons (every moon that has a original route price of 0 credits)
- Dynamic free moons (every moon that currently has a route price of 0 credits)
- Paid moons

You can configure the base count for each of those and you can also have them increase every time the current selection (or rotation) is shuffled.

### Shuffling
By default every time a new quota begins the moon rotation will be shuffled i.e. the current selection is discarded and new moons will be randomly selected.  
You have options to change this to shuffle every day instead or never shuffle at all.

### Discovered moons (Discoveries)
Every moon available for travel in the Terminal is considered a discovered moon - including the base selections.  
However as mentioned before there are more ways to discover moons that that. Those will be added on top of your base selection.
They will *also vanish* when the rotation is shuffled. That is unless.. you make them permanent.

### Permanently Discovered Moons (Permanent Discoveries)
Permanently discovered moons are just that - permanent. Meaning they are added on top of your base selection but *do not* vanish on shuffle.  
Moons can be permanently discovered in various ways and this ultimately depends on your configuration. 

It could be
- making discoveries permanent by unlocking them (or unlocking a discount)
- making discoveries permanent by landing a set number of times
- making discoveries granted by a certain mechanic permanent

Of course in the same way you can also permanently discover moons from the base selections.

Permanent discoveries allow you to pick moons from your current rotation and keep them around for the rest of the run.
> Combined with the discovery mechanics below this allows for granting more and more additional discoveries as the quota progresses but in order to keep any of them, you have to purchase them.
> This can incentivize buying moons even several days into a quota. Especially when combined with [Moon Sales](#moon-sales)

The only way permanent discoveries can vanish are the options associated with unlocks and discounts expiring.
> This can enhance variety in the late game

### (Optional) Quota Discoveries
Quota Discoveries are a mechanic that rewards your for meeting the quota by granting one or more moon discoveries.  
<details>
	<summary>Quota Discoveries Configuration</summary>

- set the chance to trigger Quota Discoveries
- set a min and max for the amount of moons discovered
- make moons discovered this way permanent discoveries
</details>

### (Optional) Travel Discoveries
Travel Discoveries are a mechanic that randomly grants moon discoveries as you route and travel to paid moons.
<details>
	<summary>Travel Discoveries Configuration</summary>

- set the chance to trigger a Travel Discovery
- set a min and max for the amount of moons discovered
- make moons discovered this way permanent discoveries
- prefer discovering moons that belong to the same group as the one you've routed to (see [Moon Groups](#moon-groups))
</details>

### (Optional) New Day Discoveries
New Day Discoveries are a mechanic that randomly grants moons when a new day begins.
<details>
	<summary>New Day Discoveries Configuration</summary>

- set the chance to trigger a New Day Discovery
- set a min and max for the amount of moons discovered
- make moons discovered this way permanent discoveries
- prefer discovering moons that belong to the same group as the one you're currently located at (see [Moon Groups](#moon-groups))
</details>

</details>

## Terminal Tags
LethalMoonUnlocks supports displaying various conditions directly in the Terminal's moon catalogue.
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

Terminal Tags are disabled by default.  
If you're using anything but unlocks it's recommended to turn them on.

Terminal Tags present you all information relevant to LethalMoonUnlocks directly in the moon catalogue.  
Which tags are displayed depends on the current state of each moon and your configuration.

You can enable or disable every tag individually.

Here's an example where I tried to fit all tags on a single screenshot. Explanation for each tag below.

![Example of LMU Terminal Tags](https://i.ibb.co/nrtGcY9/image.png)

<details>
  <summary><strong style="">Looks too crowded? Check this out</strong></summary>

  There's a config option in the advanced section allowing you to control the maximum tag line length.  
  This can give the moon catalogue a more organized look at the cost of more scrolling.

![More organized example of Terminal Tags](https://i.ibb.co/88ZGLXj/image.png)

</details>

| Tag | Information |
| --- | --- |
| **[IN ORBIT]** | Indicates the moon you're currently orbiting.|
| **[UNEXPLORED]** | Indicates which moons you haven't landed on |
| **[EXPLORED: X]** | Indicates which moons you have landed on and keeps track of your total landings. |
| **[UNLOCK]** | Indicates the moon is unlocked. |
| **[UNLOCK EXPIRES:X]** | Indicates how many times you can route to the moon for free before the unlock expires. |
| **[DISCOUNT-XX%]** | Discount Mode: indicates the moon is on discount and shows the currently unlocked rate. |
| **[DISCOUNT EXPIRES:X]** | Indicates how many times you can route to the moon for free until the discount expires. |
| **[NEW]** | Indicates the moon has been discovered for the first time this run. Resets every day. |
| **[PINNED]** | Indicates the moon has been permanently discovered - effectively pinning it in the moon catalogue. |
| **[SALE-XX%]** | Indicates the moon being on sale and shows the sales rate. |
| **[MoonGroups]** | For example [Zeekers Galaxy] or [VANILLA/FOREST]. Indicates the name(s) of the custom group(s) or LLL Tag(s) a moon belongs to. Only if moon group custom or tag matching is enabled. |

Tags are added to the moon catalogue using an event provided by LLL and will also show with TerminalFormatter!

</details>

## Moon Sales
Moons have a random chance to go on sale.
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

Each moon has a random chance to go on sale every time the sales are shuffled.  
Moon Sales are multiplicative with other price reductions like discounts from [**Discount Mode**](#discount-mode).

You can configure the chance as well as minimum and maximum sale rate.  
They can either be shuffled every quota or every day.
> Shuffling Moon Sales daily can incentivize buying moons even days into a quota.


</details>

## Moon Groups
LethalMoonUnlocks supports matching moons by group. This is relevant for certain discovery mechanics.

<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

In Discovery mode all new discoveries are randomly selected. With moon group matching LethalMoonUnlocks will prefer selecting from group matches instead of all moons.

This is always in reference to a *matching moon*. For [**Travel Discoveries**](#optional-travel-discoveries) that is the moon you've routed to and for [**New Day Discoveries**](#optional-new-day-discoveries) it's the moon you're currently at.

Moon group matching can be disabled individually for each of these mechanics.

There are multiple group matching methods available.

### Price, PriceRange, PriceRangeUpper
All of these methods use the moon's original prices to match them into groups.

**Price** matches all moons that share the exact same price.

**PriceRange** matches all moons within a configurable +- price range.

**PriceRangeUpper** same as PriceRange but only considers equally or more expensive moons.

### Tag
Selects a random LLL content tag and matches all other moons sharing that tag.

### Custom
Custom allows you to define fully custom groups. A group is defined by name and a list of members (moons).  

A moon will always match with all members of the same group.  
If a moon is member of multiple groups, a random group will be selected for matching.  
When all members of every group are already discovered a random moon will be chosen instead.

The group name will be displayed in various locations in-game e.g. *Autopilot discovered new moons during travel to Zeekers Galaxy*.
</details>

## Other stuff

### Cheap Moon Bias
<details>
  <summary><strong style="">Learn more</strong></summary>
<br>

In Discovery Mode moons will be randomly selected (as new discoveries) e.g. when a the paid selection is shuffled. 
**Cheap Moon Bias** will increase the odds of cheaper moons being selected.
>Makes it less likely - especially in the early game - to only discover moons you can't afford yet.

The bias value is configurable. The bias in its entirety can be enabled or disabled for each applicable discovery mechanic.
</details>

### Compatibility
<details>
  <summary><strong style="">Learn more</strong></summary>

#### LethalConstellations
*Planned!*

#### TerminalFormatter
Tags are shown in TerminalFormatter moons node. Thanks @mrov!

#### LethalQuantities
Advanced config option to prefer LQ risk levels in moon catalogue.

#### Malfunctions
Advanced config option to enable interpreting Malfunctions' Navigation malfunction as routing to a moon.  
If it's a paid moon LethalMoonUnlocks will see it as buying the moon - even though you didn't pay.

#### All mods displaying alert messages
LethalMoonUnlocks uses a queue for sending alert messages.  
Alerts from other mods and the vanilla game are added to the same queue to avoid overlapping and missing messages.
</details>

## Example configurations
In this section I provide some ideas of what's possible using all the different config options.

Only showing settings that are not default.
<details>
  <summary><strong style="">Simple Discounts</strong></summary>

	Display tags in terminal = true
	Discount rates = 50,75,90,100
	Discounts expire = 3
	Enable Quota Discounts = true
	Quota Discount trigger chance = 33
	Maximum discounted moon count = 2
	Enable Quota Full Discounts = true
	Quota Full Discount trigger chance = 10
	Moon Sales = true
	Shuffle sales daily = true

Simple setup with slightly modified discounts, rewards on Quota completion and Moon Sales.

</details>

<details>
  <summary><strong style="">Progression focused</strong></summary>

	Display tags in terminal = true
	Unlocked moons are permanently discovered = true
	Enable Discovery Mode = true
	Free moons base count = 99
	Paid moons base count = 3
	Enable Quota Discoveries = true
	Quota Discovery trigger chance = 100
	Maximum quota discovery moon count = 3
	Enable Travel Discoveries = true
	Travel Discovery trigger chance = 50
	Travel Discovery group matching = true
	Enable New Day Discoveries = true
	New Day Discovery trigger chance = 50
	New Day Discovery group matching = true
	Group Matching Method = PriceRange
	Price range = 500
	
All free and unlocked moons are always available for travel.  
3 paid moons available which are shuffled every quota. Additionally discover new moons on completing the quota, new day and travelling.
Buy them before the quota ends and you keep them, don't and they will be lost with shuffle.  
Repeat every quota and grow your catalogue.

</details>

<details>
  <summary><strong style="">Full exploration</strong></summary>

	Display tags in terminal = true
	Enable Discount Mode = true
	Discounts expire = 3
	Enable Discovery Mode = true
	Never shuffle = true
	Free moons base count = 1
	Dynamic free moons base count = 0
	Paid moons base count = 2
	Enable Travel Discoveries = true
	Travel Discovery trigger chance = 100
	Travel Discovery group matching = true
	Enable New Day Discoveries = true
	New Day Discovery trigger chance = 50
	Maximum new day discovery moon count = 2
	New Day Discovery group matching = true
	Group Matching Method = Custom
	Custom moon groups = ...

> Assumes fully set up custom moon groups

Start with only 1 free and 2 paid moons. Never shuffle the base selection. 
Instead discover new moons mainly by travel and on new days. Due to moon group matching you'll discover moons group by group.  
Fully discover a group and you'll discover moons from other groups. 
Add Quota rewards to preference. 

Depending on the amount of custom moons you have, a setup like this would probably require modifying quota steepness.

</details>

# Special Thanks

* Huge thanks to @nickham13 for helping me test the mod, suggesting features and streamlining the config file. Thank you!
* [Permanent Moons](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/)
* [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/)
* [LethalNetworkAPI](https://thunderstore.io/c/lethal-company/p/xilophor/LethalNetworkAPI/)