using CADability.Actions;
using CADability.GeoObject;
using System.Collections.Generic;

namespace CADability.UserInterface
{
    /// <summary>
    /// Shows the properties of a point.
    /// </summary>

    public class ShowPropertyPoint : IShowPropertyImpl, ICommandHandler, IGeoObjectShowProperty, IDisplayHotSpots
    {
        private Point point;
        private IFrame frame;
        private GeoPointProperty locationProperty;
        private IShowProperty[] subEntries;
        private IShowProperty[] attributeProperties; // Anzeigen für die Attribute (Ebene, Farbe u.s.w)

        public ShowPropertyPoint(Point point, IFrame frame)
        {
            this.point = point;
            this.frame = frame;
            locationProperty = new GeoPointProperty("Point.Location", frame, true);
            locationProperty.GetGeoPointEvent += new CADability.UserInterface.GeoPointProperty.GetGeoPointDelegate(OnGetLocation);
            locationProperty.SetGeoPointEvent += new CADability.UserInterface.GeoPointProperty.SetGeoPointDelegate(OnSetLocation);
            locationProperty.GeoPointChanged(); // Initialisierung
            locationProperty.ModifyWithMouseEvent += new ModifyWithMouseDelegate(ModifyLocationWithMouse);
            locationProperty.StateChangedEvent += new StateChangedDelegate(OnStateChanged);

            attributeProperties = point.GetAttributeProperties(frame);

            resourceId = "Point.Object";
        }

        private void OnPointDidChange(IGeoObject Sender, GeoObjectChange Change)
        {
            locationProperty.GeoPointChanged();
        }

        #region IShowPropertyImpl Overrides
        public override ShowPropertyEntryType EntryType { get { return ShowPropertyEntryType.GroupTitle; } }
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
                    IShowProperty[] mainProps = { locationProperty };
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
                List<MenuWithHandler> items = new List<MenuWithHandler>(MenuResource.LoadMenuDefinition("MenuId.Object.Point", false, this));
                CreateContextMenueEvent?.Invoke(this, items);
                point.GetAdditionalContextMenue(this, Frame, items);
                return items.ToArray();
            }
        }
        public override void Opened(bool IsOpen)
        {	// dient dazu, die Hotspots anzuzeigen bzw. zu verstecken wenn die SubProperties
            // aufgeklappt bzw. zugeklappt werden. Wenn also mehrere Objekte markiert sind
            // und diese Linie aufgeklappt wird.
            if (HotspotChangedEvent != null)
            {
                if (IsOpen)
                {
                    HotspotChangedEvent(locationProperty, HotspotChangeMode.Visible);
                }
                else
                {
                    HotspotChangedEvent(locationProperty, HotspotChangeMode.Invisible);
                }
            }
            base.Opened(IsOpen);
        }
        public override void Added(IPropertyPage propertyTreeView)
        {	// die events müssen in Added angemeldet und in Removed wieder abgemeldet werden,
            // sonst bleibt die ganze ShowProperty für immer an der Linie hängen
            this.point.DidChangeEvent += new ChangeDelegate(OnPointDidChange);
            point.UserData.UserDataAddedEvent += new UserData.UserDataAddedDelegate(OnUserDataAdded);
            point.UserData.UserDataRemovedEvent += new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Added(propertyTreeView);
        }
        void OnUserDataAdded(string name, object value)
        {
            this.subEntries = null;
            attributeProperties = point.GetAttributeProperties(frame);
            propertyTreeView.Refresh(this);
        }
        public override void Removed(IPropertyTreeView propertyTreeView)
        {
            this.point.DidChangeEvent -= new ChangeDelegate(OnPointDidChange);
            point.UserData.UserDataAddedEvent -= new UserData.UserDataAddedDelegate(OnUserDataAdded);
            point.UserData.UserDataRemovedEvent -= new UserData.UserDataRemovedDelegate(OnUserDataAdded);
            base.Removed(propertyTreeView);
        }
#endregion
        private GeoPoint OnGetLocation(GeoPointProperty sender)
        {
            return point.Location;
        }
        private void OnSetLocation(GeoPointProperty sender, GeoPoint p)
        {
            point.Location = p;
        }
        private void ModifyLocationWithMouse(IPropertyEntry sender, bool StartModifying)
        {
            GeneralGeoPointAction gpa = new GeneralGeoPointAction(locationProperty, point);
            frame.SetAction(gpa);
        }
#region ICommandHandler Members

        public bool OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Reverse":
                    {
                    }

                    ;
                    return true;
            }
            return false;
        }

        public bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.Reverse":
                    {
                    }
                    return true;
            }
            return false;
        }

#endregion
#region IGeoObjectShowProperty Members
        public event CADability.GeoObject.CreateContextMenueDelegate CreateContextMenueEvent;
        IGeoObject IGeoObjectShowProperty.GetGeoObject()
        {
            return point;
        }
        string IGeoObjectShowProperty.GetContextMenuId()
        {
            return "MenuId.Object.Point";
        }
#endregion
#region IDisplayHotSpots Members
        public event CADability.HotspotChangedDelegate HotspotChangedEvent;
        void IDisplayHotSpots.ReloadProperties()
        {
            base.propertyTreeView.Refresh(this);
        }
        private void OnStateChanged(IShowProperty sender, StateChangedArgs args)
        {
            if (HotspotChangedEvent != null)
            {
                if (args.EventState == StateChangedArgs.State.Selected || args.EventState == StateChangedArgs.State.SubEntrySelected)
                {
                    if (sender == locationProperty)
                    {
                        HotspotChangedEvent(locationProperty, HotspotChangeMode.Selected);
                    }
                }
                else if (args.EventState == StateChangedArgs.State.UnSelected)
                {
                    if (sender == locationProperty)
                    {
                        HotspotChangedEvent(locationProperty, HotspotChangeMode.Deselected);
                    }
                }
            }
        }
#endregion
    }
}
