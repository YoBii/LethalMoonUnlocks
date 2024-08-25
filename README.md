# LethalMoonUnlocks

Unlock moons permanently or progressively reduce their costs.  
Introducing new progression mechanics in Discovery Mode where you keep uncovering new moons as you play!

## Overview

LethalMoonUnlocks gives players the power to customize their moon unlocking experience in a number of ways. Here's a quick look at what you can do:

- **Unlock Mode**: Permanently unlock moons after just one purchase.
- **Discount Mode**: Gradually unlock discounts until moons are free. Alternative to Unlock Mode.
- **Discovery Mode**: Start with limited moons and discover new ones as you play.
- **Random Moon Sales**: Enjoy spontaneous sales on moon prices.
- **Terminal Tags**: Display extra information in the moon catalogue (you can toggle this on or off).
- **Custom Moon Groups**: Organize moons into groups.
- **Compatibility**: Integrates with [**LethalConstellations**](https://thunderstore.io/c/lethal-company/p/darmuh/Lethal_Constellations/) and other mods.
- **Extensive Configuration**: Offers a wide variety of setups - your suggestions are welcome!

LethalMoonUnlocks is a great addition to modpacks suffering from paradox of choice because you just can't stop yourself from adding more custom moons. I know who you are. You're me :)

Special thanks to [**Permanent Moons by BULLETBOT**](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/) for the original inspiration.

<details>
	<summary><strong>Why LethalMoonUnlocks?</strong></summary>

Unfortunately, Permanent Moons is no longer maintained and has issues with certain custom moons (e.g., `Atlas Abyss`, `Outpost-31`) due to their names.  
This was particularly frustrating in my personal modpack, where all moon prices were balanced around permanently unlocking them.  
So, I took this as a learning opportunity to create LethalMoonUnlocks.
</details>

<details>
	<summary><strong>Can I use my Permanent Moons savegame?</strong></summary>

Absolutely! LethalMoonUnlocks automatically imports your Permanent Moons data from existing save files when you load them.  
Any data for moons that are not installed (or enabled) at the time or can't be matched for other reasons will be discarded.  

You can uninstall Permanent Moons directly after installing this mod. There's no need to start the game with both installed.
</details>

<details>
	<summary><strong>Dependencies</strong></summary>

- **LethalNetwork API**: Used for sending data between host and clients.
- **LLL**: Used for changing moon prices and visibility (discoverability), as well as adding tags to the moon catalogue.

If you don't use LLL, you likely don't have custom moons, so you can continue using Permanent Moons (link above) without issue.
</details>

**Note**: All clients must have this mod installed!

