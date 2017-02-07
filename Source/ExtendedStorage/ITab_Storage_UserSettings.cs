using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExtendedStorage
{
    public class ITab_Storage : RimWorld.ITab_Storage
    {
        private Vector2 scrollPosition = Vector2.zero;

        // adapted from RimWorld.ITab_Storage
        protected override void FillTab()
        {
            // change displayed settings to be the user settings.
            Building_ExtendedStorage parent = SelThing as Building_ExtendedStorage;
            StorageSettings settings = parent?.userSettings;
            if (settings == null)
                throw new Exception("failed to get user storage settings.");

            // ITab_Storage.WinSize is copied to base.size in the constructor, no need to access it.
            Rect position = new Rect( 0f, 0f, size.x, size.y ).ContractedBy( 10f );
            GUI.BeginGroup( position );
            Text.Font = GameFont.Small;
            Rect rect = new Rect( 0f, 0f, 160f, 29f );
            if ( Widgets.ButtonText( rect, "Priority".Translate() + ": " + settings.Priority.Label(), true, false, true ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                // remove decompiler garbage
                var priorities = Enum.GetValues( typeof( StoragePriority ) );
                foreach ( StoragePriority priority in priorities )
                    if (priority != StoragePriority.Unstored)
                        options.Add( new FloatMenuOption( priority.Label().CapitalizeFirst(), delegate { settings.Priority = priority; } ) );

                Find.WindowStack.Add( new FloatMenu( options ) );
            }
            UIHighlighter.HighlightOpportunity( rect, "StoragePriority" );
            ThingFilter parentFilter = null;
            if ( parent.GetParentStoreSettings() != null )
            {
                // parentStoreSettings refers to the building's fixed filter, which we haven't touched.
                parentFilter = parent.GetParentStoreSettings().filter;
            }
            Rect rect2 = new Rect( 0f, 35f, position.width, position.height - 35f );
            ThingFilterUI.DoThingFilterConfigWindow( rect2, ref scrollPosition, settings.filter, parentFilter, 8, null, null );
            PlayerKnowledgeDatabase.KnowledgeDemonstrated( ConceptDefOf.StorageTab, KnowledgeAmount.FrameDisplayed );
            GUI.EndGroup();
        }
    }
}
