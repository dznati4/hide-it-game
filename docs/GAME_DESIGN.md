# Hide iT — agreed design

Developer brand: **ZanettiZONEai**. Initial language: English; Hebrew later.
Engine: Unity, initially targeting Windows; Steam is a later distribution target.
Current budget: **0 NIS**. No purchases or paid service upgrades.

## Core experience

A comic, colorful 3D stealth co-op game. One player drags a body, another opens a door,
and a third watches for patrols. The body catches on doorframes, falls down steps,
and produces awkward situations through actual ragdoll physics.
Start in third person; V switches between third and first person.

The game supports one to three players. Co-op rooms allow two or three players.
A separate compact map is designed for solo play, including a small four-wheel trolley
or an equivalent handling aid. Solo does not use an unchanged co-op layout.

## Rooms and online play

The host creates a room and receives a code. Friends enter the code from their own
computers over the internet. The host starts the mission once the group is ready.
Limit every room to three players, including the host.
The first prototype uses Discord for voice; no built-in voice chat.

An internet room code needs a working connection service, not an invented local code.
Evaluate Unity Netcode and Relay at the networking milestone, with project linking,
authentication, and the free-tier limits made explicit. No service has been activated
and no claim of working internet multiplayer is made at the offline milestone.

## Body handling

- A player grabs a hand or ankle and drags slowly.
- Two players carry opposite ends faster; different pulling directions remain physical.
- A third player can scout, open routes, or distract patrols.
- The body remains jointed and collides with doors, floors, stairs, and props.
- Handling aids are permitted in the solo map, without replacing the core body physics.

## Detection and pursuit

Being visible is not itself an offense. Guards, cameras, and bystanders react to
seeing the body, illegal actions, or suspicious activity.
Suspicion fills gradually. At high suspicion the screen gains a red effect and the
affected character receives a red marker. At maximum, guards pursue.
Cameras report suspicious activity to guards. Bystanders can alert them.
After 5–8 seconds without detection, suspicion starts to fall.
Use lockers, service rooms, cover, and designated body hiding places.

## Arrest and rescue

A captured player is detained at a map-specific holding area. The others continue
and can release that player. The mission fails when all active players are detained.
In solo, detention therefore fails the mission. Restart is available after failure.

Victory requires the body at the extraction point **and every participating player escaped**.
A detained teammate must be rescued before the team can finish.
The host leaving ends the session in the initial version; host migration is not a first goal.

## Mission maps

1. **Hotel** — co-op, target 8–12 minutes: rooms, corridors, lobby, elevator routes,
   guests, guards, cameras, storage rooms, and an extraction route.
2. **Underground parking** — co-op, target 8–12 minutes: cars, pillars, barriers,
   security office, guards, cameras, and an exit gate.
3. **Solo map** — a separate small service area with routes and handling adapted to
   one player. Exact layout and duration will be tuned after the core works.

Small route objectives include keys, locked doors, switches, access cards, and gates.
The 8–12 minute timings are design targets, not measured results.

## First playable release requirements

Walking, running, crouching, dragging, shared carrying, ragdoll physics, guard vision,
cameras, hiding places, keys/locked doors, and a distraction item.
Include a trolley if development permits. Trail cleaning and built-in voice are deferred.

## Process

Implement one milestone, run its checks, and playtest it before adding the next system.
Do not build all maps, AI, networking, and polish concurrently.
Use original placeholders initially, then only assets with verified licenses suitable
for the intended release. Keep names, branding, and project content specific to Hide iT.
