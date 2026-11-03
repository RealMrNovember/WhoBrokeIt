# Who Broke It?

## Complete Game Design Document, Product Requirements & Technical Implementation Brief

**Document version:** 1.0\
**Project type:** PC multiplayer game for Steam\
**Document purpose:** Single source of truth for Claude IDE and the
development team\
**Working title:** Who Broke It?\
**Tagline:** *The world broke. We make it worse before we make it
better.*

------------------------------------------------------------------------

# 0. How to use this document

This document defines the intended player experience, game systems,
visual direction, interface, multiplayer behaviour, persistence model,
technical architecture, production phases, and acceptance criteria for
the game "Who Broke It?".

It is both a Game Design Document (GDD) and a Product/Technical
Requirements Document (PRD). Treat it as the primary project
specification.

## 0.1 Decisions already established

-   **Platform:** PC, Steam first.
-   **Genre:** Cooperative open-world restoration sandbox with
    physics-driven comedy, light simulation, exploration, and
    management.
-   **Core tone:** Chaos, humour, social interaction, emergent gameplay.
-   **World model:** A persistent shared world, with independent mission
    instances that feed rewards and progress back into the shared world.
-   **Player scale:** Do not design the product around a small fixed
    party size. Support a growing community through world zones and
    separate mission instances. A single physical simulation area still
    has a practical concurrency limit and must be load-tested.
-   **Visual direction:** Stylized, colourful, readable, low-poly /
    mid-poly cartoon art. Avoid photorealism.
-   **Development direction:** Unity 6, C#, Universal Render Pipeline
    (URP), Blender asset workflow, dedicated authoritative servers.
    These are the recommended defaults unless a technical spike
    demonstrates a compelling reason to change them.
-   **Primary design rule:** Fun and readable gameplay take priority
    over visual fidelity and simulation complexity.

## 0.2 Important implementation rule

Do not attempt to build the entire game in one pass. First create a
complete project skeleton and a playable vertical slice. Implement
systems in small, testable phases. Every feature must have a clear
acceptance test.

Claude must inspect the existing repository before changing it. Do not
overwrite working code, configuration, project settings, or assets
without understanding them. Maintain a `README.md`, `CHANGELOG.md`,
`DECISIONS.md`, and phase checklist. Keep code modular, documented, and
buildable.

------------------------------------------------------------------------

# 1. High concept

Who Broke It? is a multiplayer game about rebuilding a broken world with
friends.

A chain of environmental disasters, infrastructure failures, and
economic collapse has left towns, industrial sites, transport networks,
and public spaces abandoned. Players arrive as members of a small
restoration crew. They clean, salvage, repair, transport, rebuild,
explore, and gradually turn ruined districts into lively places again.

The game is not a serious disaster simulator. Its world may be damaged,
but its tone is playful. Equipment is a little unreliable, objects have
satisfying physical behaviour, characters are expressive, and teamwork
regularly turns into slapstick chaos.

Players can cooperate efficiently, compete over optional challenges,
invent their own projects, or simply spend an evening making a mess with
friends.

## 1.1 The core promise

> Join a shared world, bring your friends, pick up a ridiculous tool,
> and help rebuild a town. You can be productive, creative, chaotic, or
> all three in the same five minutes.

## 1.2 Design pillars

1.  **Instantly understandable:** A new player should understand the
    immediate task by looking at the environment and the objective
    marker.
2.  **Fun with friends:** Interactions between players must create
    stories, laughter, teamwork, and occasional harmless sabotage.
3.  **Visible transformation:** Players must clearly see the world
    change as they clean, repair, and rebuild.
4.  **Persistent contribution:** Meaningful work should remain saved in
    the shared world.
5.  **Emergent comedy:** Physics, tools, vehicles, and environmental
    systems should combine to create unexpected moments.
6.  **Low-friction social play:** Inviting friends, joining a group,
    chatting, and starting an activity must be simple.
7.  **Readable stylized art:** Every important object, tool, hazard, and
    interaction must be recognizable at normal gameplay camera distance.
8.  **Expandable world:** New districts, missions, tools, vehicles,
    cosmetics, and events should be addable without redesigning the core
    architecture.

## 1.3 What Who Broke It? is not

-   Not a photorealistic construction simulator.
-   Not a competitive shooter.
-   Not a survival game focused on hunger, thirst, or punishing resource
    loss.
-   Not a city-builder where the player only clicks menus.
-   Not a single-session party game with no persistent progression.
-   Not an MMO promise of unlimited players in one physically simulated
    location.
-   Not a game that requires players to grind for basic fun.

------------------------------------------------------------------------

# 2. Setting, world and narrative

## 2.1 World premise

The world has suffered a sequence of cascading failures called **The
Collapse**. The cause is intentionally broad and should be revealed
through environmental storytelling rather than a long cinematic
exposition.

Power stations stopped operating. Freight routes failed. Factories were
abandoned. Flooding and vegetation reclaimed neighbourhoods. People
moved away, leaving equipment, vehicles, buildings, and infrastructure
behind.

The player joins the **Reclaimers**, a loose network of independent
restoration crews. Their goal is to make abandoned places useful and
welcoming again.

The story should remain light and optimistic. Avoid grim survival
horror. The world is damaged, not hopeless.

## 2.2 Narrative delivery

Use short, optional storytelling: - Radio messages from the Reclaimer
coordinator. - Notes, signs, old posters, maintenance logs, and
environmental clues. - Short introductions when a new district becomes
available. - Mission briefings with a little humour. - NPC requests that
explain why a location matters.

Do not interrupt normal multiplayer sessions with long mandatory
cutscenes.

## 2.3 World structure

The game is divided into connected **districts**. Each district is a
logical world zone with its own theme, restoration stages, points of
interest, and player activity capacity.

Initial district concept:

### District 01 --- Greenfield

A small abandoned town on the edge of a forest.

Landmarks: - Reclaimer Yard: the initial player hub. - Main Street:
shops, houses, road debris, and a small square. - Old Workshop: repair
and crafting tutorial. - Waterworks: first infrastructure restoration
project. - Community Park: landscaping and public-space restoration. -
Freight Depot: vehicle and material logistics. - Hilltop Relay: a small
exploration objective.

Greenfield is the first playable district and must be compact enough for
a vertical slice.

### Future district themes

-   **Industrial Basin:** factories, warehouses, conveyors, cranes,
    power infrastructure.
-   **Coastal Port:** docks, containers, boats, seawalls, flooded areas.
-   **Old Resort:** hotels, pools, gardens, kitchens, service corridors.
-   **Mountain Pass:** roads, tunnels, bridges, rockslides, cable
    systems.
