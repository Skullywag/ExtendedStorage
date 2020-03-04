# Version 3.5.0

1.1 release
some tweaks by Terragg: updated preview image, fluffy's mod manager support, correct display of weapons & corpses in storage

# Version 3.4.1.1

Fix for clothing rack right click menu issue

# Version 3.4.1

1.0 release

# Version 3.4.0

B19 release

# Version 3.3.0

Experimental 1.0 version

# Version 3.2.0

Implement stack merging by fluffy

# Version 3.1.0

Experimental A18 version (compiles & starts, basic functionality tested... may still show weird behavior)

# Version 3.0.1

## Bugfixes
- Changes to storage priority correctly trigger hauling jobs to & from Extended Storage buildings
- Correctly update total item count label when partial stack is used from output slot.
- Move single non-max stacks from input cell to output.
- Fix error for reinstalled non empty Extended Storage buildings.
- Disallowing & reallowing a stored item while paused will no longer eject the stored item from storage after unpause.

## New features
- Skip now officially supports storing stone chunks
- (Debug) options in God mode (Allow switching displayed filter between User & Storage settings. User settings are default, storage settings are what is actually currently stored in the building).
- Upgrade Harmony to 1.0.9.1

# Version 3.0

_Upgrading to this version in an existing savegame is fully supported._

- Items are no longer stored as one giant stack on the output cell, but as multiple regular sized stacks. Allows multiple pawns picking up/queueing items from the same storage building at once. Also prevents (a mostly rare) issue on save/load where giant stacks might get truncated to regular  stack sizes on load.
- Storage buildings now show a cumulative item count on the output slot (or the lowest quality for Clothing racks).
- Stored items now correctly use single/some/max stacksize icons for stored items.
- Storage buildings can be renamed.
- Changed capacity limits for storage buildings. Buildings no longer have a hard number of items they can store, but a multiplier relative to a regular stack's size. This change leads to - at worst - unchanged capacity (for normal items), a slightly better capacity for small size items, or significantly better capacity for 'odd' stack size items like Chemfuel, Hay or Pills.

    Building | New capacity
    --- | ---:
    Basket | 667 %
    Fabric Hemper | 533 %
    HazMat Container | 250 %
    Clothing rack | 1000 %
    Med. Cabinet | 400 %
    Skip | 1200 %
    Tray rack | 1000 %
    Pallet | 800 %

- Stored items are now ejected on storage building minification/exclusion by filter
- Items stored in clothing rack can now all be selected for force wear in right click popup menu.
- Changed internal workings so mods like [StorageSearch](http://steamcommunity.com/sharedfiles/filedetails/?id=726479594) can filter Extended Storage buildings
