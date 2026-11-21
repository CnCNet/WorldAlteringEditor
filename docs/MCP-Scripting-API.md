# MCP Scripting API

## Purpose

The World-Altering Editor MCP server exposes map-local TaskForces, Scripts, TeamTypes, AITriggers, Local Variables, Triggers, and Tags to AI agents. The API supports browsing the active game configuration and applying related scripting changes as one validated, atomic operation.

Each applied scripting change set is recorded as a single entry in the map's undo/redo history. The result of `apply_scripting_changes` includes its `mutationId`; agents can inspect it with `review_action` and revert it with `undo_latest` or `undo_through`, and users can revert it with Ctrl+Z like any other edit. See [Action history, review, and undo](#action-history-review-and-undo).

## Recommended workflow

1. Discover active definitions, HouseTypes, object INI names, waypoints, flags, and other parameter options.
2. Browse any existing elements that will be updated, deleted, referenced, or guarded.
3. Build one change set, using `TemporaryKey` references between elements created together.
4. Call `validate_scripting_changes` for a dry run.
5. If validation succeeds, call `apply_scripting_changes` with the same logical request and still-current hashes.
6. Keep the generated IDs and `ContentHash` values returned in `created` and `updated`.

Validation and application both run against the current map state. A successful validation is therefore not a lock: application can still reject the request if a guarded element changes in between.

## Runtime discovery

Do not assume that a familiar action, event, HouseType, unit, flag, side, or preset has its default ID or INI name. Discover these values from the active game configuration.

- `get_scripting_definitions` returns Script actions, Trigger events and actions, their editable and hardcoded parameter definitions, preset options, TeamType flags and defaults, AITrigger conditions and comparators, and sides.
- `get_house_types` returns valid owner HouseType INI names.
- `get_scripting_parameter_options` returns current semantic options for types such as `Techno`, `TeamType`, `HouseType`, `Waypoint`, `WaypointZZ`, `LocalVariable`, `Sound`, and `Theme`.
- The element browsing tools return permanent IDs, references, raw values where relevant, and current content hashes. `includeGlobal` exposes read-only global TaskForces, Scripts, and TeamTypes where supported.

The numeric definition IDs used in the examples below are the bundled defaults only. Always look up the matching definition by its runtime name and substitute the returned ID. Likewise, `Soviet`, `HTNK`, and `V2RL` are illustrative INI names that must be replaced with values returned for the loaded rules.

## Content hashes and concurrent editing

Every returned permanent scripting element includes a deterministic `ContentHash` in the form `sha256:...`. It is calculated on demand from the element's canonical persisted state and is independent of the global map revision.

Every update and delete must include the element's current `ExpectedContentHash`. Immediately before applying a change set, the server recalculates all expected hashes and rejects the entire request if one differs. This prevents an agent from overwriting a human's concurrent edit to the same scripting element while allowing unrelated terrain, object, and scripting work to continue.

`Preconditions` add the same guard to elements that influenced the requested change but are not themselves updated or deleted. They can use permanent identities returned by reference inspection, including global scripting elements and read-only `MapTechno` and `CellTag` reference sources. A precondition cannot use a `TemporaryKey`, because temporary elements do not exist before the change set.

For example, an update is a complete replacement, not a patch:

```json
{
  "request": {
    "updates": {
      "taskForces": [
        {
          "id": "01000000",
          "expectedContentHash": "sha256:current-task-force-hash",
          "replacement": {
            "name": "Updated Soviet armor",
            "group": -1,
            "members": [
              { "index": 0, "technoType": "HTNK", "count": 6 },
              { "index": 3, "technoType": "V2RL", "count": 4 }
            ]
          }
        }
      ]
    },
    "preconditions": [
      {
        "kind": "TeamType",
        "id": "01000002",
        "expectedContentHash": "sha256:current-team-type-hash"
      }
    ]
  }
}
```

Copy every editable property from the latest browse result into `replacement`, changing only what the user requested. Omitted optional properties are cleared or reset to their documented defaults. Updates and deletes apply only to map-local editable elements; global elements and read-only reference sources may only be browsed, referenced, or guarded.