-   **Underground Network:** service tunnels, drainage, metro, hidden
    facilities.

These are expansion targets, not requirements for the first playable
build.

## 2.4 Persistent world stages

Each district has a visible restoration state. A district may move
through broad stages:

1.  **Abandoned:** debris, dirt, broken infrastructure, inaccessible
    areas.
2.  **Cleared:** rubbish and major debris removed; routes become usable.
3.  **Operational:** essential systems such as power and water are
    restored.
4.  **Rebuilt:** buildings and public spaces are repaired.
5.  **Thriving:** district gains decoration, NPC activity, services, and
    new optional projects.

The transformation must be visible in the environment, lighting accents,
soundscape, NPC presence, and available interactions.

The world must not reset when a player logs out.

------------------------------------------------------------------------

# 3. Player journey: from first launch to gameplay

The complete first-time user journey is defined below. All screens must
support keyboard and mouse. UI should be scalable for common PC
resolutions and ultrawide screens.

## 3.1 First launch

On the first launch:

1.  Show the Who Broke It? logo on a simple animated background.
2.  Perform local settings and connection checks.
3.  Show a short legal/privacy notice and EULA acceptance where
    required.
4.  Connect to Steam services and authenticate the player.
5.  If the player is new, create a player profile.
6.  Offer accessibility and basic graphics/audio setup.
7.  Enter the main menu.

Do not show a long loading sequence without feedback. Use a clear
loading indicator and helpful status text such as "Connecting to the
Reclaimer network..."

### First-launch visual treatment

-   Background: stylized abandoned town square, gently moving grass,
    drifting leaves, a flickering streetlight, and a small restoration
    vehicle.
-   Logo: large, friendly, bold lettering. Slightly imperfect industrial
    lettering, not horror typography.
-   Camera: slow cinematic pan or a subtle loop.
-   Audio: warm, playful ambient music with distant birds, wind, and
    occasional machinery.
-   Loading indicator: simple animated tool/bolt icon.
-   No photorealistic post-processing, heavy fog, or dark horror
    presentation.

## 3.2 Main menu

The main menu is the first major interactive screen.

### Layout

**Left side / main navigation** - Play - Join Friends - Missions -
Character - Garage - Progress - Settings - Credits - Exit Game

**Right side** - Player portrait and display name. - Online status. -
Friend activity preview. - Current district status. - Weekly community
event card. - News/update panel.

**Bottom area** - Build/version number in small text, not visually
prominent. - Steam connection status. - Region/network status if useful.

### Main menu behaviour

-   **Play:** Opens the world/session selection flow.
-   **Join Friends:** Shows friends who are online and joinable.
-   **Missions:** Opens available mission categories and recommended
    activities.
-   **Character:** Opens appearance customization.
-   **Garage:** Opens owned vehicles and cosmetic customization.
-   **Progress:** Shows personal and community restoration progress.
-   **Settings:** Opens graphics, audio, controls, accessibility,
    language, and privacy settings.
-   **Exit:** Confirms only if there is unsaved local configuration;
    otherwise exits directly.

The main menu must never trap a player in a submenu. Every screen needs
a clear Back action and consistent navigation.

## 3.3 Player profile creation

On first entry, the player creates a profile.

Fields: - Display name. - Character name (optional; may default to
display name). - Preferred pronouns (optional and cosmetic only). -
Character body preset. - Face preset. - Skin tone palette. - Hair style
and colour. - Outfit. - Voice/emote style (optional, cosmetic only).

Do not ask for real-world personal information. The Steam identity
should be used for authentication, while the in-game display name and
cosmetics are game profile data.

Display names must be moderated and protected against impersonation
where possible.

## 3.4 Onboarding and tutorial

The tutorial takes place in a small fenced section of Reclaimer Yard. It
should be playable solo or with a friend, and it should take
approximately 8--12 minutes.

Tutorial sequence: 1. Move, look, jump, crouch, interact. 2. Pick up a
small object. 3. Carry and place an object. 4. Use a basic cleaning
tool. 5. Use a repair tool on a broken fuse box. 6. Operate a simple
hand trolley. 7. Work with another player to move a heavy object. 8.
Complete a tiny restoration objective. 9. Receive the first reward and
unlock the main district.

Tutorial dialogue should be short and humorous. Teach mechanics through
interaction, not text walls.

Allow experienced players to skip the tutorial. Skipping must not remove
essential unlocks; grant the same starter equipment.

------------------------------------------------------------------------

# 4. Session, party, lobby and social systems

Social functionality is a core product feature, not a secondary menu.

## 4.1 Social terminology

-   **Friend:** A Steam friend or supported in-game friend.
-   **Party:** A temporary group of players who want to play together.
-   **Lobby:** A pre-activity social room where party members gather,
    chat, configure privacy, and launch an activity.
-   **World session:** A live instance of a persistent district.
-   **Mission instance:** A separate activity session that returns
    rewards and results to the persistent world.
-   **Host/leader:** The party member who controls activity selection
    and party settings. Dedicated servers remain authoritative; the host
    is not the simulation authority.

## 4.2 Party creation

From the main menu, the player can: - Create a party. - Join a friend. -
Accept a party invitation. - Join a public party if enabled. - Rejoin
the last party if it still exists.

Party privacy: - **Friends:** Friends of party members can request to
join, subject to leader approval. - **Invite Only:** Only invited
players can join. - **Public:** The party appears in a public party
browser, subject to moderation and capacity. - **Closed:** No new
members.

The first release may use a practical party capacity such as 8--12 for
the pre-game lobby. This is a lobby limit, not a promise about the whole
shared world. Keep capacity configurable in backend settings.

## 4.3 Lobby screen

The lobby must feel like a small social space, not a spreadsheet.

### Lobby visual layout

**Top bar** - Who Broke It? logo. - Party name or activity name. - Privacy
indicator. - Connection status.

**Central area** - 3D character staging area or a clean stylized
backdrop. - Party members shown as full-body avatars. - Each avatar can
idle, wave, sit, dance, or perform simple emotes. - Empty slots show
"Invite a friend" or "Waiting for crew". - Ready state is visible above
or below each avatar.

**Left panel --- Party members** - Avatar. - Display name. - Ready/not
ready. - Voice connection indicator if voice is implemented. - Leader
crown/icon. - Context menu: View profile, Mute, Block, Report, Remove
from party (leader only).

**Right panel --- Activity** - Activity selection. - Selected district
or mission. - Difficulty or optional modifiers. - Expected duration. -
Reward preview. - Start/Launch button for leader.

