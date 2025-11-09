# Away Collections Feature

## Overview

The Away Collections feature allows players to earn rewards while offline. When a player starts an activity (mining or fighting) and then leaves the game, they will receive rewards based on how long they were away when they return.

## How It Works

1. **Activity Tracking**: When a player starts mining or fighting, the system tracks:
   - What activity they're doing
   - When they started
   - What resource/monsters they're interacting with

2. **Saving State**: When the player leaves (returns to character screen or closes the game), the activity state is saved.

3. **Calculating Rewards**: When the player returns, the system:
   - Calculates how long they were away
   - Calculates rewards based on the activity type and duration
   - Shows a rewards panel

4. **Collecting Rewards**: The player can collect all rewards with a single button click.

## Activity Types

### Mining
- Tracks what resource is being gathered
- Calculates items gathered based on gather rate and time away
- Rewards: Items from the resource's drop table

### Fighting
- Tracks what monsters are being fought
- Calculates monsters killed based on combat speed
- Rewards: XP, Gold, and items from monster drop tables
- Applies equipment and talent bonuses to XP/Gold rewards

## Implementation Details

### New Scripts Created

1. **AwayActivityManager.cs**
   - Singleton that tracks current activity
   - Saves/loads activity state to PlayerPrefs
   - Called by ResourceManager and CombatManager when activities start/stop

2. **AwayRewardsCalculator.cs**
   - Static class that calculates rewards based on time away
   - Handles both mining and fighting reward calculations
   - Applies equipment and talent bonuses

3. **AwayRewardsPanel.cs**
   - UI component that displays away rewards
   - Shows time away, activity name, and all rewards earned
   - Handles collecting rewards and applying them to the character

### Modified Scripts

1. **ResourceManager.cs**
   - Registers mining activities with AwayActivityManager
   - Clears activity when gathering stops

2. **CombatManager.cs**
   - Registers fighting activities with AwayActivityManager
   - Clears activity when combat ends

3. **CharacterLoader.cs**
   - Checks for away rewards when loading a character
   - Creates and shows AwayRewardsPanel if rewards are available

4. **ReturnToCharacterSelect.cs**
   - Saves away activity state before returning to character screen

5. **CharacterManager.cs**
   - Ensures AwayActivityManager is created on game start

## UI Setup

The `AwayRewardsPanel` component can be added to a UI GameObject in your scene. It requires:

- **Panel Object**: The main panel GameObject (can be the same GameObject as the component)
- **Title Text**: TextMeshProUGUI showing "Welcome Back!"
- **Activity Text**: TextMeshProUGUI showing what activity was being done
- **Time Away Text**: TextMeshProUGUI showing formatted time away
- **Rewards Container**: Transform/GameObject that will hold reward items
- **Reward Item Prefab** (optional): Prefab for displaying individual rewards
- **Collect Button**: Button to collect rewards and close panel
- **XP Icon** (optional): Sprite for XP rewards
- **Gold Icon** (optional): Sprite for gold rewards

If no prefab is assigned, the panel will create simple text items for rewards.

## Usage

The feature works automatically once set up:

1. Player starts mining or fighting
2. Player returns to character screen or closes game
3. Player logs back in and enters the game scene
4. AwayRewardsPanel automatically appears showing rewards
5. Player clicks "Collect" to receive rewards

## Notes

- Rewards are calculated based on the activity's rate (gather rate for mining, attack speed for fighting)
- Equipment and talent bonuses are applied to fighting rewards (XP/Gold)
- The system uses PlayerPrefs to persist activity state between sessions
- Activity state is cleared after rewards are collected