## Temporary references

The server generates permanent INI IDs, so a caller cannot know the IDs of several related elements before creating them in one operation. A create can therefore have a request-scoped `TemporaryKey`, and another value in the same change set can reference it with `TemporaryKey` plus the expected element `Kind`.

A reference must specify exactly one of `Id`, `Index`, or `TemporaryKey`. Temporary keys are case-sensitive, may not have leading or trailing whitespace, are never stored in the map, and are distinct from permanent IDs and user-facing names. The result maps each key to its generated permanent ID or Local Variable index.

## Atomic change sets

`apply_scripting_changes` accepts typed create, update, delete, and precondition collections. The server performs these phases atomically:

1. Validate request shapes, temporary keys, runtime definitions, references, and primitive values.
2. Verify expected content hashes and preconditions.
3. Reserve permanent scripting IDs and Local Variable indices.
4. Construct new elements and resolve permanent and temporary references.
5. Validate the projected reference graph and reject dangling references or unsafe deletions.
6. Commit the validated changes and return normalized results and generated IDs.

Validation errors commit nothing, and unexpected commit-time failures are rolled back. Deletion never silently cascades or detaches references.

## Parameter handling

Script actions, Trigger events, and Trigger actions are identified by IDs returned from `get_scripting_definitions`. Callers provide semantic values rather than engine storage encodings:

- A configured preset accepts its returned `Value`, full option text, or display label. For example, the bundled `Do This` action accepts either `14` or `Hunt`.
- Object-valued parameters accept the option's INI name or index as described by its parameter type.
- `HouseType` accepts a loaded INI name or a raw numeric value documented by the active definition, including sentinel values such as `-1` for any house.
- `Waypoint` and `WaypointZZ` both accept a numeric waypoint identifier, which must exist in the current map.
- Reference-valued parameters use a typed reference object and may point to an existing permanent element or a `TemporaryKey` in the same request.
- Trigger event/action parameter entries include the zero-based configured parameter `Index`.

Supply only editable parameters. Omit parameters whose definition reports `IsUsed: false` or `IsHardcoded: true`; the server fills those fields with their configured defaults. Unknown existing Script or Trigger action definitions remain browseable through raw values and may be preserved unchanged, but a newly authored action requires an active runtime definition.

TaskForce members similarly have an optional zero-based `Index` from 0 through 5. Omit it to use the first free slot. Preserve the indices returned by `get_task_forces` during a full-replacement update so sparse INI slots are not compacted unintentionally.

## Discovery and browsing tools

- `get_house_types`
- `get_scripting_definitions`
- `get_scripting_parameter_options`
- `get_task_forces`
- `get_scripts`
- `get_team_types`
- `get_ai_triggers`
- `get_local_variables`
- `get_triggers`
- `get_tags`
- `get_scripting_references`
- `validate_scripting_changes`
- `apply_scripting_changes`

## End-to-end example: Hard-only Soviet AI attack

Goal: produce five Heavy Tanks and four V2 Launchers for the Soviet HouseType on Hard difficulty, then run `Do This -> Hunt`.

First discover the active values:

```text
get_house_types { "nameFilter": "Soviet" }
get_scripting_definitions {}
get_scripting_parameter_options { "parameterType": "Techno", "nameFilter": "Heavy Tank" }
get_scripting_parameter_options { "parameterType": "Techno", "nameFilter": "V2 Launcher" }
```

From those results, select the exact HouseType and object INI names, the `Do This` action ID and `Hunt` preset, the unconditional AITrigger condition, comparator, and side. With the bundled illustrative values, one atomic `apply_scripting_changes` request is:

```json
{
  "request": {
    "creates": {
      "taskForces": [
        {
          "temporaryKey": "hard-soviet-force",
          "value": {
            "name": "H Soviet Armor",
            "group": -1,
            "members": [
              { "index": 0, "technoType": "HTNK", "count": 5 },
              { "index": 1, "technoType": "V2RL", "count": 4 }
            ]
          }
        }
      ],
      "scripts": [
        {
          "temporaryKey": "hard-soviet-hunt-script",
          "value": {
            "name": "Do This - Hunt",
            "actions": [
              { "actionId": 11, "value": "Hunt" }
            ]
          }
        }
      ],
      "teamTypes": [
        {
          "temporaryKey": "hard-soviet-team",
          "value": {
            "name": "H Soviet Assault Team",
            "group": -1,
            "houseType": "Soviet",
            "script": { "kind": "Script", "temporaryKey": "hard-soviet-hunt-script" },
            "taskForce": { "kind": "TaskForce", "temporaryKey": "hard-soviet-force" },
            "max": 1,
            "priority": 7,
            "techLevel": 0,
            "veteranLevel": 1
          }
        }
      ],
      "aiTriggers": [
        {
          "temporaryKey": "hard-soviet-ai-trigger",
          "value": {
            "name": "H Soviet Armor Attack",
            "primaryTeam": { "kind": "TeamType", "temporaryKey": "hard-soviet-team" },
            "ownerName": "Soviet",
            "techLevel": 0,
            "conditionType": -1,
            "comparatorOperator": 0,
            "comparatorQuantity": 0,
            "initialWeight": 50,
            "minimumWeight": 30,
            "maximumWeight": 70,
            "side": 0,
            "easy": false,
            "medium": false,
            "hard": true,
            "enabled": true
          }
        }
      ]
    }
  }
}
```

Omitting `enabledFlags` on a new TeamType applies the active configuration defaults. The TeamType has `max: 1` because an AITrigger cannot produce a map-local team whose maximum is zero.

## End-to-end example: timed off-map reinforcement

Goal: after 300 in-game seconds, create eight Heavy Tanks at waypoint 44 and have them hunt the player.

Discover the same HouseType, Heavy Tank, `Do This -> Hunt` values, plus the runtime definitions named `Elapsed Time` and `Reinforcement At Waypoint`. Confirm waypoint 44 exists:

```text
get_scripting_definitions {}
get_scripting_parameter_options { "parameterType": "Waypoint", "nameFilter": "44" }
```

Using bundled default definition IDs 11, 13, and 80 for illustration:

```json
{
  "request": {
    "creates": {
      "taskForces": [
        {
          "temporaryKey": "reinforcement-force",
          "value": {
            "name": "Eight heavy tanks",
            "group": -1,
            "members": [
              { "index": 0, "technoType": "HTNK", "count": 8 }
            ]
          }
        }
      ],
      "scripts": [
        {
          "temporaryKey": "reinforcement-hunt-script",
          "value": {
            "name": "Reinforcement hunt",
            "actions": [
              { "actionId": 11, "value": "Hunt" }
            ]
          }
        }
      ],
      "teamTypes": [
        {
          "temporaryKey": "reinforcement-team",
          "value": {
            "name": "Heavy tank reinforcement team",
            "group": -1,
            "houseType": "Soviet",
            "script": { "kind": "Script", "temporaryKey": "reinforcement-hunt-script" },
            "taskForce": { "kind": "TaskForce", "temporaryKey": "reinforcement-force" },
            "max": 0,
            "priority": 7,
            "waypoint": 44,
            "techLevel": 0,
            "veteranLevel": 1
          }
        }
      ],
      "triggers": [
        {
          "temporaryKey": "reinforcement-trigger",
          "value": {
            "name": "Heavy tanks after 300 seconds",
            "houseType": "Soviet",
            "disabled": false,
            "easy": true,
            "normal": true,
            "hard": true,
            "events": [
              {
                "eventId": 13,
                "parameters": [
                  { "index": 1, "value": "300" }
                ]
              }
            ],
            "actions": [
              {
                "actionId": 80,
                "parameters": [
                  {
                    "index": 1,
                    "reference": { "kind": "TeamType", "temporaryKey": "reinforcement-team" }
                  },
                  { "index": 6, "value": "44" }
                ]
              }
            ]
          }
        }
      ],
      "tags": [
        {
          "temporaryKey": "reinforcement-tag",
          "value": {
            "name": "Heavy tank reinforcement tag",
            "repeating": 0,
            "trigger": { "kind": "Trigger", "temporaryKey": "reinforcement-trigger" }
          }
        }
      ]
    }
  }
}
```