**Bottom panel --- Chat** - Party chat tab. - System messages tab. -
Text input. - Send button. - Timestamp and sender name. - Scroll
history. - Basic emoji support. - Enter sends; Shift+Enter creates a new
line. - Chat must not cover ready states or important controls.

### Lobby interactions

-   Players can move their avatar around a small staging area if
    practical.
-   Players can use emotes.
-   Players can change character cosmetics without leaving the party.
-   Players can inspect another member's profile.
-   Players can ready/unready.
-   The leader can change activity and privacy.
-   The leader can transfer leadership.
-   The leader can kick a member with a confirmation dialog.
-   Members can leave the party at any time.
-   The party should survive a leader disconnect where possible; assign
    a new leader using a deterministic rule.

### Ready system

-   Every member has a Ready/Not Ready state.
-   Changing activity resets readiness.
-   Joining the lobby starts as Not Ready.
-   The leader cannot launch until all required members are ready,
    unless the selected activity explicitly allows unready members.
-   Show a clear message explaining who is not ready.
-   Do not silently launch a player into an activity they did not
    accept.

## 4.4 Friend list and invitations

The friend panel must be accessible from the main menu and lobby.

Friend states: - Online. - In game. - In party. - Away. - Offline.

Friend actions: - Invite to party. - Join (if privacy and capacity
permit). - Whisper/direct message (optional; can be deferred). - View
profile. - Remove friend. - Block. - Report.

Invitation behaviour: - Invitation appears as an in-game notification
and Steam overlay notification where supported. - Show inviter, party
name, current activity, and expiry. - Accept / Decline. - If the player
is already in a party, ask whether they want to leave or switch. -
Prevent duplicate invitation spam. - Add configurable invite
cooldowns. - Invitations expire after a configurable period. - If the
party becomes full, show a clear error. - If the inviter disconnects,
invalidate the invite gracefully.

## 4.5 Party and lobby chat

Chat is text-first for the initial implementation. Voice chat is
optional and should not block the first release.

Requirements: - Party-only chat. - System messages for joins, leaves,
readiness, activity changes, and mission launch. - Basic profanity
filtering with user-configurable filtering where appropriate. - Mute,
block, and report. - Rate limiting and spam protection. - Message length
limit. - Server-side validation. - Do not store chat indefinitely by
default. Define a short retention period for moderation/security
requirements and comply with applicable privacy law. - Clearly
communicate reporting and moderation rules.

## 4.6 Voice chat (later phase)

If added: - Push-to-talk default. - Party channel. - Proximity channel
in world sessions as an optional mode. - Mute individual players. -
Master voice volume. - Input/output device selection. - Voice activity
indicator. - No automatic voice recording. - Platform and privacy
compliance review before release.

## 4.7 Public party browser (later phase)

A searchable browser may show: - Party activity. - Language. - Region. -
Open slots. - Privacy/join policy. - Short party description.

Do not expose sensitive player data. Add reporting and moderation
controls.

------------------------------------------------------------------------

# 5. World entry and in-game HUD

## 5.1 Entering a world

After choosing Play: 1. Show available shared districts and the player's
last location. 2. If joining a party, use the party leader's selected
destination. 3. Show district population/load status. 4. Select an
appropriate world zone instance. 5. Connect to an authoritative
dedicated server. 6. Load the district and spawn at a safe spawn point
or party rendezvous point. 7. Restore the player's saved equipment and
appearance. 8. Show a short "Welcome back" panel with recent district
changes.

Do not spawn players inside vehicles, colliders, hazards, or active
construction geometry.

## 5.2 Third-person camera

Default camera: third person.

Camera requirements: - Follow the player smoothly. - Mouse-controlled
orbit. - Adjustable sensitivity. - Invert Y option. - Camera collision
to avoid clipping through walls. - Configurable field of view. -
Optional shoulder swap. - Camera shake must be subtle and adjustable. -
Do not use excessive motion blur. - Do not force camera movement during
normal gameplay.

First-person mode may be considered later, but it is not required for
the initial release.

## 5.3 HUD layout

The HUD must be minimal and readable.

**Top-left** - Current objective. - Objective progress. - Optional party
contribution indicator.

**Top-centre** - Contextual district/event notification. - Temporary
interaction prompts.

**Top-right** - Minimap or compact compass. - Nearby party members. -
District name and status.

**Bottom-left** - Player status: stamina only if stamina is
implemented. - Interaction/tool status. - Voice status if voice chat
exists.

**Bottom-centre** - Tool/quick-slot bar. - Current equipped tool. -
Contextual controls.

**Bottom-right** - Resource count relevant to the current activity. -
Vehicle controls when driving.

HUD rules: - Never show every resource all the time. - Contextualize
information. - Support UI scale settings. - Use icons plus text for
critical objectives. - Provide colour-blind-safe status indicators;
never rely on colour alone.

## 5.4 Interaction system

Use a consistent interaction model: - Aim at an interactable object. -
Show its name and action. - Press and hold or tap the interaction key as
appropriate. - Show progress for timed interactions. - Cancel cleanly if
the player moves away or is interrupted.

Example: `[E] Repair Fuse Box` `[Hold E] Pick Up Container`
`[F] Enter Forklift`

Interaction prompts must not overlap or flicker rapidly when several
objects are close together. Use priority rules and distance checks.

------------------------------------------------------------------------

# 6. Core gameplay loop

## 6.1 Main loop

1.  Enter the shared district.
2.  Find a restoration project or accept a mission.
3.  Gather tools, equipment, and materials.
4.  Work alone or coordinate with nearby players.
5.  Clean, move, repair, build, or explore.
6.  Complete milestones.
7.  Earn credits, reputation, cosmetics, and district contribution.
8.  See the environment visibly improve.
9.  Unlock new projects and equipment.
10. Return to social play, another mission, or free-form sandbox
    activity.

The loop must be rewarding even when a player only has 10--20 minutes.

## 6.2 Core verbs

The game should be built around a small, reusable set of verbs: -
Move. - Look. - Jump. - Crouch. - Interact. - Pick up. - Carry. -
Drop. - Push. - Pull. - Clean. - Repair. - Build/place. - Drive. -
Operate. - Inspect. - Emote.

Each new tool should extend these verbs rather than introduce an
entirely unrelated control scheme.

## 6.3 Restoration projects

A project consists of: - Project identity and location. - Current
stage. - Required actions. - Required materials. - Progress state. -
Contributors. - Reward table. - Visual state transitions. - Completion
criteria.

Example: **Reopen the Old Workshop** 1. Remove rubbish from the
entrance. 2. Move scrap metal to the recycling point. 3. Repair the
power box. 4. Replace the broken door. 5. Clean the workshop floor. 6.
Deliver a tool cabinet. 7. Activate the workshop.

