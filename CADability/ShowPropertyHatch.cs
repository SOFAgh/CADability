using CADability.Actions;
using CADability.Attribute;
using CADability.GeoObject;
using System.Collections.Generic;

namespace CADability.UserInterface
{
    /// <summary>
    /// 
    /// </summary>
    internal class ShowPropertyHatch : IShowPropertyImpl, ICommandHandler, IGeoObjectShowProperty
    {
        private Hatch hatch;
        private IShowProperty[] subEntries;
        private IShowProperty[] attributeProperties; // Anzeigen für die Attribute (Ebene, Farbe u.s.w)
        private IFrame frame;
        public ShowPropertyHatch(Hatch hatch, IFrame frame)
        {
            this.hatch = hatch;
            this.frame = frame;
            base.resourceId = "Hatch.Object";
            attributeProperties = hatch.GetAttributeProperties(frame);
        }
        public IShowProperty GetHatchStyleProperty()
        {
            return SubEntries[0];
        }
        #region IShowPropertyImpl Overrides
        public override ShowPropertyEntryType EntryType
        {
            get
            {
                return ShowPropertyEntryType.GroupTitle;
            }
        }

        public override int SubEntriesCount
        {
            get
            {
                return SubEntries.Length;
            }
        }

        public override IShowProperty[] SubEntries
        {
            get
            {
                if (subEntries == null)
                {
                    IShowProperty[] mainProps = new IShowProperty[1];
                    // Die HatchStyleSelectionProperty kommt ja mit den Attributen
                    //HatchStyleSelectionProperty hssp = new HatchStyleSelectionProperty("Hatch.HatchStyle",frame.Project.HatchStyleList,hatch.HatchStyle,false);
                    //mainProps[0] = hssp;
                    //hssp.HatchStyleChangedEvent += new CADability.UserInterface.HatchStyleSelectionProperty.HatchStyleChanged(OnHatchStyleSelectionChanged);
                    DoubleProperty dp = new DoubleProperty("Hatch.Area", frame);
                    dp.GetDoubleEvent += new CADability.UserInterface.DoubleProperty.GetDoubleDelegate(OnGetArea);
                    dp.Refresh();
                    mainProps[0] = dp;
                    subEntries = IShowPropertyImpl.Concat(mainProps, attributeProperties);
                }
                return subEntries;
            }
        }
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                return ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.ContextMenu | ShowPropertyLabelFlags.Selectable;
            }
        }
        public override MenuWithHandler[] ContextMenu
        {
            get
            {
                List<MenuWithHandler> items = new List<MenuWithHandler>(MenuResource.LoadMenuDefinition("MenuId.Object.Hatch", false, this));
                CreateContextMenueEvent?.Invoke(this, items);
                hatch.GetAdditionalContextMenue(this, Frame, items);
                return items.ToArray();
            }
        }
        public override void Added(IPropertyPage propertyTreeView)
        {	// die events müssen in Added angemeldet und in Removed wieder abgemeldet werden,
            // sonst bleibt die ganze ShowProperty für immer an der Linie hängen
            hatch.UserData.UserDataAddedEvent += new UserData.UserDataAddedDelegate(OnUserDataAdded);
            hatch.UserData.UserDataRemovedEvent += new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Added(propertyTreeView);
        }
        void OnUserDataAdded(string name, object value)
        {
            this.subEntries = null;
            attributeProperties = hatch.GetAttributeProperties(frame);
            propertyTreeView.Refresh(this);
        }
        public override void Removed(IPropertyTreeView propertyTreeView)
        {
            hatch.UserData.UserDataAddedEvent -= new UserData.UserDataAddedDelegate(OnUserDataAdded);
            hatch.UserData.UserDataRemovedEvent -= new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Removed(propertyTreeView);
        }

#endregion
        private void OnHatchStyleSelectionChanged(HatchStyle SelectedHatchstyle)
        {
            hatch.HatchStyle = SelectedHatchstyle;
        }
#region ICommandHandler Members
        public bool OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Explode":
                    if (frame.ActiveAction is SelectObjectsAction)
                    {
                        using (frame.Project.Undo.UndoFrame)
                        {
                            IGeoObjectOwner addTo = hatch.Owner;
                            if (addTo == null) addTo = frame.ActiveView.Model;
                            GeoObjectList toSelect = hatch.Decompose();
                            addTo.Remove(hatch);
                            for (int i = 0; i < toSelect.Count; ++i)
                            {
                                addTo.Add(toSelect[i]);
                            }
                            SelectObjectsAction soa = frame.ActiveAction as SelectObjectsAction;
                            soa.SetSelectedObjects(toSelect); // alle Teilobjekte markieren
                        }
                    }
                    return true;
            }
            return false;
        }
        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.Explode":
                    CommandState.Enabled = true; // hier müssen die Flächen rein
                    return true;
            }
            return false;
        }
#endregion
#region IGeoObjectShowProperty Members
        public event CADability.GeoObject.CreateContextMenueDelegate CreateContextMenueEvent;
        IGeoObject IGeoObjectShowProperty.GetGeoObject()
        {
            return hatch;
        }
        string IGeoObjectShowProperty.GetContextMenuId()
        {
            return "MenuId.Object.Hatch";
        }
#endregion
        private double OnGetArea(DoubleProperty sender)
        {
            if (hatch != null && hatch.CompoundShape != null)
                return hatch.CompoundShape.Area;
            return 0.0;
        }
    }
}
