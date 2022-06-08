# [RebornProfiles][0]

[![Download][1]][2]
[![Discord][3]][4]
[![Sponsor][13]][14]
[![Donate][5]][6]

https://img.shields.io/github/sponsors/domesticwarlord86?color=purple&style=plastic


**RebornProfiles** is a collection of various OrderBot scripts for [RebornBuddy][7].

## Installation

### Prerequisites

- [RebornBuddy][7] with active license (paid)
- [ExBuddy][8] (free)
- [Lisbeth][9] with active license (paid)
- [LlamaLibrary][10] (free)
- Better combat routine, such as [Magitek][11] (free)

### Automatic Setup (recommended)

Want **automatic installation and updates**, including prerequisites?

Install the [RepoBuddy][12] plugin -- `RebornProfiles `is configured by default!

#### Adding `RebornProfiles` to RepoBuddy

ℹ️ New users can skip this step.

In case your repoBuddy config is too old or otherwise missing `RebornProfiles`, you can add it via repoBuddy's settings menu:

- **Name:** RebornProfiles
- **Type:** Profile
- **URL:** `https://github.com/domesticwarlord86/RebornProfiles.git/trunk`

![repBuddy Settings](https://i.imgur.com/KJhwxtw.png)

OR by closing the bot and editing `RebornBuddy\Plugins\repoBuddy\repoBuddyRepos.xml`:

```xml
<Repo>
  <Name>RebornProfiles</Name>
  <Type>Profile</Type>
  <URL>https://github.com/domesticwarlord86/RebornProfiles.git/trunk</URL>
</Repo>
```

### Manual Setup

If you prefer manual setup instead of automatic,

1. Download the [latest version][2].
2. On the `.zip` file, right click > `Properties` > `Unblock` > `Apply`.
3. Unzip all contents into `RebornBuddy\Profiles\` so it looks like this:

```
RebornBuddy
└── Profiles
    └── RebornProfiles
        ├── README.md
        └── ...
```

4. Start RebornBuddy as normal.

## Usage

To load an OrderBot script:

1. Start RebornBuddy and set the BotBase dropdown to `Order Bot`.
2. Click `Load Profile` and navigate to `RebornBuddy\Profiles\RebornProfiles\`.
3. Select the desired `.xml` script from the appropriate subfolder.
4. Back in RebornBuddy, click `Start`.
5. Watch for notifications in the client and logs -- some profiles require intervention!

## Troubleshooting

For live volunteer support, join the [Project BR Discord][4] channel `#domesticwarlord86-profile-help`.

When asking for help, always include:

- which script you loaded,
- what you tried to do,
- what went wrong,
- relevant logs from the `RebornBuddy\Logs\` folder.

No need to ask if anyone's around or for permission to ask -- just go for it!

<!-- ## Looking to Donate? ❤️

[![Donate via Ko-Fi](https://i.imgur.com/bXUIjNA.png)][6] -->

[0]: https://github.com/domesticwarlord86/RebornProfiles "RebornProfiles on GitHub"
[1]: https://img.shields.io/badge/-Download-brightgreen
[2]: https://github.com/domesticwarlord86/RebornProfiles/archive/refs/heads/main.zip "Download"
[3]: https://img.shields.io/badge/Discord-7389D8?logo=discord&logoColor=ffffff&labelColor=6A7EC2
[4]: https://discord.gg/CucSWEhJSZ "Discord"
[5]: https://shields.io/badge/-Buy%20me%20a%20coffee-FF5E5B?logo=kofi&logoColor=ffffff&labelColor=FF5E5B
[6]: https://ko-fi.com/domesticwarlord86 "Donate via Ko-Fi"
[7]: https://www.rebornbuddy.com/ "RebornBuddy"
[8]: https://github.com/Entrax643/ExBuddy "ExBuddy"
[9]: https://www.siune.io/ "Lisbeth"
[10]: https://github.com/nt153133/__LlamaLibrary "LlamaLibrary"
[11]: https://discord.gg/rDsFbKr "Magitek Discord"
[12]: https://github.com/Zimgineering/repoBuddy "RepoBuddy"
[13]; https://github.com/sponsors/domesticwarlord86
[14]; https://img.shields.io/github/sponsors/domesticwarlord86?color=purple&style=plastic