Each step should have physical, visual feedback.

## 6.4 Task design principles

-   Avoid long repetitive chores without variation.
-   A task should usually take 30 seconds to 5 minutes.
-   Larger projects can contain several short tasks.
-   Tasks should support multiple players where possible.
-   Provide alternative approaches when practical.
-   Make the objective visually obvious.
-   Use humour in descriptions and NPC dialogue.
-   Do not punish a player heavily for accidental mistakes.

------------------------------------------------------------------------

# 7. Physics, comedy and player interaction

Physics-driven comedy is one of the game's defining features. It must be
reliable and readable, not random chaos that makes the game frustrating.

## 7.1 Physics principles

-   Server-authoritative gameplay for important networked physics.
-   Predict local movement where safe.
-   Use simplified physics for distant or low-priority objects.
-   Use stable collision shapes.
-   Avoid excessive numbers of fully simulated rigidbodies.
-   Important objects must not teleport or jitter during normal play.
-   Use deterministic state transitions for critical project objects
    where possible.
-   Apply ownership/authority rules to prevent clients from cheating.

## 7.2 Player interactions

Initial interactions: - Gentle push. - High-five. - Wave. - Point. -
Carry an object together. - Pass an object. - Help a fallen player stand
up. - Sit on suitable props. - Use harmless emotes.

Avoid enabling unrestricted player damage or griefing. The game is
cooperative and humorous, not hostile.

## 7.3 Comedy systems

Comedy should emerge from: - Heavy objects with momentum. - Vehicles
with turning radius and braking distance. - Swinging loads. - Wobbly
scaffolding. - Misaligned placement. - Water pressure and hoses. -
Cables and power tools. - Environmental hazards. - Cooperative lifting
that requires coordination.

Provide safety valves: - Reset a stuck object after a delay. - Return
mission-critical objects to a checkpoint. - Prevent permanent
soft-locks. - Allow a leader or authorized player to reset a mission
object. - Keep destructive effects reversible in the shared world.

## 7.4 Physics tuning

The game should feel slightly exaggerated: - Objects are a little
lighter than realistic. - Impacts are readable and satisfying. -
Character reactions are expressive. - Small mistakes can create funny
chain reactions. - Large collisions should not cause unavoidable instant
failure.

Use sound, animation, particles, and camera response to sell impact
rather than relying on extreme physical forces.

------------------------------------------------------------------------

# 8. Tools, equipment and vehicles

## 8.1 Starter tools

The vertical slice should include: 1. **Grabber / hands:** Pick up and
move small objects. 2. **Broom:** Push small debris into collection
zones. 3. **Repair hammer:** Repair marked components. 4. **Pressure
washer:** Clean surfaces using a readable dirt-removal mask or staged
material state. 5. **Hand trolley:** Move several objects more
efficiently. 6. **Basic tool bag:** Holds selected equipment.

Do not build complex tool systems before the core interaction loop
works.

## 8.2 Tool behaviour

Every tool needs: - Clear use animation. - Audio feedback. - Visual
feedback. - Interaction range. - Tool-specific input. - Cooldown or
resource cost only if it improves gameplay. - Network synchronization. -
Failure and interruption behaviour. - Accessibility options where
appropriate.

## 8.3 Initial vehicle: forklift

The forklift is the signature vehicle for the first vertical slice.

Requirements: - Enter/exit interaction. - Basic acceleration, braking,
steering, reverse. - Fork lift/lower control. - Load attachment and
detachment. - Stable centre of mass. - Readable load capacity. - Horn. -
Vehicle camera. - Collision response. - Server-authoritative vehicle
state. - Reset-to-safe-position option if stuck.

The forklift should be fun to drive, slightly exaggerated, and
forgiving. It must not be so realistic that new players struggle to use
it.

## 8.4 Future vehicles

-   Utility pickup.
-   Small loader.
-   Flatbed truck.
-   Mini excavator.
-   Tow truck.
-   Small crane.
-   Street sweeper.
-   Water tanker.

These are future additions, not first-slice requirements.

------------------------------------------------------------------------

# 9. Building and placement

## 9.1 Building philosophy

Building should be approachable and satisfying. It is not intended to be
a full professional CAD or city-planning simulator.

Initial placement categories: - Modular walls. - Floors. - Doors. -
Windows. - Fences. - Lights. - Benches. - Planters. - Signs. - Small
utility structures.

## 9.2 Placement flow

1.  Open the build menu.
2.  Choose a category and item.
3.  Preview a ghost object.
4.  Rotate and move the object.
5.  Show valid/invalid placement clearly.
6.  Confirm placement.
7.  Server validates ownership, resources, collision, and permissions.
8.  Object is saved to the world.

Include snapping to grids and compatible sockets. Allow free placement
only where safe.

## 9.3 Permissions

The district owner/community or project permissions determine who can: -
Build. - Remove. - Move. - Upgrade. - Decorate.

For the initial shared world, use simple rules: - Public restoration
projects are editable by authorized players in the zone. - Personal
decoration areas are owned by the player or group. - Critical
infrastructure cannot be destroyed by ordinary players. - All
destructive edits should be reversible or logged.

------------------------------------------------------------------------

# 10. Exploration and independent missions

The persistent world is the social home. Missions provide focused
activities that can be completed independently and return rewards to the
shared world.

## 10.1 Mission categories

-   Cleanup.
-   Repair.
-   Salvage.
-   Transport.
-   Search and recovery.
-   Infrastructure restart.
-   Exploration.
-   Time trial.
-   Cooperative challenge.
-   Weekly community event.

## 10.2 Mission lifecycle

1.  Mission appears on a board, map, NPC, or event notification.
2.  Player or party inspects details.
3.  Party selects mission.
4.  Lobby shows requirements, estimated duration, and reward.
5.  Members ready up.
6.  Dedicated mission instance is allocated.
7.  Players load into the mission.
8.  Objectives and optional challenges are completed.
9.  Results screen displays contribution and rewards.
10. Rewards and district impact are committed to the persistent backend.
11. Players return to the shared district or lobby.

## 10.3 Mission design example: Flooded Depot

**Premise:** A freight depot is flooded after a storm.

Objectives: - Find the main power isolation switch. - Remove debris
blocking the drainage channel. - Carry portable pumps into position. -
Repair a damaged power cable. - Drain the loading area. - Recover three
supply crates. - Deliver the crates to the Reclaimer Yard.

Optional: - Finish under a time target. - Recover a hidden toolbox. -
Avoid damaging the forklift. - Complete the job with no player downed.