Only the editable event/action parameters are present. For the bundled definitions, Elapsed Time uses parameter index 1; Reinforcement At Waypoint uses the TeamType at index 1 and waypoint at index 6. The server fills action parameter 0 with its hardcoded value and normalizes all unused storage fields.

## Validation principles

- TaskForces contain one through six positive-count member entries drawn from valid aircraft, infantry, or vehicle types.
- Scripts use active configured actions and respect the engine's supported action count.
- TeamTypes reference valid TaskForces, Scripts, optional Tags, HouseTypes, flags, and existing waypoints.
- AITriggers reference valid TeamTypes and game-appropriate condition types, owners, sides, and comparison objects. AI-produced map-local teams must have `Max` of at least 1.
- Local Variable indices are unique; an omitted create index allocates the first available value.
- Trigger definitions, parameter types, availability, owners, linked triggers, difficulty flags, and special encodings are validated. A Trigger may contain at most 18 actions.
- Tags reference a valid Trigger and use a repeating value from 0 through 2.
- Deletion checks incoming references from scripting elements, placed technos, and cell tags.
- Global TaskForces, Scripts, and TeamTypes may be browsed, referenced, and guarded where supported, but cannot be edited through map-local operations.

## Action history, review, and undo

Every MCP action that changes the map, including scripting change sets, is recorded in the editor's regular undo/redo history alongside the user's own edits. The history is the list of actions an agent can inspect and revert:

- `get_mutation_history` lists undo and redo entries newest first. Entries created through MCP have a `mutationId`, the tool name, a UTC timestamp, a `kind` of `Map` or `Scripting`, and an affected-cell summary.
- `review_action` returns one entry in detail, whether it is currently applied or has been undone. For a scripting change set it lists each created, updated, and deleted element with a field-level before/after diff and whether the element still matches the recorded state. For a map edit it suggests a cell region to pass to `screenshot_map_region` or `move_camera`.
- `undo_latest` and `redo_latest` step one entry at a time.
- `undo_through` undoes every entry from the top of the undo history down to a given `mutationId` in one call. It refuses, without changing anything, if any entry in that range is a manual edit by the user.

The scripting editor windows change scripting data directly rather than through the undo history. Undo and redo of a scripting change set therefore check first that the map still matches the state the entry expects:

- Updated and created elements must not have been edited or removed since.
- Elements that undo or redo would remove must not be referenced by anything outside the change set, such as a TeamType the user created afterwards.
- Elements that undo or redo would bring back must not collide with an ID or Local Variable index that has been reused since, and their direct references must still resolve.

If a check fails, MCP undo and redo refuse with an explanation and change nothing; the entry reports a `blockReason` in the history. When the user presses Ctrl+Z or Ctrl+Y on such an entry, the editor removes it from the history and shows a notification instead of overwriting the user's newer work.

## Editor tools

These tools act on the editor rather than on map content. None of them shows a dialog or takes keyboard or mouse focus from the user.

- `get_camera` returns the cell at the center of the user's view, the zoom level, and the approximate visible size in cells.
- `move_camera` centers the view on a cell, or on the cells changed by a `mutationId`, and can change the zoom level. While the user is scrolling, painting, dragging or rotating objects, or has moved the view within the last two seconds, the camera is left alone and `notMovedReason` says why. `force` overrides this and should only be used when the user asks for it.
- `save_map` saves the map like File > Save. Without `filePath` it writes to the map's current file; with `filePath` (an absolute `.map`, `.mpr`, or `.yrm` path) it saves to a new file, which is required for a map that has never been saved. Replacing a different existing file requires `overwrite`. Instead of the "Issues Found" dialog that follows a manual save, the result returns the map checker's issues, and the editor shows a brief "Map saved by an MCP agent" notification.
