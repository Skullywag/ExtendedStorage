# Version 2.5

_Upgrading to this version in an existing savegame is fully supported._

- Items are no longer stored as one giant stack on the output cell, but as multiple regular sized stacks. Allows multiple pawns picking up/queueing items from the same storage building at once. Also prevents (a mostly rare) issue on save/load where giant stacks might get truncated to regular  stack sizes on load.
- Storage buildings now show a cumulative item count on the output slot (or the lowest quality for Clothing racks).
- Stored items now correctly use single/some/max stacksize icons for stored items.
- Storage buildings can be renamed.
- Changed capacity limits for storage buildings. Buildings no longer have a hard number of items they can store, but a multiplier relative to a regular stack's size. This change leads to - at worst - unchanged capacity (for normal items), a slightly better capacity for small size items, or significantly better capacity for 'odd' stack size items like Chemfuel, Hay or Pills.
- Stored items are now ejected on storage building minification/exclusion by filter
- Items stored in clothing rack can now all be selected for force wear in right click popup menu.
- Changed internal workings so mods like [StorageSearch](http://steamcommunity.com/sharedfiles/filedetails/?id=726479594) can filter Extended Storage buildings

## Storage limits

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