Rewards: - Credits. - Reclaimer reputation. - Depot restoration
contribution. - A cosmetic decal or emote chance.

The mission should support multiple roles without forcing a fixed class
system.

## 10.4 Mission modifiers

Examples: - Low visibility. - Limited equipment. - Extra debris. -
Slippery surfaces. - Unstable cargo. - Time pressure. - Bonus objective.

Modifiers should create variety, not unfair difficulty.

------------------------------------------------------------------------

# 11. Progression, economy and rewards

## 11.1 Progression goals

Progression should unlock options, not basic fun.

Players should gain: - New tools. - Vehicle variants. - Cosmetic
outfits. - Emotes. - Decorations. - Access to new districts. - New
project types. - Titles and badges.

Do not make core movement, basic interaction, or essential cooperative
tools grind-gated.

## 11.2 Currencies

Initial proposal: - **Credits:** Earned through jobs and used for tools,
vehicles, and practical upgrades. - **Reputation:** Earned through
restoration contribution and used to unlock districts/projects. -
**Cosmetic tokens:** Optional event reward currency. Avoid excessive
currency complexity.

Avoid pay-to-win design. Monetization decisions must not compromise fair
cooperative play.

## 11.3 Contribution scoring

Contribution can consider: - Objective completion. - Materials
delivered. - Repairs completed. - Objects moved to correct locations. -
Support actions. - Teamwork bonuses. - Optional objectives.

Do not reward only the player who performs the final interaction. Avoid
incentives that make players compete destructively over tasks.

## 11.4 Reward screen

After a mission: - Mission name and outcome. - Team result. - Personal
contribution summary. - Credits/reputation earned. - District
contribution. - New unlocks. - Optional highlights such as "Most
Helpful", "Best Forklift Recovery", or "Unexpected Chaos". - Continue /
Return to World / Replay options.

Awards must be positive and humorous, never humiliating.

------------------------------------------------------------------------

# 12. Character customization and animation

## 12.1 Art direction

Characters are stylized, friendly, slightly exaggerated workers.

Visual traits: - Slightly oversized head and hands. - Compact torso and
limbs. - Clear silhouette. - Simple readable facial features. - Strong
colour blocking. - Expressive idle and movement animation. - Slightly
exaggerated reactions.

Avoid uncanny realistic faces.

## 12.2 Modular character system

Use one shared base skeleton and modular cosmetic parts: - Head/face
presets. - Hair. - Headwear. - Upper-body clothing. - Lower-body
clothing. - Gloves. - Boots. - Back accessories. - Glasses/masks. -
Colour variants.

Cosmetics should not affect gameplay hitboxes or tool reach.

## 12.3 Animation set

Minimum: - Idle. - Walk. - Run. - Sprint if used. - Jump. - Fall. -
Land. - Crouch. - Pick up. - Carry. - Drop. - Push. - Pull. - Repair. -
Sweep. - Pressure wash. - Use tool. - Enter/exit vehicle. - Sit. -
Wave. - Celebrate. - Help another player up.

Use animation layers or additive animation where practical. Tool
animations should blend with movement only when it looks acceptable.

## 12.4 Character audio

-   Footsteps vary by surface.
-   Tool sounds are distinct.
-   Short non-verbal effort sounds may be used.
-   Avoid constant voice barks.
-   Provide volume controls.
-   Do not use licensed voices or music without permission.

------------------------------------------------------------------------

# 13. Environment art bible

## 13.1 Visual identity

**Style name:** Friendly Industrial Low-Poly.

The world should feel like a colourful toy-like miniature environment
that has been abandoned and reclaimed by nature.

References are mood references only. Do not copy protected characters,
maps, logos, or distinctive assets from other games.

Visual goals: - Stylized low-poly / mid-poly geometry. - Soft, broad
forms. - Clear silhouettes. - Warm natural lighting. - Saturated but
controlled colours. - Hand-painted or simple procedural textures. -
Modular architecture. - Readable material differences. - Strong visual
transformation between abandoned and restored states.

## 13.2 Colour palette

Suggested palette: - Forest green: nature and restoration. - Muted teal:
infrastructure and water. - Warm ochre: construction and machinery. -
Rust orange: abandoned industrial objects. - Concrete grey: damaged
structures. - Cream/off-white: UI surfaces and readable labels. - Bright
safety yellow: interaction and hazard highlights. - Red: critical
warning only.

Do not make the whole world brown, grey, or desaturated. The game should
remain cheerful and readable.

## 13.3 Geometry and polygon budget philosophy

Do not enforce arbitrary polygon counts without profiling. Use these
principles: - Simple silhouettes for small props. - Moderate detail for
hero props and vehicles. - Modular building pieces. - Reuse meshes and
materials. - Use LODs for large environments. - Combine static
environment geometry where beneficial. - Avoid excessive unique
materials. - Use GPU instancing for repeated props. - Use impostors or
simplified representations for distant detail where appropriate.

## 13.4 Texture and material direction

Target: - Mostly 512px or 1K textures for common assets. - 2K only for
selected large hero assets when justified. - Reuse trim sheets and
material atlases. - Use simple roughness/metallic variation. - Avoid
expensive layered shaders. - Use vertex colours for dirt, moss, and
variation where appropriate. - Use staged material swaps or masks for
restoration states.

Texture resolution is a budget guideline, not a hard rule. Profile
memory on target hardware.

## 13.5 Lighting and rendering

Use Unity URP.

Recommended direction: - Baked or mixed lighting for static environments
where appropriate. - Limited real-time shadow casters. - Moderate shadow
distance. - Simple sky and atmospheric colour. - Optional screen-space
effects only where affordable. - Avoid heavy ray tracing. - Avoid
requiring high-end global illumination. - Use reflection probes
selectively. - Use occlusion culling where useful. - Profile on an 8 GB
VRAM target.

The visual result should come from art direction, composition, colour,
animation, and sound---not expensive rendering features.

## 13.6 Environment asset checklist

Greenfield vertical slice: - Modular house set. - Small shop front. -
Workshop building. - Road and pavement modules. - Broken streetlights. -
Fences and gates. - Trees, bushes, grass clumps. - Rubbish bags and
debris. - Pallets and crates. - Scrap metal piles. - Utility poles and
cables. - Fuse boxes. - Water pipes. - Drain covers. - Benches and
planters. - Signs and road markings. - Reclaimer Yard props. - One
forklift. - One small hand trolley. - Basic tools.

## 13.7 Restoration visual states

Each restorable object should have at least two clearly distinguishable
states; major projects should have multiple stages.