Report issues on GitHub: [**LethalMoonUnlocks**](https://github.com/YoBii/LethalMoonUnlocks)

## Unlock Mode
> **Default**

Unlock Mode is the only feature enabled by default and works exactly like the original Permanent Moons mod.  

<details>
  <summary><strong>Learn more</strong></summary>
<br>

In Unlock Mode, you unlock paid moons by buying and routing to them once. From that point on, the moon's route will be free. 

There are configuration options available to customize your experience in Unlock Mode.  
Read this section for more information.

### Unlocks Expire
> **Optional:** Set unlocks to expire!

You can put a limit on how many times each unlock can be used.  
After reaching the set limit of uses, the unlock expires, resetting the moon to its original price.

### Discovery Mode Related

These options provide customization for when you're using Unlock Mode together with Discovery Mode.  
For more information, read the section about [**Discovery Mode**](#discovery-mode).

<details>
  <summary>Show discovery related options</summary>

#### Unlocked Moons are Permanently Discovered
> **Optional:** Keep access to moons once unlocked!*

When a moon is unlocked, it will be permanently discovered, meaning it's added to the moons available in the Terminal's moon catalogue - on top of your base selection.  
This ensures that any moon you've unlocked will always be available for travel in Discovery Mode.

#### Reset Permanent Discoveries on Expiry
> **Optional:** Keeps things fresh by resetting discovery status on expiry!

When the unlock for a permanently discovered moon expires, it will also reset that moon's permanent discovery status, making it disappear from the moon catalogue.  
> This is the only way permanent discoveries can vanish during a run, increasing variety in the late game.
</details>

### Quota Unlocks
> **Optional:** Get rewarded for meeting quotas!

Quota Unlocks reward you for meeting a quota by granting random unlocks for free.  

<details>
  <summary>Quota Unlock Configuration</summary>

- Set the chance to trigger Quota Unlocks.
- Set a minimum and maximum for the number of moons to unlock when triggered.
- Limit Quota Unlocks to moons up to a certain price.
- Limit the total number of times Quota Unlocks can be triggered.
</details>

</details>

## Discount Mode

In Discount Mode, instead of directly unlocking moons, you will unlock a new discount rate each time you buy a moon, eventually making it free.

<details>
  <summary><strong>Learn more</strong></summary>
<br>

Discount Mode provides a balanced approach for when you think directly unlocking moons is too easy or doesn't fit your gameplay style.  

There are configuration options available to customize your experience in Discount Mode.  
Read this section for more information.

### Discount Rates
> **Optional:** Set your own discount rates!

Moons will progress through a configurable list of discount rates. With each discount received, the next rate is unlocked.  
You can set up as many discount rates as you like, and they don't necessarily have to go from low to high.

For example, you could set up discounts so that moons are 50% off after your first purchase, 75% after the second, and free after the third.  
This would be achieved by setting the discount rates config option to `50,75,100`.  
> Typically, the final rate would be 100 - making the moon free like a normal 'Unlock'.

### Discounts Expire
> **Optional:** Set discounts to expire.

You can put a limit on how many times a fully discounted moon can be travelled to for free.
After reaching the set limit of uses, the discount expires, resetting the moon to its original price.  
> This option requires you to set up discount rates so that the final rate is free (100).

### Discovery Mode Related
These options provide customization for when you're using Discount Mode together with Discovery Mode.  
For more information, read the section about [**Discovery Mode**](#discovery-mode).

<details>
  <summary>Show discovery related options</summary>

#### Discounted Moons are Permanently Discovered
> **Optional:** Keep access to discounted moons!

When a moon is discounted, it will be permanently discovered, meaning it's added to the moons available in the Terminal's moon catalogue - on top of your base selection.  
This ensures that any moon you've unlocked a discount for will always be available for travel in Discovery Mode.

#### Reset Permanent Discoveries on Expiry
> **Optional:** Keeps things fresh by resetting discovery status on expiry!

When the discount for a permanently discovered moon expires, it will also reset that moon's permanent discovery status, making it disappear from the moon catalogue.  
> This is the only way permanent discoveries can vanish during a run, increasing variety in the late game.
</details>

### Quota Discounts
> **Optional:** Get rewarded for meeting quotas!

Quota Discounts reward you for meeting a quota by granting random discounts for free.  

<details>
  <summary>Quota Discounts Configuration</summary>

- Set the chance to trigger Quota Discounts.
- Set a minimum and maximum for the number of moons to receive a discount (rate) when triggered.
- Limit Quota Discounts to moons up to a certain price.
- Limit the total number of times Quota Discounts can be triggered.
</details>

### Quota Full Discounts
> **Optional:** Get rewarded for meeting quotas!

Quota Full Discounts reward you for meeting a quota by unlocking the final discount rate for random moons for free.  

<details>
	<summary>Quota Full Discounts Configuration</summary>

- Set the chance to trigger Quota Full Discounts.
- Set a minimum and maximum for the number of moons to receive the final discount rate when triggered.
- Limit Quota Full Discounts to moons up to a certain price.
- Limit the total number of times Quota Full Discounts can be triggered.
</details>

</details>

## Discovery Mode

In Discovery Mode, you start with a limited selection of moons for travel. You will discover new moons as you play.

<details>
  <summary><strong>Learn more</strong></summary>
<br>

The configuration options for this mode are extensive.  
Read this section for more information on Discovery Mode.

### General
Discovery Mode is all about exploration and progression, synergizing really well with having a lot of custom moons.  

There are various ways to discover new moons, adding a fresh layer to the game: *Moon Progression*.  
New moons can be discovered regularly, as rewards, randomly, or any combination of those.

There's even support for moon group matching using custom-defined groups.  
This means you can set up your own moon groups (galaxies, solar systems, tiers, etc.), and when you travel to a moon, you might discover more moons of the same group.  
You can also use your existing **LethalConstellations** for this.

But first, let's start with the basics - your base selections.

### Base Selections (Moon Rotation)
When you start a new game in Discovery Mode, your selection of moons available for travel in the Terminal's moon catalogue is limited.  
The number of available moons is determined by your moon base counts. There are three categories:
- **Free Moons**: Moons that have an original route price of 0 credits.
- **Dynamic Free Moons**: Moons that currently have a route price of 0 credits. Baseline but also through unlocks or discounts.
- **Paid Moons**: Moons you have to pay for travelling to.

You can configure the base count for each of these categories, and you can also have them increase every time the current selection (or rotation) is shuffled.

### Shuffling
By default, every time a new quota begins, the moon rotation will be shuffled, meaning the current selection is discarded, and new moons will be randomly selected.  
You have options to change this to shuffle every day instead or never shuffle at all.

### Discovered Moons (Discoveries)
Every moon available for travel in the Terminal is considered a discovered moon, including the base selections.  
However, as mentioned before, there are more ways to discover moons that will be added on top of your base selection.  
These will *also vanish* when the rotation is shuffled unless you make them permanent.

### Permanently Discovered Moons (Permanent Discoveries)
Permanently discovered moons are just that - permanent. They are added on top of your base selection but *do not* vanish on shuffle.  
Moons can be permanently discovered in various ways, depending on your configuration. 

This could include:
- Making discoveries permanent by unlocking them (or unlocking a discount).
- Making discoveries permanent by landing a set number of times.
- Making discoveries granted by a certain mechanic permanent.

Of course, you can also permanently discover moons from the base selections.

Permanent discoveries allow you to pick moons from your current rotation and keep them for the rest of the run.  
> Combined with the discovery mechanics below, this allows for granting more and more additional discoveries as the quota progresses, but to keep any of them, you have to purchase them.  
> This can incentivize buying moons even several days into a quota, especially when combined with [Moon Sales](#moon-sales).

The only way permanent discoveries can vanish is through the options associated with unlocks and discounts expiring.  
> This can enhance variety in the late game.

### Quota Discoveries
> **Optional**: Get rewarded for meeting quotas!

Quota Discoveries reward you for meeting a quota by granting one or more moon discoveries.  

<details>
	<summary>Quota Discoveries Configuration</summary>

- Set the chance to trigger Quota Discoveries.
- Set a minimum and maximum for the number of moons discovered.
- Make moons discovered this way permanent discoveries.
- Prefer discovering moons from the group of the currently cheapest undiscovered moon.
	- Allows you to unlock the *next best tier* of moons when combined with permanent discoveries.
</details>

### Travel Discoveries
> **Optional**: Discover new moons while travelling!

Travel Discoveries randomly grant moon discoveries as you route and travel to paid moons.  

<details>
	<summary>Travel Discoveries Configuration</summary>

- Set the chance to trigger a Travel Discovery.
- Set a minimum and maximum for the number of moons discovered.
- Make moons discovered this way permanent discoveries.
- Prefer discovering moons that belong to the same group as the one you've routed to (see [Moon Groups](#moon-groups)).
</details>

### New Day Discoveries
> **Optional**: Get more travel options as the quota progresses!

New Day Discoveries randomly grant moons when a new day begins.  
> Combine this with `Unlocks are permanently discovered` and you'll get additional moons to travel to from day to day, but to keep them around you have to buy them or they will vanish when the rotation is shuffled next quota.

<details>
	<summary>New Day Discoveries Configuration</summary>

- Set the chance to trigger a New Day Discovery.
- Set a minimum and maximum for the number of moons discovered.
- Make moons discovered this way permanent discoveries.
- Prefer discovering moons that belong to the same group as the one you're currently located at (see [Moon Groups](#moon-groups)).
</details>

</details>

## Terminal Tags

**Optional:** LethalMoonUnlocks will display information about each moon directly in the Terminal's moon catalogue.

<details>
  <summary><strong>Learn more</strong></summary>
<br>

Terminal Tags are disabled by default.  
If you're using anything but unlocks, it's recommended to turn them on.

Terminal Tags present all information relevant to LethalMoonUnlocks directly in the moon catalogue.  
The tags displayed depend on the current state of each moon and your configuration.

You can enable or disable each tag individually.

Here's an example where I tried to fit all tags on a single screenshot. Explanation for each tag below.

![Example of LMU Terminal Tags](https://i.ibb.co/nrtGcY9/image.png)

<details>
  <summary><strong>Looks too crowded? Check this out</strong></summary>

  There's a config option in the advanced section allowing you to control the maximum tag line length.  
  This can give the moon catalogue a more organized look at the cost of more scrolling.

![More organized example of Terminal Tags](https://i.ibb.co/88ZGLXj/image.png)

</details>

| Tag | Information |
| --- | --- |
| **[IN ORBIT]** | Indicates the moon you're currently orbiting. |
| **[UNEXPLORED]** | Indicates which moons you haven't landed on. |
| **[EXPLORED: X]** | Indicates which moons you have landed on and keeps track of your total landings. |
| **[UNLOCK]** | Indicates the moon is unlocked. |
| **[UNLOCK EXPIRES:X]** | Indicates how many times you can route to the moon for free before the unlock expires. |
| **[DISCOUNT-XX%]** | Discount Mode: indicates the moon is on discount and shows the currently unlocked rate. |
| **[DISCOUNT EXPIRES:X]** | Indicates how many times you can route to the moon for free until the discount expires. |
| **[NEW]** | Indicates the moon has been discovered for the first time this run. Resets every day. |
| **[PINNED]** | Indicates the moon has been permanently discovered - effectively pinning it in the moon catalogue. |
| **[SALE-XX%]** | Indicates the moon is on sale and shows the sales rate. |
| **[MoonGroups]** | For example, [ZEEKERS GALAXY] or [VANILLA/FOREST]. Indicates the name(s) of the custom group(s) or LLL Tag(s) a moon belongs to. Only if moon group custom or tag matching is enabled. |

Tags are added to the moon catalogue using an event provided by LLL and will also show with TerminalFormatter!
</details>

## Moon Sales

**Optional:** Moons have a random chance to go on sale.

<details>
  <summary><strong>Learn more</strong></summary>
<br>

Each moon has a random chance to go on sale every time the sales are shuffled.  
Moon Sales are multiplicative with other price reductions like discounts from [**Discount Mode**](#discount-mode).

You can configure the chance as well as the minimum and maximum sale rate.  
They can either be shuffled every quota or every day.  
> Shuffling Moon Sales daily can incentivize buying moons even days into a quota.
</details>

## Moon Group Matching

LethalMoonUnlocks supports matching moons by group, which is relevant for certain discovery mechanics.

<details>
  <summary><strong>Learn more</strong></summary>
<br>

In Discovery Mode, all new discoveries are randomly selected. With moon group matching, LethalMoonUnlocks will prefer selecting from group matches instead of all moons.

This is always in reference to a *matching moon*. For [**Travel Discoveries**](#optional-travel-discoveries), that is the moon you've routed to, and for [**New Day Discoveries**](#optional-new-day-discoveries), it's the moon you're currently at.

Moon group matching can be disabled individually for each of these mechanics.

There are multiple group matching methods available.

### Price, PriceRange, PriceRangeUpper
All of these methods use the moon's original prices to match them into groups.

- **Price**: Matches all moons that share the exact same price.
- **PriceRange**: Matches all moons within a configurable ± price range.
- **PriceRangeUpper**: Same as PriceRange but only considers equally or more expensive moons.

### Tag
Selects a random LLL content tag and matches all other moons sharing that tag.

### Custom
Custom allows you to define fully custom groups. A group is defined by name and a list of members (moons).  

A moon will always match with all members of the same group.  
If a moon is a member of multiple groups, a random group will be selected for matching.  
When all members of every group are already discovered, a random moon will be chosen instead.

The group name will be displayed in various locations in-game, e.g., *Autopilot discovered new moons during travel to Zeekers Galaxy*.
</details>

## Other Stuff

### Cheap Moon Bias
<details>
  <summary><strong>Learn more</strong></summary>
<br>

LethalMoonUnlocks randomly selects moons during Travel Discovery or for your paid rotation.  
You can use **Cheap Moon Bias** to increase the odds of cheaper moons being selected.  
>This makes it less likely - especially in the early game - to only discover moons you can't afford yet or are considered too valuable.

The bias can be enabled and tweaked individually for every mechanic that randomly selects moons.
</details>

### Compatibility
<details>
  <summary><strong>Learn more</strong></summary>

#### LethalConstellations
LethalMoonUnlocks is compatible with LethalConstellations!  
You can even choose to group match moons by their constellation.

In Discovery Mode, only constellations with discovered moons will be visible and available for routing in the LethalConstellations screen on the Terminal.  
That screen will also show the number of moons currently discovered for every constellation.  
Default moons will be set to the cheapest discovered moon in each constellation.  

Independent of Discovery Mode, you have the option to override the constellation prices with that of the cheapest (currently discovered) moon, which will consequently also apply unlocks, discounts, and sales to constellations.

#### TerminalFormatter
Tags are shown in TerminalFormatter moons node. Thanks @mrov!

#### LethalQuantities
Advanced config option to prefer LQ risk levels in the moon catalogue.

#### Malfunctions
Advanced config option to enable interpreting Malfunctions' Navigation malfunction as routing to a moon.  
If it's a paid moon, LethalMoonUnlocks will see it as buying the moon - even though you didn't pay.

#### All Mods Displaying Alert Messages
LethalMoonUnlocks uses a queue for sending alert messages.  
Alerts from other mods and the vanilla game are added to the same queue to avoid overlapping and missing messages.
</details>

## Example Configurations

In this section, I provide some ideas of what's possible using all the different config options.  
Only showing settings that are not default.

<details>
  <summary><strong>Simple Discounts</strong></summary>

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

A simple setup with slightly modified discounts, rewards on quota completion, and moon sales.

</details>

<details>
  <summary><strong>Progression Focused</strong></summary>

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
Three paid moons available, which are shuffled every quota. Additionally, discover new moons on completing the quota, new day, and traveling.  
Buy them before the quota ends, and you keep them; otherwise, they will be lost with shuffle.  
Repeat every quota and grow your catalogue.

</details>

<details>
  <summary><strong>Full Exploration</strong></summary>

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

> Assumes fully set up custom moon groups.

Start with only one free and two paid moons. Never shuffle the base selection.  
Instead, discover new moons mainly by travel and on new days. Due to moon group matching, you'll discover moons group by group.  
Fully discover a group, and you'll discover moons from other groups.  
Add quota rewards to preference. 

Depending on the number of custom moons you have, a setup like this would probably require modifying quota steepness.

</details>

# Special Thanks

* Huge thanks to @nickham13 for helping me test the mod, suggesting features, and streamlining the config file. Thank you!
* [Permanent Moons](https://thunderstore.io/c/lethal-company/p/BULLETBOT/Permanent_Moons/)
* [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/)
* [LethalNetworkAPI](https://thunderstore.io/c/lethal-company/p/xilophor/LethalNetworkAPI/)