Examples: - Dirty floor → partly cleaned → clean. - Broken light →
repaired but off → powered on. - Damaged wall → patched → painted. -
Overgrown park → cleared → landscaped. - Dead machine → repaired →
animated/operational.

Use mesh variants, material states, decals, particles, and simple
animation. Avoid complex runtime geometry destruction in the first
version.

------------------------------------------------------------------------

# 14. UI and UX visual design

## 14.1 UI identity

The UI should feel like a modern workshop management tablet: - Dark
forest-green / charcoal base. - Warm off-white text. - Safety-yellow
accent for primary actions. - Rounded but not overly bubbly panels. -
Strong hierarchy. - Simple industrial iconography. - Subtle motion. - No
oversized gradients or excessive glass effects.

## 14.2 Typography

Use a readable sans-serif font with broad language support. Use: - Bold
headings. - Medium-weight section labels. - Regular body text. - Tabular
numerals for quantities and timers.

Check font licensing before shipping.

## 14.3 UI states

Every interactive component must have: - Default. - Hover. - Pressed. -
Focused. - Disabled. - Loading. - Success. - Error. - Empty state.

Keyboard focus must be visible. Support remappable controls.

## 14.4 Required screens

1.  Splash/loading.
2.  First-launch setup.
3.  Main menu.
4.  Profile creation.
5.  Character customization.
6.  Friend list.
7.  Party lobby.
8.  Party chat.
9.  Mission browser.
10. District/world selection.
11. Loading screen.
12. In-game HUD.
13. Interaction panel.
14. Build menu.
15. Map.
16. Inventory/tool wheel.
17. Vehicle interface.
18. Pause menu.
19. Settings.
20. Mission results.
21. Player profile.
22. Garage.
23. Progression/rewards.
24. Report/block/mute dialogs.
25. Reconnect/error screens.

## 14.5 Settings

Graphics: - Resolution. - Display mode. - VSync. - Frame-rate cap. -
Quality preset. - Shadow quality. - View distance. - Anti-aliasing. -
Texture quality. - FOV. - UI scale.

Audio: - Master. - Music. - Effects. - Ambient. - Voice (if
implemented).

Controls: - Rebind keys. - Mouse sensitivity. - Invert Y. - Toggle/hold
options.

Accessibility: - Subtitle size. - UI scale. - Colour-blind support. -
Camera shake. - Motion effects. - Interaction hold/toggle. - Text chat
display options.

Language: - English and Turkish at minimum for the first public test if
feasible. - All UI strings must use localization keys, not hard-coded
text.

------------------------------------------------------------------------

# 15. Audio direction

Audio is a major part of comedy and feedback.

## 15.1 Music

-   Warm, playful, slightly quirky.
-   Light industrial percussion and acoustic elements.
-   Calm exploration themes.
-   More energetic restoration/event themes.
-   Music should not overpower voice or tool sounds.
-   Avoid constant music loops during long sessions.

## 15.2 Sound effects

Priorities: - Tool start/stop. - Cleaning and repair feedback. - Object
pickup/drop. - Material impacts. - Vehicle engine and horn. - UI
interactions. - Objective completion. - District restoration
milestones. - Mission success/failure. - Character footsteps.

## 15.3 Comedy audio

Use short, original, non-verbal reactions and exaggerated but restrained
sound effects. Avoid copying famous meme sounds or copyrighted music.

------------------------------------------------------------------------

# 16. Multiplayer and network architecture

## 16.1 Architecture principles

-   Dedicated authoritative servers for live gameplay.
-   Clients send input/requests; server validates important actions.
-   Server owns mission state, inventory changes, project progress,
    vehicle ownership, and critical physics.
-   Client-side prediction for responsive movement where appropriate.
-   Interpolation for remote players.
-   Interest management: clients receive only relevant nearby entities
    and required global state.
-   Separate persistent backend services from real-time simulation.
-   Reconnection support.
-   Graceful handling of disconnects and server restarts.

## 16.2 Player scale and world zoning

Do not claim unlimited players in one physical simulation. Design for a
large total community through: - Multiple instances of the same
district. - Logical world zones. - Zone transfer. - Separate mission
instances. - Shared global progression and economy. - Player density
controls. - Server allocation based on population and load.

A zone should have a configurable concurrency target. The initial
prototype may use 4--8 players. Later load tests determine safe values.

When a zone is full: - Offer another instance. - Allow party members to
stay together where possible. - Avoid silently splitting a party. - Show
population and queue status.

## 16.3 Networked entity priorities

High priority: - Nearby players. - Vehicles in use. - Objects currently
manipulated. - Active project state. - Mission objectives. - Important
hazards.

Lower priority: - Distant ambient props. - Decorative NPCs. - Distant
non-interactive physics. - Cosmetic particles.

Do not replicate every tiny object at full frequency.

## 16.4 Disconnect and reconnect

If a player disconnects: - Keep their persistent inventory and profile
safe. - Release temporary vehicle control safely. - Preserve completed
project progress. - Allow reconnect for a configurable grace period. -
Rejoin party if still active. - If in a mission, restore at a safe
checkpoint if the mission supports it. - Never duplicate rewards on
reconnect.

## 16.5 Anti-cheat and trust

-   Validate currency and rewards on the server.
-   Validate inventory changes.
-   Validate project completion.
-   Rate-limit requests.
-   Do not trust client-reported mission results.
-   Protect APIs with authentication and authorization.
-   Log important economy and moderation events.
-   Do not expose server secrets in the client.

------------------------------------------------------------------------

# 17. Backend and persistence

The game requires persistent accounts, characters, social relationships,
progression, world state, and mission results.

## 17.1 Suggested service boundaries

-   Authentication / Steam identity integration.
-   Player profile service.
-   Party and invitation service.
-   Presence service.
-   Chat service.
-   World/district state service.
-   Mission orchestration service.
-   Inventory and economy service.
-   Reward/progression service.
-   Moderation/reporting service.
-   Telemetry and analytics.

These may begin as a modular monolith. Do not introduce microservices
prematurely.

## 17.2 Suggested backend stack

Recommended initial backend: - ASP.NET Core Web API or a similarly
maintainable backend. - PostgreSQL for persistent data. - Redis for
transient presence, party state, and caching if needed. - Docker for
deployment. - Dedicated game server builds separate from the web API. -
Structured logs and metrics.

The backend language is not a gameplay constraint. Unity gameplay
remains C#.

## 17.3 Core data entities

At minimum, model: - Player. - PlayerProfile. - CharacterAppearance. -
FriendRelationship. - BlockRelationship. - Party. - PartyMember. -
PartyInvitation. - ChatMessage (with defined retention). - District. -
DistrictInstance. - DistrictRestorationState. - RestorationProject. -
ProjectContribution. - MissionDefinition. - MissionInstance. -
MissionParticipant. - MissionResult. - Inventory. - InventoryItem. -
CurrencyLedger. - Unlock. - VehicleOwnership. - VehicleState (only
persistent fields). - PlayerReport. - ModerationAction. - Entitlement
(if paid content is introduced).

Use stable IDs. Add created/updated timestamps. Use migrations. Define
transaction boundaries for rewards and economy.

## 17.4 Persistence rules

Persist: - Player profile and cosmetics. - Inventory and unlocks. -
Currency ledger. - Mission completion and rewards. - District
restoration milestones. - Approved player-built objects. -
Personal/group decoration. - Settings.

Do not persist every transient physics transform. Save only important
world state and checkpoints.

## 17.5 World save strategy

Use event or transaction records for meaningful changes: - Project stage
completed. - Infrastructure restored. - Building placed. - Decoration
changed. - Mission reward committed.

Batch non-critical updates. Use periodic snapshots for world state.
Ensure a crash cannot duplicate currency or rewards.

------------------------------------------------------------------------

# 18. Steam integration and release requirements

Steam is the first and only required platform for the initial target.

Integrate where appropriate: - Steam authentication. - Friends list. -
Invites. - Rich presence. - Overlay. - Achievements. - Cloud saves for
appropriate local settings only; authoritative progression remains
server-side. - Steam Deck compatibility can be evaluated later.

Do not assume every Steam feature is available without the relevant SDK
setup, app configuration, and review. Keep the integration behind an
abstraction so development can use a local test identity.

------------------------------------------------------------------------

# 19. Performance and hardware targets

## 19.1 Player hardware target

The game should be designed for mainstream PC hardware, not only
high-end machines.

Initial target profile: - 1080p. - 60 FPS target on a reasonable
mid-range gaming PC. - 8 GB VRAM compatibility target. - SSD
recommended. - Scalable quality settings. - Low/medium settings should
remain playable on older supported hardware after profiling.

Do not promise exact minimum specifications until the vertical slice has
been profiled.

## 19.2 Developer GPU servers

The project has access to two separate servers, each with an RTX 5060 8
GB GPU.

Use them as independent workers: - **Node A:** Blender rendering,
AI-assisted concept/texture work, asset baking, visual generation. -
**Node B:** Unity build/test automation, secondary asset generation,
graphics profiling, and multiplayer test support where GPU work is
useful.

Important: two separate 8 GB GPUs do not automatically behave as one 16
GB GPU. Each machine has its own memory. The dedicated game server
should be designed around CPU, RAM, storage, and network performance; a
GPU is not normally required for headless authoritative simulation.

## 19.3 Optimization rules

-   Profile early, not at the end.
-   Use Unity Profiler and Frame Debugger.
-   Monitor CPU, GPU, memory, draw calls, network bandwidth, and server
    tick time.
-   Avoid uncontrolled runtime rigidbody counts.
-   Pool frequently spawned objects.
-   Use LODs and culling.
-   Use shared materials and instancing.
-   Keep particle counts bounded.
-   Use network interest management.
-   Test crowded scenes, not only empty maps.

------------------------------------------------------------------------

# 20. Accessibility, safety and community health

-   Player mute, block, and report.
-   Party privacy controls.
-   No forced voice chat.
-   Text chat controls.
-   Clear moderation rules.
-   Anti-spam protections.
-   Safe handling of player-built content.
-   Avoid mechanics that enable harassment or permanent destruction of
    other players' work.
-   Provide a way to recover from stuck states.
-   Support remappable controls and scalable UI.
-   Avoid flashing effects that may create accessibility issues.
-   Provide camera shake and motion-effect controls.

------------------------------------------------------------------------

# 21. Monetization principles

Monetization is not part of the first prototype. The game must first
prove that its core loop is fun.

Potential future model: - Premium base game on Steam. - Optional
cosmetic packs. - Optional district/expansion content only if it does
not fragment the community unfairly. - Seasonal cosmetic events.

Never sell: - Gameplay power. - Faster restoration progress that creates
unfair advantages. - Essential tools. - Access to core cooperative
mechanics.

Avoid pay-to-win and manipulative monetization.

------------------------------------------------------------------------

# 22. Analytics and success metrics

Measure product health without collecting unnecessary personal data.

Useful metrics: - First-session completion rate. - Tutorial completion
rate. - Time to first cooperative interaction. - Party creation and
invite acceptance rate. - Average session duration. - Return rate. -
Mission completion rate. - Disconnect/reconnect rate. - Crash rate. -
Server tick performance. - Average players per zone. - Most-used tools
and vehicles. - Restoration project completion. - Reports and moderation
actions.

The most important early qualitative question is:

**Do players create funny, memorable stories without the game forcing a
scripted joke?**

Run small playtests and observe where players laugh, get confused,
become bored, or need help.

------------------------------------------------------------------------

# 23. Development roadmap

## Phase 0 --- Repository and technical foundation

Deliverables: - Unity 6 project. - URP configured. - Git repository and
folder conventions. - Input System. - Basic character controller. -
Camera. - Interaction framework. - Build pipeline. - README and decision
log.

Acceptance: - Project opens without errors. - Windows development build
runs. - Player can move, look, jump, and interact with a test object. -
No unnecessary packages or architecture are added.

## Phase 1 --- Single-player gameplay prototype

Deliverables: - Small test yard. - Character controller. - Pick
up/drop. - Push/pull. - Broom or cleaning tool. - Repair interaction. -
One restoration objective. - Basic UI and audio feedback.

Acceptance: - A player can complete a small restoration job. - Every
interaction has clear feedback. - The environment visibly changes. - No
major stuck states.

## Phase 2 --- Multiplayer vertical slice

Deliverables: - Dedicated server prototype. - 4--8 player test. - Player
synchronization. - Object interaction synchronization. - Basic vehicle
synchronization. - Disconnect handling. - Party creation and join flow.

Acceptance: - Several clients can join the same test district. - Players
see each other's movement and interactions. - Important objects remain
consistent. - Server remains authoritative for project progress. -
Reconnection does not duplicate rewards.

## Phase 3 --- Social lobby and Steam

Deliverables: - Main menu. - Steam identity integration. - Friend
list. - Party invites. - Lobby. - Ready system. - Party chat. - Privacy
settings. - Join friend flow.

Acceptance: - A player can invite a Steam friend. - The friend can
accept and join the party. - Both can chat and ready up. - Leader can
select an activity and launch. - Disconnects and full-party errors are
handled cleanly.

## Phase 4 --- Greenfield district

Deliverables: - First art-complete district slice. - Reclaimer Yard. -
Main Street. - Workshop. - Waterworks. - Park. - Basic NPC ambience. -
Restoration states. - Map and objectives.

Acceptance: - The district has a coherent visual identity. - At least
three projects can be completed. - Restoration changes remain after
reconnect. - Players can navigate without getting lost.

## Phase 5 --- Missions and progression

Deliverables: - Mission browser. - Mission instances. - Results
screen. - Credits/reputation. - Inventory and unlocks. - Character
cosmetics. - Basic garage.

Acceptance: - Mission rewards are committed once. - Mission progress
returns to the persistent world. - Players understand their rewards. -
No critical progression can be lost by a disconnect.

## Phase 6 --- Scale, polish and Steam playtest

Deliverables: - Zone allocation. - Load testing. - Network
optimization. - Crash reporting. - Moderation tools. - Accessibility. -
Localization. - Steam playtest package.

Acceptance: - Documented safe player capacity per zone. - Stable server
tick under target load. - No severe exploit in reward/economy flow. -
Main onboarding and social flow are understandable without developer
help.

------------------------------------------------------------------------

# 24. Vertical slice: exact playable scenario

The first playable build should deliver this complete scenario:

**Scenario: Reopen the Greenfield Workshop**

1.  Player launches the game.
2.  Player sees the Who Broke It? splash screen.
3.  Player signs in through Steam.
4.  Player creates a character.
5.  Player enters the main menu.
6.  Player creates a party and invites one friend.
7.  Friend accepts the invite.
8.  Both players enter the lobby.
9.  They see each other's avatars.
10. They use lobby chat and emotes.
11. Party leader selects Greenfield.
12. Both players ready up.
13. The game connects them to a dedicated server.
14. They spawn in Reclaimer Yard.
15. They see the Workshop restoration project.
16. They accept the project.
17. One player sweeps rubbish while the other moves scrap with the
    trolley.
18. They repair a fuse box.
19. They use the forklift to move a tool cabinet.
20. The forklift tips or the load falls in a funny but recoverable way.
21. They recover the object and finish the job.
22. The workshop lights turn on and its appearance changes.
23. Both receive contribution and rewards.
24. The project remains completed after both players disconnect.
25. On their next login, the workshop is still restored.

This scenario is the minimum proof that Who Broke It? works as a game, not
just as a collection of menus and systems.

------------------------------------------------------------------------

# 25. Claude IDE implementation instructions

Claude should follow these rules throughout development.

## 25.1 Before coding

1.  Inspect the repository and existing Unity project.
2.  Report the current state and any conflicts with this document.
3.  Identify installed Unity version and packages.
4.  Check available disk space and target build environment.
5.  Create a phased implementation plan.
6.  Do not ask the user to manually create files that Claude can safely
    create.
7.  Do not replace project settings without explaining the reason.
8.  Do not introduce paid assets or services without approval.

## 25.2 Coding standards

-   Use C# conventions consistently.
-   Prefer composition over giant manager classes.
-   Keep gameplay systems decoupled.
-   Use ScriptableObjects for tunable definitions where suitable.
-   Separate runtime logic from editor tooling.
-   Use interfaces for replaceable service boundaries.
-   Avoid hard-coded scene object lookups where a robust reference
    system is appropriate.
-   Use dependency injection only where it adds real value.
-   Add comments for non-obvious logic, not every line.
-   Use structured logging.
-   Handle null references and disconnect states.
-   Keep network authority explicit.
-   Do not trust client input for rewards or persistent state.
-   Add tests for critical calculations and state transitions.

## 25.3 Asset workflow

For every asset: 1. Define purpose and in-game scale. 2. Create a
concept or reference sheet. 3. Model in Blender or use a legally
licensed asset. 4. Apply low-cost materials. 5. Set pivots and colliders
correctly. 6. Create LODs where useful. 7. Export to Unity with
consistent naming. 8. Test in the actual scene. 9. Check draw calls,
memory, and collision behaviour. 10. Record the source and license.

Never use ripped assets, unlicensed models, or copied game art.

## 25.4 AI-generated asset guidance

AI tools may be used for: - Mood boards. - Concept art. - Colour
exploration. - Texture ideas. - Prop variations. - Character outfit
concepts. - UI inspiration.

AI-generated images are not automatically production-ready 3D assets.
Convert concepts into clean, optimized, riggable game assets. Check
usage rights and commercial terms for every tool and model.

## 25.5 Required project documentation

Maintain: - `README.md` --- setup and run instructions. - `GDD.md` ---
this game design document. - `CHANGELOG.md` --- completed changes. -
`DECISIONS.md` --- architecture and design decisions. - `ROADMAP.md` ---
phase progress. - `TEST_PLAN.md` --- test scenarios and results. -
`ASSET_CREDITS.md` --- asset source and license records.

## 25.6 Definition of done

A feature is not complete because code was written.

A feature is complete only when: - It compiles. - It runs in the
intended scene. - Its normal flow works. - Its failure flow works. -
Multiplayer authority is correct where applicable. - It has been tested
with at least two clients when networked. - It has no obvious UI or
input dead ends. - It is documented. - Its acceptance criteria are met.

## 25.7 Work reporting format

After each implementation phase, Claude should report: - What was
implemented. - Files created or changed. - How to run it. - Tests
performed. - Known limitations. - Remaining work. - Any decision
requiring user approval.

Do not claim a feature works unless it has actually been built and
tested.

------------------------------------------------------------------------

# 26. First task for Claude

Start with **Phase 0 only**.

Inspect the repository and environment. Then: 1. Confirm Unity 6
availability and project state. 2. Create or configure a URP project. 3.
Set up the folder structure. 4. Configure the Input System. 5. Create a
basic third-person character controller and camera. 6. Create a simple
test yard using primitive or placeholder assets. 7. Add one interactable
object. 8. Produce a Windows development build. 9. Write setup
instructions and a Phase 0 completion report.

Do not start the full lobby, economy, world persistence, large map, or
mission system in Phase 0.

The goal is to establish a clean, buildable foundation.

------------------------------------------------------------------------

# 27. Final product definition

Who Broke It? succeeds when a group of friends can enter a shared world,
understand what needs doing, cooperate naturally, create funny
unscripted moments, make visible progress, and return later to see that
their work still matters.

The game should be approachable for a casual player, expressive enough
for content creators, and architected to grow into a larger persistent
multiplayer experience.

**Build the fun first. Make the world bigger second.**
